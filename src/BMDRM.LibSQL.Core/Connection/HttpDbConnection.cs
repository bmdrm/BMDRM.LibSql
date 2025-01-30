// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents an HTTP-based database connection for interacting with a database over HTTP.
/// This class provides basic functionality for opening, closing, and managing connection state.
/// </summary>
public class HttpDbConnection : DbConnection
{
    private readonly IHttpClientFactory _httpClientFactory;
    private ConnectionState _state = ConnectionState.Closed;
    private string _connectionString;
    private HttpClient? _httpClient;
    /// <summary>
    /// Default timeout
    /// </summary>
    public int DefaultTimeout { get; set; } = 30;

    private readonly Dictionary<string, ILibSqlFunction> _customFunctions = new();

    private HttpDbTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbConnection"/> class with the specified connection string.
    /// </summary>
    /// <param name="connectionString">The connection string used to configure the connection.</param>
    /// <param name="httpClientFactory">The factory for creating HttpClient instances.</param>
    public HttpDbConnection(string connectionString, IHttpClientFactory httpClientFactory)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        ConnectionString = _connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbConnection"/> class with no parameters.
    /// </summary>
    public HttpDbConnection()
    {
        _connectionString = string.Empty;
        _httpClientFactory = default!;
    }

    /// <summary>
    /// Gets or sets the connection string for the database connection.
    /// </summary>
    /// <value>The connection string for the database connection.</value>
    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        [MemberNotNull(nameof(_connectionString))]
        set => _connectionString = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the name of the database to which the connection is made.
    /// </summary>
    /// <value>The name of the database, which is set to "LibSQL" in this implementation.</value>
    public override string Database => "LibSQL";

    /// <summary>
    /// Gets the name of the data source for the connection.
    /// </summary>
    /// <value>The name of the data source, which is set to "LibSQL HTTP API" in this implementation.</value>
    public override string DataSource => "LibSQL HTTP API";

    /// <summary>
    /// Gets the version of the database server.
    /// </summary>
    /// <value>The version of the server.</value>
    public override string ServerVersion => "2";

    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    /// <value>The state of the connection.</value>
    public override ConnectionState State => _state;

