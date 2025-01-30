// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.EntityFrameworkCore.TestUtilities;

using Microsoft.EntityFrameworkCore.LibSql.Connection;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.LibSql;

public sealed class LibSqlTestStore : RelationalTestStore
{
    private readonly string _testConnectionString;
    private readonly IHttpClientFactory _httpClientFactory;

    public LibSqlTestStore(string name, IHttpClientFactory httpClientFactory, bool sharedCache = false)
        : base(name, shared: true)
    {
        _seed = true;
        _httpClientFactory = httpClientFactory;
        _testConnectionString = LibSqlTestSettings.ConnectionString;
        Connection = new HttpDbConnection(_testConnectionString, _httpClientFactory);
    }

    private LibSqlTestStore(string name, IHttpClientFactory httpClientFactory, bool seed, bool sharedCache, bool shared)
        : base(name, shared)
    {
        _seed = seed;
        _httpClientFactory = httpClientFactory;
        _testConnectionString = LibSqlTestSettings.ConnectionString;
        Connection = new HttpDbConnection(_testConnectionString, _httpClientFactory);
    }

    public const int CommandTimeout = 30;

    public static LibSqlTestStore GetOrCreate(string name, IHttpClientFactory httpClientFactory, bool sharedCache = false)
        => new(name, httpClientFactory, sharedCache: sharedCache);

    public static LibSqlTestStore GetOrCreateInitialized(string name, IHttpClientFactory httpClientFactory)
        => new LibSqlTestStore(name, httpClientFactory, seed: true, sharedCache: false, shared: true).Initialize(
            new ServiceCollection().AddEntityFrameworkLibSql().BuildServiceProvider(validateScopes: true),
            (Func<DbContext>)null!,
            null!);

    public static LibSqlTestStore GetExisting(string name, IHttpClientFactory httpClientFactory)
        => new(name, httpClientFactory, seed: false, sharedCache: false, shared: true);

    public static LibSqlTestStore Create(string name, IHttpClientFactory httpClientFactory)
        => new(name, httpClientFactory, seed: true, sharedCache: false, shared: false);

    private readonly bool _seed;

    public DbContextOptionsBuilder AddProviderOptions(
        DbContextOptionsBuilder builder,
        Action<LibSqlDbContextOptionsBuilder> configureLibSql)
        => builder.UseLibSql(
            (HttpDbConnection)Connection, b =>
            {
                b.SetTimeout(CommandTimeout);
                configureLibSql?.Invoke(b);
            });

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        => AddProviderOptions(builder, configureLibSql: null!);

    public LibSqlTestStore Initialize<TContext>(
        IServiceProvider serviceProvider,
        Func<TContext> createContext,
        Action<TContext> seed,
        Action<TContext> clean = null!) where TContext : DbContext
    {
        if (createContext != null)
        {
            using var context = createContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (_seed && seed != null)
            {
                seed(context);
            }
        }
        return this;
    }

    public override LibSqlTestStore Initialize(
        IServiceProvider serviceProvider,
        Func<DbContext> createContext,
        Action<DbContext> seed,
        Action<DbContext> clean = null!)
    {
        if (createContext != null)
        {
            using var context = createContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (_seed && seed != null)
            {
                seed(context);
            }
        }
        return this;
    }

    public LibSqlTestStore Initialize<TContext>(
        IServiceProvider serviceProvider,
        Func<LibSqlTestStore, TContext> createContext,
        Action<TContext> seed) where TContext : DbContext
        => Initialize(serviceProvider, () => createContext(this), seed, clean: null!);

    public TContext CreateContext<TContext>(IServiceProvider serviceProvider) where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        AddProviderOptions(optionsBuilder);

        return (TContext)ActivatorUtilities.CreateInstance(
            serviceProvider,
            typeof(TContext),
            new object[] { optionsBuilder.Options });
    }

    protected override void Initialize(Func<DbContext> createContext, Action<DbContext> seed, Action<DbContext> clean)
    {
        if (createContext != null)
        {
            using var context = createContext();
            var dbContext = context as DbContext;
            if (dbContext == null)
            {
                throw new InvalidOperationException($"Context must be derived from {nameof(DbContext)}");
            }

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            if (_seed && seed != null)
            {
                seed(dbContext);
            }
        }
    }

    public override void Clean(DbContext context)
        => context.Database.EnsureDeleted();

    private DbCommand CreateCommand(string commandText, object[] parameters)
    {
        var command = Connection.CreateCommand();
        command.CommandText = commandText;

        if (parameters != null)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                command.Parameters.Add(parameters[i]);
            }
        }

        return command;
    }
}
