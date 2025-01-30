// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.BulkUpdates;

using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains functional tests for LibSQL bulk update operations.
/// Tests the proper handling of batch updates and their behavior
/// with different entity configurations and scenarios.
/// </summary>
public class BulkUpdateLibSqlTest : BulkUpdatesTestBase<BulkUpdateLibSqlTest.BulkUpdateLibSqlFixture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpdateLibSqlTest"/> class.
    /// </summary>
    /// <param name="fixture">The test fixture providing the required test infrastructure.</param>
    public BulkUpdateLibSqlTest(BulkUpdateLibSqlFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Tests a simple bulk update operation that modifies a subset of entities
    /// based on a filter condition. Verifies that the correct number of entities
    /// are updated and their values are modified as expected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [ConditionalFact]
    public async Task ExecuteBulkUpdate_Simple()
    {
        await using var context = this.CreateContext();

        // Arrange
        var entities = new List<TestEntity>();
        for (int i = 0; i < 100; i++)
        {
            entities.Add(new TestEntity { Name = $"Entity{i}", Value = i });
        }

        await context.Set<TestEntity>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act
        var affected = await context.Set<TestEntity>()
            .Where(e => e.Value < 50)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Value, b => b.Value + 100));

        // Assert
        Assert.Equal(50, affected);
        var updated = await context.Set<TestEntity>()
            .Where(e => e.Value >= 100)
            .CountAsync();
        Assert.Equal(50, updated);
    }

    /// <summary>
    /// Tests bulk update operations with calculated values, demonstrating the ability
    /// to update multiple properties using expressions. Verifies that both numeric
    /// and string properties are updated correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [ConditionalFact]
    public async Task ExecuteBulkUpdate_WithCalculatedValue()
    {
        await using var context = this.CreateContext();

        // Arrange
        var entities = new List<TestEntity>
        {
            new () { Name = "Test1", Value = 10 },
            new () { Name = "Test2", Value = 20 },
            new () { Name = "Test3", Value = 30 },
        };

        await context.Set<TestEntity>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act
        var affected = await context.Set<TestEntity>()
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(b => b.Value, b => b.Value * 2)
                    .SetProperty(b => b.Name, b => b.Name + "_Updated"));

        // Assert
        Assert.Equal(3, affected);
        var results = await context.Set<TestEntity>().OrderBy(e => e.Value).ToListAsync();
        Assert.Collection(
            results,
            e =>
            {
                Assert.Equal(20, e.Value);
                Assert.Equal("Test1_Updated", e.Name);
            },
            e =>
            {
                Assert.Equal(40, e.Value);
                Assert.Equal("Test2_Updated", e.Name);
            },
            e =>
            {
                Assert.Equal(60, e.Value);
                Assert.Equal("Test3_Updated", e.Name);
            });
    }

    /// <summary>
    /// Tests bulk update operations within a transaction, verifying proper transaction
    /// handling and isolation. Ensures that updates are properly committed and can be
    /// rolled back if necessary.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [ConditionalFact]
    public async Task ExecuteBulkUpdate_WithTransaction()
    {
        await using var context = this.CreateContext();

        // Arrange
        var entities = new List<TestEntity>();
        for (int i = 0; i < 10; i++)
        {
            entities.Add(new TestEntity { Name = $"Trans{i}", Value = i * 100 });
        }

        await context.Set<TestEntity>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var affected = await context.Set<TestEntity>()
                .Where(e => e.Value > 500)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Value, b => -1));

            await transaction.CommitAsync();

            // Assert
            Assert.Equal(4, affected);
            var results = await context.Set<TestEntity>()
                .Where(e => e.Value == -1)
                .CountAsync();
            Assert.Equal(4, results);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Creates a new instance of the database context for testing.
    /// </summary>
    /// <returns>A new instance of the database context.</returns>
    protected DbContext CreateContext()
        => this.Fixture.CreateContext();

    /// <summary>
    /// Test fixture for bulk update tests, providing the necessary infrastructure
    /// and database configuration for running the tests.
    /// </summary>
    public class BulkUpdateLibSqlFixture : SharedStoreFixtureBase<DbContext>, IBulkUpdatesFixtureBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BulkUpdateLibSqlFixture()
        {
            // Create an IHttpClientFactory
            var services = new ServiceCollection();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            LibSqlTestStoreFactory.Instance = LibSqlTestStoreFactory.CreateInstance(_httpClientFactory);
        }

        /// <summary>
        /// Gets the name of the test store.
        /// </summary>
        protected override string StoreName
            => "BulkUpdateTest";

        /// <summary>
        /// Gets the test store factory for creating LibSQL test stores.
        /// </summary>
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;

        /// <summary>
        /// Configures the entity model for the test database.
        /// </summary>
        /// <param name="modelBuilder">The model builder to use for configuration.</param>
        /// <param name="context">The database context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            => modelBuilder.Entity<TestEntity>(
                b =>
                {
                    b.ToTable("TestEntities");
                    b.Property(e => e.Id).ValueGeneratedOnAdd();
                    b.Property(e => e.Name).IsRequired().HasMaxLength(200);
                    b.Property(e => e.Value);
                    b.HasKey(e => e.Id);
                });

        /// <summary>
        /// Gets the context creator function used to create new DbContext instances.
        /// </summary>
        /// <returns>A function that creates a new instance of <see cref="DbContext"/>.</returns>
        public Func<DbContext> GetContextCreator()
            => () => this.CreateContext();

        /// <summary>
        /// Gets the expected data for assertions in tests.
        /// </summary>
        /// <returns>An <see cref="ISetSource"/> instance with the expected data.</returns>
        public ISetSource GetExpectedData()
            => new DefaultSetSource();

        /// <summary>
        /// Configures the test to use a specific transaction.
        /// </summary>
        /// <param name="facade">The database facade to configure.</param>
        /// <param name="transaction">The transaction to use.</param>
        public void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
            => facade.UseTransaction(transaction.GetDbTransaction());

        /// <summary>
        /// Gets the entity asserters used for test assertions.
        /// </summary>
        public IReadOnlyDictionary<Type, object> EntityAsserters
            => new Dictionary<Type, object>();

        /// <summary>
        /// Gets the entity sorters used for test assertions.
        /// </summary>
        public IReadOnlyDictionary<Type, object> EntitySorters
            => new Dictionary<Type, object>();
    }

    /// <summary>
    /// Represents a test entity used in bulk update operations.
    /// </summary>
    protected class TestEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the test entity.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the numeric value associated with the test entity.
        /// </summary>
        public int Value { get; set; }
    }
}
