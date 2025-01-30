// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore;

public class DbSetAsTableNameLibSqlTest : DbSetAsTableNameTest
{
    private static readonly IServiceProvider _serviceProvider;

    static DbSetAsTableNameLibSqlTest()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddEntityFrameworkLibSql();
        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
    }

    protected override string GetTableName<TEntity>(DbContext context)
        => context.Model.FindEntityType(typeof(TEntity)).GetTableName();

    protected override string GetTableName<TEntity>(DbContext context, string entityTypeName)
        => context.Model.FindEntityType(entityTypeName).GetTableName();

    protected override SetsContext CreateContext()
        => new LibSqlSetsContext(_serviceProvider);

    protected override SetsContext CreateNamedTablesContext()
        => new LibSqlNamedTablesContextContext(_serviceProvider);

    protected class LibSqlSetsContext : SetsContext
    {
        private readonly IServiceProvider _serviceProvider;

        public LibSqlSetsContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInternalServiceProvider(_serviceProvider)
                .UseLibSql(LibSqlTestSettings.ConnectionString);
        }
    }

    protected class LibSqlNamedTablesContextContext : NamedTablesContext
    {
        private readonly IServiceProvider _serviceProvider;

        public LibSqlNamedTablesContextContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInternalServiceProvider(_serviceProvider)
                .UseLibSql(LibSqlTestSettings.ConnectionString);
        }
    }
}
