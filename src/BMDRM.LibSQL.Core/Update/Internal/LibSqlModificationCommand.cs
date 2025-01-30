// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlModificationCommand : ModificationCommand
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlModificationCommand(in ModificationCommandParameters modificationCommandParameters)
        : base(modificationCommandParameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlModificationCommand(in NonTrackedModificationCommandParameters modificationCommandParameters)
        : base(modificationCommandParameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ProcessSinglePropertyJsonUpdate(ref ColumnModificationParameters parameters)
    {
        var property = parameters.Property!;

        var propertyProviderClrType = (property.GetTypeMapping().Converter?.ProviderClrType ?? property.ClrType).UnwrapNullableType();

        // LibSql has no bool type, so if we simply sent the bool as-is, we'd get 1/0 in the JSON document.
        // To get an actual unquoted true/false value, we pass "true"/"false" string through the json() minifier, which does this.
        // See https://LibSql.org/forum/info/91d09974c3754ea6.
        // Here we convert the .NET bool to a "true"/"false" string, and LibSqlUpdateSqlGenerator will add the enclosing json().
        if (propertyProviderClrType == typeof(bool))
        {
            var value = property.GetTypeMapping().Converter is ValueConverter converter
                ? converter.ConvertToProvider(parameters.Value)
                : parameters.Value;

            parameters = parameters with
            {
                Value = value switch
                {
                    true => "true",
                    false => "false",
                    _ => throw new UnreachableException()
                }
            };

            return;
        }

#pragma warning disable EF1001 // Internal EF Core API usage.
        base.ProcessSinglePropertyJsonUpdate(ref parameters);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }
}
