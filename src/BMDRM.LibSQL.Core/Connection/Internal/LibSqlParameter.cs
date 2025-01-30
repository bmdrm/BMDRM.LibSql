// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Connection.Internal;

internal class LibSqlParameter
{
    public string name { get; set; } = "";
    public string type { get; set; } = "";
    public object? value { get; set; }
}
