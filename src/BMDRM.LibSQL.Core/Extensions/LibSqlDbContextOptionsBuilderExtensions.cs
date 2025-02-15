// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     LibSql specific extension methods for <see cref="DbContextOptionsBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
/// </remarks>
public static class LibSqlDbContextOptionsBuilderExtensions
{
    /// <summary>
    ///     Configures the context to connect to a LibSql database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The connection or connection string must be set before the <see cref="DbContext" /> is used to connect
    ///         to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />.
    ///         Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///         <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));

        ConfigureWarnings(optionsBuilder);

        LibSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
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
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed. The caller owns the connection and is
    ///     responsible for its disposal.
    /// </param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
        => UseLibSql(optionsBuilder, connection, false, LibSqlOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed.
    /// </param>
    /// <param name="contextOwnsConnection">
    ///     If <see langword="true" />, then EF will take ownership of the connection and will
    ///     dispose it in the same way it would dispose a connection created by EF. If <see langword="false" />, then the caller still
    ///     owns the connection and is responsible for its disposal.
    /// </param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
    {
        Check.NotNull(connection, nameof(connection));

        var extension = (LibSqlOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection, contextOwnsConnection);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        ConfigureWarnings(optionsBuilder);

        LibSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to a LibSql database, but without initially setting any
    ///     <see cref="DbConnection" /> or connection string.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The connection or connection string must be set before the <see cref="DbContext" /> is used to connect
    ///         to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />.
    ///         Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///         <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseLibSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseLibSql(
            (DbContextOptionsBuilder)optionsBuilder, LibSqlOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseLibSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseLibSql(
            (DbContextOptionsBuilder)optionsBuilder, connectionString, libSqlOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed. The caller owns the connection and is
    ///     responsible for its disposal.
    /// </param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseLibSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseLibSql(
            (DbContextOptionsBuilder)optionsBuilder, connection, LibSqlOptionsAction);

    /// <summary>
    ///     Configures the context to connect to a LibSql database.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///     <see href="https://aka.ms/efcore-docs-LibSql">Accessing LibSql databases with EF Core</see> for more information and examples.
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed.
    /// </param>
    /// <param name="contextOwnsConnection">
    ///     If <see langword="true" />, then EF will take ownership of the connection and will
    ///     dispose it in the same way it would dispose a connection created by EF. If <see langword="false" />, then the caller still
    ///     owns the connection and is responsible for its disposal.
    /// </param>
    /// <param name="LibSqlOptionsAction">An optional action to allow additional LibSql specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseLibSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<LibSqlDbContextOptionsBuilder>? LibSqlOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseLibSql(
            (DbContextOptionsBuilder)optionsBuilder, connection, contextOwnsConnection, LibSqlOptionsAction);

    private static LibSqlOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder options, string connectionString = "")
        => options.Options.FindExtension<LibSqlOptionsExtension>()
            ?? new LibSqlOptionsExtension(connectionString);

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        var coreOptionsExtension
            = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        coreOptionsExtension = RelationalOptionsExtension.WithDefaultWarningConfiguration(coreOptionsExtension);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
}
