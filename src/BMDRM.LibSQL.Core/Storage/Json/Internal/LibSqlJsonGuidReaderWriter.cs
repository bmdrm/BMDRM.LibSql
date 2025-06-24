// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.LibSql.Storage.Json.Internal;

/// <summary>
///     The LibSql-specific JsonValueReaderWrite for GUID. Generates an upper-case representation instead of the more standard lower-case
///     one, in order to match our LibSql non-JSON representation.
/// </summary>
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public sealed class LibSqlJsonGuidReaderWriter : JsonValueReaderWriter<Guid>
{
    private static readonly PropertyInfo InstanceProperty = typeof(LibSqlJsonGuidReaderWriter).GetProperty(nameof(Instance))!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static LibSqlJsonGuidReaderWriter Instance { get; } = new();

    private LibSqlJsonGuidReaderWriter()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Guid FromJsonTyped(ref Utf8JsonReaderManager manager, object? existingObject = null)
        => manager.CurrentReader.GetGuid();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void ToJsonTyped(Utf8JsonWriter writer, Guid value)
        => writer.WriteStringValue(value.ToString().ToUpperInvariant());

    /// <summary>
    ///     The expression representing construction of this object.
    /// </summary>
    public override Expression ConstructorExpression
        => Expression.Property(null, InstanceProperty);
}