    /// <summary>
    /// Opens the connection to the database.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the connection is already open.</exception>
    public override void Open()
    {
        if (_state != ConnectionState.Closed)
        {
            return;
        }

        try
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_connectionString);
            _state = ConnectionState.Open;
        }
        catch (Exception ex)
        {
            _state = ConnectionState.Closed;
            throw new InvalidOperationException($"Failed to open connection: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously opens the connection to the database.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection is already open.</exception>
    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_state != ConnectionState.Closed)
        {
            return;
        }

        try
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_connectionString);
            _state = ConnectionState.Open;
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _state = ConnectionState.Closed;
            throw new InvalidOperationException($"Failed to open connection: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Closes the connection to the database.
    /// </summary>
    public override void Close()
    {
        if (_currentTransaction != null)
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
        if (_state == ConnectionState.Closed)
        {
            return;
        }

        _httpClient?.Dispose();
        _httpClient = null;
        _state = ConnectionState.Closed;
    }

    /// <summary>
    /// Creates a new instance of a <see cref="DbCommand"/> object.
    /// </summary>
    /// <returns>A new instance of a <see cref="DbCommand"/> object.</returns>
    protected override DbCommand CreateDbCommand()
    {
        var command = new HttpDbCommand(this, _currentTransaction);
        if (DefaultTimeout > 0)
        {
            command.CommandTimeout = DefaultTimeout;
        }
        return command;
    }

    /// <summary>
    /// Changes the database for the current connection.
    /// </summary>
    /// <param name="databaseName">The name of the database to switch to.</param>
    /// <exception cref="NotSupportedException">Thrown because changing the database is not supported in this connection type.</exception>
    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("Changing the database is not supported.");
    }

    /// <summary>
    /// Begins a database transaction with the specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <returns>A new <see cref="DbTransaction"/>.</returns>

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        _currentTransaction?.Dispose();
        var mode = "close";
        _currentTransaction = new HttpDbTransaction(this, isolationLevel, mode);
        return _currentTransaction;
    }


    /// <summary>
    /// Disposes of the resources used by the <see cref="HttpDbConnection"/> class.
    /// </summary>
    /// <param name="disposing">A flag indicating whether to dispose of managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
            Close();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// EnlistTransaction
    /// </summary>
    /// <param name="transaction">Transaction </param>
    public override void EnlistTransaction(System.Transactions.Transaction? transaction) {}

    internal HttpClient GetClient()
    {
        if (_state != ConnectionState.Open)
        {
            Open();
        }

        var connectionData = ConnectionString.Split(';');
        if (connectionData.Length < 2)
        {
            throw new InvalidOperationException("Connection string must include both base address and Bearer token.");
        }

        var baseAddress = connectionData[0].TrimEnd('/');
        var bearerToken = connectionData[1];

        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            throw new InvalidOperationException("Base address cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new InvalidOperationException("Bearer token cannot be null or empty.");
        }

        _httpClient = _httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(baseAddress);

        // Add Bearer token to the Authorization header
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return _httpClient ?? throw new InvalidOperationException("Connection is not open");
    }


    /// <summary>
    /// Creates a custom SQL function that can be used in SQL queries.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function.</typeparam>
    /// <param name="name">The name of the function as it will be used in SQL.</param>
    /// <param name="function">The function implementation.</param>
    /// <param name="isDeterministic">True if the function always returns the same result for the same input.</param>
    public void CreateFunction<TResult>(string name, Func<TResult> function, bool isDeterministic = false)
    {
        _customFunctions[name] = new LibSqlFunction<TResult>(name, _ => function(), isDeterministic);
    }

    /// <summary>
    /// Creates a custom SQL function that can be used in SQL queries.
    /// </summary>
    /// <typeparam name="T1">The type of the first parameter.</typeparam>
    /// <typeparam name="TResult">The return type of the function.</typeparam>
    /// <param name="name">The name of the function as it will be used in SQL.</param>
    /// <param name="function">The function implementation.</param>
    /// <param name="isDeterministic">True if the function always returns the same result for the same input.</param>
    public void CreateFunction<T1, TResult>(string name, Func<T1?, TResult?> function, bool isDeterministic = false)
    {
        _customFunctions[name] = new LibSqlFunction<T1, TResult>(name, args =>
        {
            var arg1 = args.Length > 0 ? (T1?)Convert.ChangeType(args[0], typeof(T1)) : default;
            return function(arg1);
        }, isDeterministic);
    }

    /// <summary>
    /// Creates a custom SQL function that can be used in SQL queries.
    /// </summary>
    /// <typeparam name="T1">The type of the first parameter.</typeparam>
    /// <typeparam name="T2">The type of the second parameter.</typeparam>
    /// <typeparam name="TResult">The return type of the function.</typeparam>
    /// <param name="name">The name of the function as it will be used in SQL.</param>
    /// <param name="function">The function implementation.</param>
    /// <param name="isDeterministic">True if the function always returns the same result for the same input.</param>
    public void CreateFunction<T1, T2, TResult>(string name, Func<T1?, T2?, TResult?> function, bool isDeterministic = false)
    {
        _customFunctions[name] = new LibSqlFunction<T1, T2, TResult>(name, args =>
        {
            var arg1 = args.Length > 0 ? (T1?)Convert.ChangeType(args[0], typeof(T1)) : default;
            var arg2 = args.Length > 1 ? (T2?)Convert.ChangeType(args[1], typeof(T2)) : default;
            return function(arg1, arg2);
        }, isDeterministic);
    }

    /// <summary>
    /// Gets a registered custom function by name.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <returns>The custom function if found, null otherwise.</returns>
    internal ILibSqlFunction? GetFunction(string name) =>
        _customFunctions.TryGetValue(name, out var function) ? function : null;
}
