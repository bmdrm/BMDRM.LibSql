// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Microsoft.EntityFrameworkCore.LibSql.Storage.Json.Internal;

/// <summary>
///     The LibSql-specific JsonValueReaderWrite for byte[]. Generates the LibSql representation (e.g. X'0102') rather than base64, in order
///     to match our LibSql non-JSON representation.
/// </summary>
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public sealed class LibSqlJsonByteArrayReaderWriter : JsonValueReaderWriter<byte[]>
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static LibSqlJsonByteArrayReaderWriter Instance { get; } = new();

    private LibSqlJsonByteArrayReaderWriter()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override byte[] FromJsonTyped(ref Utf8JsonReaderManager manager, object? existingObject = null)
        => Convert.FromHexString(manager.CurrentReader.GetString()!);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void ToJsonTyped(Utf8JsonWriter writer, byte[] value)
        => writer.WriteStringValue(Convert.ToHexString(value));
}
