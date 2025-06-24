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
    public const int CommandTimeout = 30;

    public static LibSqlTestStore GetOrCreate(string name, IHttpClientFactory httpClientFactory, bool sharedCache = false)
        => new(name, httpClientFactory, sharedCache: sharedCache);

    public static async Task<LibSqlTestStore> GetOrCreateInitialized(string name, IHttpClientFactory httpClientFactory)
        => await new LibSqlTestStore(name, httpClientFactory, seed: true, sharedCache: false, shared: true).InitializeLibSqlAsync(
            new ServiceCollection().AddEntityFrameworkLibSql().BuildServiceProvider(validateScopes: true),
            (Func<DbContext>)null!,
            null!);
    public static LibSqlTestStore GetExisting(string name, IHttpClientFactory httpClientFactory)
        => new(name, httpClientFactory, seed: false, sharedCache: false, shared: true);

    public static LibSqlTestStore Create(string name, IHttpClientFactory httpClientFactory)
        => new(name, httpClientFactory, seed: true, sharedCache: false, shared: false);

    private readonly bool _seed;

    public LibSqlTestStore(string name, IHttpClientFactory httpClientFactory, bool sharedCache = false)
        : base(name, shared: true, CreateConnection( LibSqlTestSettings.ConnectionString, httpClientFactory))
    {
        _seed = true;
        _httpClientFactory = httpClientFactory;
        _testConnectionString = LibSqlTestSettings.ConnectionString;
    }

    private LibSqlTestStore(string name, IHttpClientFactory httpClientFactory, bool seed, bool sharedCache, bool shared)
        : base(name, shared, CreateConnection( LibSqlTestSettings.ConnectionString, httpClientFactory))
    {
        _seed = seed;
        _httpClientFactory = httpClientFactory;
        _testConnectionString = LibSqlTestSettings.ConnectionString;
    }

    private static HttpDbConnection CreateConnection(string connectionString, IHttpClientFactory httpClientFactory)
    {
        return new HttpDbConnection(connectionString, httpClientFactory);
    }
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

    public async Task<LibSqlTestStore> InitializeLibSqlAsync(
        IServiceProvider? serviceProvider,
        Func<DbContext>? createContext,
        Func<DbContext, Task>? seed)
        => (LibSqlTestStore) await InitializeAsync(serviceProvider, createContext, seed);

    public async Task<LibSqlTestStore> InitializeLibSqlAsync(
        IServiceProvider serviceProvider,
        Func<LibSqlTestStore, DbContext> createContext,
        Func<DbContext, Task> seed)
        => (LibSqlTestStore)await InitializeAsync(serviceProvider, () => createContext(this), seed);

    protected override async Task InitializeAsync(Func<DbContext> createContext, Func<DbContext, Task>? seed, Func<DbContext, Task>? clean)
    {
        if (!_seed)
        {
            return;
        }

        using var context = createContext();
        if (!await context.Database.EnsureCreatedResilientlyAsync())
        {
            if (clean != null)
            {
                await clean(context);
            }

            await CleanAsync(context);

            // Run context seeding
            await context.Database.EnsureCreatedResilientlyAsync();
        }

        if (seed != null)
        {
            await seed(context);
        }
    }

    public override Task CleanAsync(DbContext context)
    {
        context.Database.EnsureDeleted();
        return Task.CompletedTask;
    }

    public int ExecuteNonQuery(string sql, params object[] parameters)
    {
        using var command = CreateCommand(sql, parameters);
        return command.ExecuteNonQuery();
    }

    public T ExecuteScalar<T>(string sql, params object[] parameters)
    {
        using var command = CreateCommand(sql, parameters);
        return (T)command.ExecuteScalar()!;
    }

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
