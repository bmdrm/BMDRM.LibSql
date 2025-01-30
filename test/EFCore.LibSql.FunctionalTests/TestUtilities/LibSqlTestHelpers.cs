// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.LibSql.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Provides a test store for use with LibSQL, implementing the required
/// setup and configuration for testing database-related functionalities.
/// </summary>
public class LibSqlTestHelpers : RelationalTestHelpers
{
    /// <summary>
    /// The test connection string used for the LibSQL database.
    /// </summary>
    private readonly string _testConnectionString = LibSqlTestSettings.ConnectionString ?? string.Empty;

    /// <summary>
    /// Initializes the static members of the <see cref="LibSqlTestHelpers"/> class.
    /// </summary>
    static LibSqlTestHelpers()
    {
        Instance = new LibSqlTestHelpers();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlTestHelpers"/> class.
    /// </summary>
    protected LibSqlTestHelpers()
    {
    }

    /// <summary>
    /// Gets or sets an instance of the <see cref="LibSqlTestHelpers"/> initialized with the given HTTP client factory.
    /// </summary>
    public static LibSqlTestHelpers Instance { get; set; }

    /// <summary>
    /// Adds the required LibSQL provider services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    /// <returns>The updated service collection.</returns>
    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkLibSql()
            .AddHttpClient();

    /// <summary>
    /// Configures the database context to use LibSQL as the provider.
    /// </summary>
    /// <param name="optionsBuilder">The options builder used to configure the DbContext.</param>
    /// <returns>The updated options builder.</returns>
    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
    {
        var extension = new LibSqlOptionsExtension(_testConnectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }

    /// <summary>
    /// Gets the logging definitions for LibSQL.
    /// </summary>
    public override LoggingDefinitions LoggingDefinitions { get; } = new LibSqlLoggingDefinitions();
}
