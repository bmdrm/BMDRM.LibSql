// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Builder;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Custom implementation of AnnotationCodeGenerator to generate annotations for a custom provider.
/// </summary>
public class LibSqlAnnotationCodeGenerator : AnnotationCodeGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlAnnotationCodeGenerator"/> class.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    public LibSqlAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }
}

/// <summary>
///     Used to generate code for migrations.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see>, and
///     <see href="https://aka.ms/efcore-docs-design-time-services">EF Core design-time services</see> for more information and examples.
/// </remarks>
public class LibSqlMigrationsCodeGenerator : LibSqlMigrationsSqlGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlMigrationsCodeGenerator"/> class.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    /// <param name="migrationsAnnotations">Provider-specific Migrations annotations to use.</param>
    public LibSqlMigrationsCodeGenerator(MigrationsSqlGeneratorDependencies dependencies, IRelationalAnnotationProvider migrationsAnnotations)
        : base(dependencies, migrationsAnnotations)
    {
    }
}
