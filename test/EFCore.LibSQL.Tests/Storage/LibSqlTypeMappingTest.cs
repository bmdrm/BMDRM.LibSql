// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.EntityFrameworkCore.Storage;

using Microsoft.EntityFrameworkCore.LibSql;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.LibSql.Storage.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
public class LibSqlTypeMappingTest : RelationalTypeMappingTest
{
    private readonly DbContextOptions _contextOptions;

    public LibSqlTypeMappingTest()
    {
        var services = new ServiceCollection().AddHttpClient().AddEntityFrameworkLibSql();
        var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        _contextOptions = new DbContextOptionsBuilder()
            .UseInternalServiceProvider(serviceProvider)
            .UseLibSql(LibSqlTestSettings.ConnectionString)
            .Options;
    }

    private class YouNoTinyContext : DbContext
    {
        private string TestConnectionString = LibSqlTestSettings.ConnectionString;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseLibSql(TestConnectionString);

    }

    protected override DbCommand CreateTestCommand()
        => new YouNoTinyContext().Database.GetDbConnection().CreateCommand();

    [ConditionalTheory]
    [InlineData(typeof(LibSqlDateTimeOffsetTypeMapping), typeof(DateTimeOffset))]
    [InlineData(typeof(LibSqlDateTimeTypeMapping), typeof(DateTime))]
    [InlineData(typeof(LibSqlDecimalTypeMapping), typeof(decimal))]
    [InlineData(typeof(LibSqlGuidTypeMapping), typeof(Guid))]
    [InlineData(typeof(LibSqlULongTypeMapping), typeof(ulong))]
    public override void Create_and_clone_with_converter(Type mappingType, Type type)
        => base.Create_and_clone_with_converter(mappingType, type);

    [ConditionalTheory]
    [InlineData("TEXT", typeof(string))]
    [InlineData("Integer", typeof(long))]
    [InlineData("Blob", typeof(byte[]))]
    [InlineData("numeric", typeof(byte[]))]
    [InlineData("real", typeof(double))]
    [InlineData("doub", typeof(double))]
    [InlineData("int", typeof(long))]
    [InlineData("SMALLINT", typeof(long))]
    [InlineData("UNSIGNED BIG INT", typeof(long))]
    [InlineData("VARCHAR(255)", typeof(string))]
    [InlineData("nchar(55)", typeof(string))]
    [InlineData("datetime", typeof(byte[]))]
    [InlineData("decimal(10,4)", typeof(byte[]))]
    [InlineData("boolean", typeof(byte[]))]
    [InlineData("unknown_type", typeof(byte[]))]
    [InlineData("", typeof(byte[]))]
    public void It_maps_strings_to_not_null_types(string typeName, Type type)
        => Assert.Equal(type, CreateTypeMapper().FindMapping(typeName)?.ClrType);

    private static IRelationalTypeMappingSource CreateTypeMapper()
        => TestServiceFactory.Instance.Create<LibSqlTypeMappingSource>();

    public static RelationalTypeMapping GetMapping(Type type)
        => CreateTypeMapper().FindMapping(type);

    public override void DateTimeOffset_literal_generated_correctly()
        => Test_GenerateSqlLiteral_helper(
            GetMapping(typeof(DateTimeOffset)),
            new DateTimeOffset(2015, 3, 12, 13, 36, 37, 371, new TimeSpan(-7, 0, 0)),
            "'2015-03-12 13:36:37.371-07:00'");


    public override void DateTime_literal_generated_correctly()
        => Test_GenerateSqlLiteral_helper(
            GetMapping(typeof(DateTime)),
            new DateTime(2015, 3, 12, 13, 36, 37, 371, DateTimeKind.Utc),
            "'2015-03-12 13:36:37.371'");

    [ConditionalFact]
    public override void DateOnly_literal_generated_correctly()
        => Test_GenerateSqlLiteral_helper(
            GetMapping(typeof(DateOnly)),
            new DateOnly(2015, 3, 12),
            "'2015-03-12'");

    [ConditionalFact]
    public override void TimeOnly_literal_generated_correctly()
    {
        var typeMapping = GetMapping(typeof(TimeOnly));

        Test_GenerateSqlLiteral_helper(typeMapping, new TimeOnly(13, 10, 15), "'13:10:15'");
        Test_GenerateSqlLiteral_helper(typeMapping, new TimeOnly(13, 10, 15, 120), "'13:10:15.1200000'");
        Test_GenerateSqlLiteral_helper(typeMapping, new TimeOnly(13, 10, 15, 120, 20), "'13:10:15.1200200'");
    }


    public override void Decimal_literal_generated_correctly()
    {
        var typeMapping = new LibSqlDecimalTypeMapping("NUMERIC");

        Test_GenerateSqlLiteral_helper(typeMapping, decimal.MinValue, "'-79228162514264337593543950335.0'");
        Test_GenerateSqlLiteral_helper(typeMapping, decimal.MaxValue, "'79228162514264337593543950335.0'");
    }

    public override void Guid_literal_generated_correctly()
        => Test_GenerateSqlLiteral_helper(
            GetMapping(typeof(Guid)),
            new Guid("c6f43a9e-91e1-45ef-a320-832ea23b7292"),
            "'c6f43a9e-91e1-45ef-a320-832ea23b7292'");

    public override void ULong_literal_generated_correctly()
    {
        var typeMapping = new LibSqlULongTypeMapping("INTEGER");

        Test_GenerateSqlLiteral_helper(typeMapping, ulong.MinValue, "0");
        Test_GenerateSqlLiteral_helper(typeMapping, ulong.MaxValue, "-1");
        Test_GenerateSqlLiteral_helper(typeMapping, long.MaxValue + 1ul, "-9223372036854775808");
    }

    [ConditionalFact]
    public override void Primary_key_type_mapping_can_differ_from_FK()
    {
        using var context = new MismatchedFruityContext(_contextOptions);
        Assert.Equal(
            typeof(short),
            context.Model.FindEntityType(typeof(Banana)).FindProperty("Id").GetTypeMapping().Converter.ProviderClrType);
        Assert.Null(context.Model.FindEntityType(typeof(Kiwi)).FindProperty("Id").GetTypeMapping().Converter);
    }

    private class MismatchedFruityContext : FruityContext
    {
        public MismatchedFruityContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Banana>().Property(e => e.Id).HasConversion<short>();
            modelBuilder.Entity<Kiwi>().Property(e => e.Id).HasConversion<int>();
            modelBuilder.Entity<Kiwi>().HasOne(e => e.Banana).WithMany(e => e.Kiwis).HasForeignKey(e => e.Id);
        }
    }

    private class Banana
    {
        public int Id { get; set; }
        public ICollection<Kiwi> Kiwis { get; set; }
    }

    private class Kiwi
    {
        public int Id { get; set; }
        public int BananaId { get; set; }
        public Banana Banana { get; set; }
    }

    private class FruityContext : DbContext
    {
        public FruityContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Banana> Bananas { get; set; }
        public DbSet<Kiwi> Kiwi { get; set; }
    }

    protected override DbContextOptions ContextOptions => _contextOptions;
}
