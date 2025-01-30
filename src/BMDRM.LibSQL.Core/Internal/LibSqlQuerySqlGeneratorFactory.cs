// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Query.Internal;

namespace Microsoft.EntityFrameworkCore.LibSql.Internal;

/// <summary>
/// Custom factory for generating SQL query generators for the LibSQL database
/// that uses HTTP requests for executing SQL commands.
/// </summary>
public class LibSqlQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{

    /// <summary>
    /// Initializes the factory with necessary dependencies.
    /// </summary>
    /// <param name="dependencies">The dependencies for generating SQL queries.</param>
    public LibSqlQuerySqlGeneratorFactory(
        QuerySqlGeneratorDependencies dependencies)
    {
        Dependencies = dependencies;
    }

    /// <summary>
    /// Relational provider-specific dependencies for this service.
    /// </summary>
    protected virtual QuerySqlGeneratorDependencies Dependencies { get; }

    /// <summary>
    /// Creates a new instance of the SQL query generator that is specific for LibSQL using HTTP.
    /// </summary>
    /// <returns>A new instance of <see cref="QuerySqlGenerator"/>.</returns>
    public virtual QuerySqlGenerator Create()
    {
        return new LibSqlQuerySqlGenerator(Dependencies);
    }
}
