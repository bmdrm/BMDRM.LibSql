// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.LibSql.Design.Internal;
using Microsoft.EntityFrameworkCore.LibSql.Scaffolding.Internal;

[assembly: DesignTimeProviderServices("Microsoft.EntityFrameworkCore.LibSql.Design.LibSqlDesignTimeServices")]

namespace Microsoft.EntityFrameworkCore.LibSql.Design;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using Microsoft.EntityFrameworkCore.LibSql.Scaffolding;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configures design-time services for LibSQL.
/// </summary>
public class LibSqlDesignTimeServices : IDesignTimeServices
{
    /// <summary>
    /// Configures design-time services for LibSQL.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure.</param>
    public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddEntityFrameworkLibSql();

#pragma warning disable EF1001 // Internal EF Core API usage.
        new EntityFrameworkRelationalDesignServicesBuilder(serviceCollection)
            .TryAdd<ICSharpRuntimeAnnotationCodeGenerator, LibSqlCSharpRuntimeAnnotationCodeGenerator>()
#pragma warning restore EF1001 // Internal EF Core API usage.
            .TryAdd<IDatabaseModelFactory, LibSqlDatabaseModelFactory>()
            .TryAdd<IProviderConfigurationCodeGenerator, LibSqlCodeGenerator>()
            .TryAddCoreServices();
        serviceCollection.AddScoped<HttpDbConnection>();
        serviceCollection.AddScoped<HttpDbCommand>();
    }
}
