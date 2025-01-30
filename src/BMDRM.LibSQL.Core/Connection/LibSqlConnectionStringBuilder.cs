// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Provides a simple way to create and manage LibSQL connection strings.
/// Connection string format: "http://example.com;token=your-token"
/// </summary>
public class LibSqlConnectionStringBuilder
{
    private string? _url;
    private string? _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlConnectionStringBuilder"/> class.
    /// </summary>
    public LibSqlConnectionStringBuilder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlConnectionStringBuilder"/> class with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public LibSqlConnectionStringBuilder(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        _url = connectionString;
    }

    /// <summary>
    /// Gets or sets the LibSQL server URL.
    /// </summary>
    public string? Url
    {
        get => _url;
        set => _url = value;
    }

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string? Token
    {
        get => _token;
        set => _token = value;
    }

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            if (string.IsNullOrEmpty(_url))
                return string.Empty;

            return _token == null ? _url : $"{_url};token={_token}";
        }
    }

    /// <summary>
    /// Creates a new connection string with the specified URL and optional token.
    /// </summary>
    public static string Create(string url, string? token = null)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));

        return token == null ? url : $"{url};token={token}";
    }

    /// <summary>
    /// Returns a connection string with sensitive information masked.
    /// </summary>
    public string ToDisplayString()
    {
        if (string.IsNullOrEmpty(_url))
            return string.Empty;

        return _token == null ? _url : $"{_url};token=***";
    }
}
