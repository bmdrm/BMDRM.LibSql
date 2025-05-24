// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.LibSql;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Provides extension methods to configure LibSQL as a database provider for Entity Framework Core.
/// </summary>
public static class LibSqlExtensions
{
    /// <summary>
    /// Configures the context to connect to a LibSQL database, but without initially setting any
    /// <see cref="DbConnection"/> or connection string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The connection or connection string must be set before the <see cref="DbContext"/> is used to connect
    /// to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection"/>.
    /// Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString"/>.
    /// </para>
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSQL specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
    {

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));

        ConfigureWarnings(optionsBuilder);

        libSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the context to connect to a LibSQL database.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSQL specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
    {

        var extension = (LibSqlOptionsExtension)GetOrCreateExtension(optionsBuilder, connectionString).WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        ConfigureWarnings(optionsBuilder);

        libSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the context to connect to a LibSQL database using a DbConnection.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">An existing <see cref="DbConnection"/> to be used to connect to the database.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSQL specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        HttpDbConnection connection,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
    {
        var extension = (LibSqlOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connection.ConnectionString);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        ConfigureWarnings(optionsBuilder);

        libSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    /// Creates or gets the extension for LibSQL.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The builder connection string of the dbcontext.</param>
    /// <returns>The LibSQL options extension.</returns>
    private static LibSqlOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder, string? connectionString = null)
    {

        var extension = optionsBuilder.Options.FindExtension<LibSqlOptionsExtension>();
        if (extension == null && !string.IsNullOrEmpty(connectionString))
        {
            return new LibSqlOptionsExtension(connectionString);
        }

        return extension ?? new LibSqlOptionsExtension(connectionString ?? string.Empty);
    }

    /// <summary>
    /// Configures warnings for the context.
    /// </summary>
    /// <param name="optionsBuilder">The options builder being configured.</param>
    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning));
    }
}
