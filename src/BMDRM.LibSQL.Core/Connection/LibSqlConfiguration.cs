// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Configuration options for LibSQL connection.
/// </summary>
public class LibSqlConfiguration
{
    /// <summary>
    /// Gets or sets the base URI for the LibSQL server.
    /// </summary>
    public string BaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
