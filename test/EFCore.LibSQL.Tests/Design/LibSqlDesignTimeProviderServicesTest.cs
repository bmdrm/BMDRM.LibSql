// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.LibSql.Design;

/// <inheritdoc/>
public class LibSqlDesignTimeProviderServicesTest : DesignTimeProviderServicesTest
{
    /// <inheritdoc/>
    protected override Assembly GetRuntimeAssembly()
        => typeof(LibSqlDesignTimeServices).Assembly;

    /// <inheritdoc/>
    protected override Type GetDesignTimeServicesType()
        => typeof(LibSqlDesignTimeServices);
}
