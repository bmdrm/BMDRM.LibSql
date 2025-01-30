// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestUtilities;

/// <summary>
/// Provides configuration settings for LibSQL test utilities.
/// </summary>
public static class LibSqlTestSettings
{
    /// <summary>
    /// The connection string used for connecting to the LibSQL test database.
    /// </summary>
    /// <remarks>
    /// The connection string can be provided through the LIBSQL_TEST_CONNECTION environment variable.
    /// If not provided, it will fall back to the default test database connection string.
    /// </remarks>
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("LIBSQL_TEST_CONNECTION") ?? 
        "https://test-db.turso.io/v2/pipeline;token=your-jwt-token";
}
