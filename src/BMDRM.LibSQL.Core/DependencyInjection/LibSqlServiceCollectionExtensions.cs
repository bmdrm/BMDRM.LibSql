// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Metadata.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Migrations.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Storage.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Update.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using Microsoft.EntityFrameworkCore.LibSql.Helpers;
using Microsoft.EntityFrameworkCore.LibSql.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Storage;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Query.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;

#pragma warning disable EF1001 // Internal EF Core API usage.

/// <summary>
/// LibSql specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class LibSqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers the given Entity Framework <see cref="DbContext"/> as a service in the <see cref="IServiceCollection"/>
    /// and configures it to connect to a LibSql database.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="optionsAction">Optional action to configure DbContextOptions.</param>
    /// <returns>The same service collection to allow method chaining.</returns>
    public static IServiceCollection AddLibSql<TContext>(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
    {
        // Registers the DbContext with the connection string.
        return serviceCollection.AddDbContext<TContext>(
            (_, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseLibSql(connectionString);
            });
    }

    /// <summary>
    /// Configures LibSQL connection settings.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="baseUri">The LibSQL base URI.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <returns>The same service collection to allow method chaining.</returns>
    public static IServiceCollection ConfigureLibSql(
        this IServiceCollection serviceCollection,
        string baseUri,
        string apiKey)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentException.ThrowIfNullOrEmpty(baseUri);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        serviceCollection.Configure<LibSqlConfiguration>(options =>
        {
            options.BaseUri = baseUri;
            options.ApiKey = apiKey;
        });

        serviceCollection.AddHttpClient();
        serviceCollection.AddHttpClient(
            "LibSQLClient",
            (provider, client) =>
            {
                var config = provider.GetRequiredService<IOptions<LibSqlConfiguration>>().Value;
                client.BaseAddress = new Uri(config.BaseUri);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            });

        serviceCollection.AddScoped<HttpDbConnection>();
        serviceCollection.AddScoped<HttpDbCommand>();

        return serviceCollection;
    }

    /// <summary>
    /// Adds the services required by the LibSql database provider for Entity Framework
    /// to an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection to allow method chaining.</returns>
    public static IServiceCollection AddEntityFrameworkLibSql(
        this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, LibSqlLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<LibSqlOptionsExtension>>()
            .TryAdd<IRelationalTypeMappingSource, LibSqlTypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, LibSqlSqlGenerationHelper>()
            .TryAdd<IRelationalAnnotationProvider, LibSqlAnnotationProvider>()
            .TryAdd<IModelValidator, LibSqlModelValidator>()
            .TryAdd<IProviderConventionSetBuilder, LibSqlConventionSetBuilder>()
            .TryAdd<IModificationCommandBatchFactory, LibSqlModificationCommandBatchFactory>()
            .TryAdd<IModificationCommandFactory, LibSqlModificationCommandFactory>()
            .TryAdd<IRelationalConnection>(p => p.GetRequiredService<ILibSqlRelationalConnection>())
            .TryAdd<IMigrationsSqlGenerator, LibSqlMigrationsSqlGenerator>()
            .TryAdd<IRelationalDatabaseCreator, LibSqlDatabaseCreator>()
            .TryAdd<IHistoryRepository, LibSqlHistoryRepository>()
            .TryAdd<IRelationalQueryStringFactory, LibSqlQueryStringFactory>()
            .TryAdd<IMethodCallTranslatorProvider, LibSqlMethodCallTranslatorProvider>()
            .TryAdd<IAggregateMethodCallTranslatorProvider, LibSqlAggregateMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, LibSqlMemberTranslatorProvider>()
            .TryAdd<IQuerySqlGeneratorFactory, LibSqlQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalConnection, LibSqlConnection>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, LibSqlQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, LibSqlSqlTranslatingExpressionVisitorFactory>()
            .TryAdd<IQueryTranslationPostprocessorFactory, LibSqlQueryTranslationPostprocessorFactory>()
            .TryAdd<IUpdateSqlGenerator, LibSqlUpdateSqlGenerator>()
            .TryAdd<ISqlExpressionFactory, LibSqlExpressionFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, LibSqlParameterBasedSqlProcessorFactory>()
            .TryAddProviderSpecificServices(
                b =>
                {
                    b.TryAddScoped<ILibSqlRelationalConnection, LibSqlRelationalConnection>();
                });

        builder.TryAddCoreServices();

        return serviceCollection;
    }
}
