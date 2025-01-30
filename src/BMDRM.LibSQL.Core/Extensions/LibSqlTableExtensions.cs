// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     LibSql specific extension methods for <see cref="ITable" />.
/// </summary>
public static class LibSqlTableExtensions
{
    /// <summary>
    ///     Returns a value indicating whether to use the SQL RETURNING clause when saving changes to the table.
    ///     The RETURNING clause is incompatible with certain LibSql features, such as virtual tables or tables with AFTER triggers.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <returns><see langword="true" /> if the SQL RETURNING clause is used to save changes to the table.</returns>
    public static bool IsSqlReturningClauseUsed(this ITable table)
    {
        if (table.FindRuntimeAnnotation(LibSqlAnnotationNames.UseSqlReturningClause) is { Value: bool isSqlOutputClauseUsed })
        {
            return isSqlOutputClauseUsed;
        }

        isSqlOutputClauseUsed = table.EntityTypeMappings.All(
            e => ((IEntityType)e.TypeBase).IsSqlReturningClauseUsed(StoreObjectIdentifier.Table(table.Name, table.Schema)));

        table.SetRuntimeAnnotation(LibSqlAnnotationNames.UseSqlReturningClause, isSqlOutputClauseUsed);

        return isSqlOutputClauseUsed;
    }
}
