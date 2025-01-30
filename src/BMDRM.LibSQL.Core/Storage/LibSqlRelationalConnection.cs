// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.LibSql.Storage;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents a relational database connection specifically tailored for LibSQL.
/// </summary>
public class LibSqlRelationalConnection : LibSqlConnection, ILibSqlRelationalConnection
{
#pragma warning restore EF1001 // Internal EF Core API usage.

    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Infrastructure>? _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LibSqlConnectionStringBuilder _connectionStringBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlRelationalConnection"/> class.
    /// </summary>
    public LibSqlRelationalConnection(
        RelationalConnectionDependencies dependencies,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IDiagnosticsLogger<DbLoggerCategory.Infrastructure>? logger,
        IHttpClientFactory httpClientFactory)
        : base(dependencies, httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.Logger.LogInformation("Initializing LibSqlRelationalConnection...");
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
        _httpClientFactory = httpClientFactory;
        _connectionStringBuilder = new LibSqlConnectionStringBuilder(ConnectionString!);

        var optionsExtension = dependencies.ContextOptions.Extensions.OfType<LibSqlOptionsExtension>().FirstOrDefault();
        if (optionsExtension != null)
        {
            _logger.Logger.LogInformation("LibSqlOptionsExtension found. Loading options...");
        }
    }

    /// <summary>
    /// Gets the connection string builder for this connection.
    /// </summary>
    public virtual LibSqlConnectionStringBuilder ConnectionStringBuilder => _connectionStringBuilder;

    /// <summary>
    /// Creates a new database connection specific to LibSQL.
    /// </summary>
    protected override DbConnection CreateDbConnection()
    {
        _logger?.Logger.LogInformation("Creating a new database connection...");
        var connection = new HttpDbConnection(GetValidatedConnectionString(), _httpClientFactory);
        _logger?.Logger.LogInformation(GetDisplayConnectionString());
        InitializeDbConnection(connection);
        _logger?.Logger.LogInformation("Database connection created successfully.");
        return connection;
    }

    /// <summary>
    /// Gets a display-safe version of the connection string (with API key masked).
    /// </summary>
    protected  string GetDisplayConnectionString()
        => _connectionStringBuilder.ToDisplayString();

    /// <summary>
    /// Creates a read-only connection to the database.
    /// </summary>
    public virtual ILibSqlRelationalConnection CreateReadOnlyConnection()
    {
        _logger?.Logger.LogInformation("Creating a read-only database connection...");

        // For LibSQL, we'll append a read-only flag to the URL if supported
        var readOnlyBuilder = new LibSqlConnectionStringBuilder(GetValidatedConnectionString());
        if (!readOnlyBuilder.Url?.Contains("?", StringComparison.Ordinal) ?? false)
        {
            readOnlyBuilder.Url += "?mode=ro";
        }
        else
        {
            readOnlyBuilder.Url += "&mode=ro";
        }

        var contextOptions = new DbContextOptionsBuilder()
            .UseLibSql(readOnlyBuilder.ConnectionString)
            .Options;

        _logger?.Logger.LogInformation("Read-only connection created.");
        return new LibSqlRelationalConnection(
            Dependencies with { ContextOptions = contextOptions },
            _rawSqlCommandBuilder,
            _logger,
            _httpClientFactory);
    }

    /// <summary>
    /// Initializes the database connection with custom functions.
    /// </summary>
    private void InitializeDbConnection(DbConnection connection)
    {
        _logger?.Logger.LogInformation("Initializing database connection...");

        if (connection is HttpDbConnection httpConnection)
        {
            _logger?.Logger.LogInformation("Connection is of type HttpDbConnection. Setting up custom functions...");

            httpConnection.CreateFunction<string, string, bool>(
                "regexp",
                (pattern, input) => input != null && Regex.IsMatch(input, pattern ?? ""),
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "sqrt",
                value => value.HasValue ? Math.Sqrt(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?, double?>(
                "power",
                (value, power) => value.HasValue && power.HasValue ? Math.Pow(value.Value, power.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "sin",
                value => value.HasValue ? Math.Sin(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "cos",
                value => value.HasValue ? Math.Cos(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "tan",
                value => value.HasValue ? Math.Tan(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "asin",
                value => value.HasValue ? Math.Asin(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "acos",
                value => value.HasValue ? Math.Acos(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "atan",
                value => value.HasValue ? Math.Atan(value.Value) : null,
                isDeterministic: true);

            httpConnection.CreateFunction<double?, double?>(
                "abs",
                value => value.HasValue ? Math.Abs(value.Value) : null,
                isDeterministic: true);

            _logger?.Logger.LogInformation("Custom functions initialized successfully.");
        }
        else
        {
            _logger?.Logger.LogInformation("Connection is not of type HttpDbConnection.");
        }
        _logger?.Logger.LogInformation("Database connection initialization completed.");
    }
}
