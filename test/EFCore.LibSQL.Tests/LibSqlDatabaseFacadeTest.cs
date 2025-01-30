// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore;

public class LibSqlDatabaseFacadeTest
{
    [ConditionalFact]
    public void IsLibSql_when_using_LibSql()
    {
        var services = new ServiceCollection()
            .AddHttpClient()
            .AddEntityFrameworkLibSql();

        using var context = new ProviderContext(
            new DbContextOptionsBuilder()
                .UseInternalServiceProvider(services.BuildServiceProvider(validateScopes: true))
                .UseLibSql(LibSqlTestSettings.ConnectionString).Options);
        Assert.True(typeof(LibSqlOptionsExtension).Assembly.GetName().Name == context.Database.ProviderName);
    }

    [ConditionalFact]
    public void Not_IsLibSql_when_using_different_provider()
    {
        using var context = new ProviderContext(
            new DbContextOptionsBuilder()
                .UseInternalServiceProvider(InMemoryFixture.DefaultServiceProvider)
                .UseInMemoryDatabase("Maltesers").Options);
        Assert.False(context.Database.ProviderName == typeof(LibSqlOptionsExtension).Assembly.GetName().Name);
    }

    private class ProviderContext : DbContext
    {
        public ProviderContext(DbContextOptions options)
            : base(options)
        {
        }
    }
}
