// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var sqlExpressionFactory = (LibSqlExpressionFactory)dependencies.SqlExpressionFactory;

        AddTranslators(
            new IMethodCallTranslator[]
            {
                new LibSqlByteArrayMethodTranslator(sqlExpressionFactory),
                new LibSqlCharMethodTranslator(sqlExpressionFactory),
                new LibSqlDateOnlyMethodTranslator(sqlExpressionFactory),
                new LibSqlDateTimeMethodTranslator(sqlExpressionFactory),
                new LibSqlGlobMethodTranslator(sqlExpressionFactory),
                new LibSqlHexMethodTranslator(sqlExpressionFactory),
                new LibSqlMathTranslator(sqlExpressionFactory),
                new LibSqlObjectToStringTranslator(sqlExpressionFactory),
                new LibSqlRandomTranslator(sqlExpressionFactory),
                new LibSqlRegexMethodTranslator(sqlExpressionFactory),
                new LibSqlStringMethodTranslator(sqlExpressionFactory),
                new LibSqlSubstrMethodTranslator(sqlExpressionFactory)
            });
    }
}
