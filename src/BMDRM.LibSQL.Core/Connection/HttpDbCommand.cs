// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.LibSql.Connection.Internal;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents a command that can be executed against an HTTP-based database.
/// Inherits from <see cref="DbCommand"/> and performs operations using HTTP requests.
/// </summary>
public class HttpDbCommand : DbCommand
{
    private readonly HttpDbConnection _connection;
    private DbTransaction? _transaction;
    private string? _commandText;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbCommand"/> class.
    /// </summary>
    /// <param name="connection">The <see cref="HttpDbConnection"/> used to execute commands.</param>
    /// <param name="transaction">The transaction within which the command executes, or null.</param>
    public HttpDbCommand(
        HttpDbConnection connection,
        DbTransaction? transaction = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    /// <summary>
    /// Gets or sets the text of the SQL or query command to be executed.
    /// </summary>
    [AllowNull]
    public override string CommandText
    {
        get => _commandText ?? string.Empty;
        set => _commandText = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the timeout for the command execution in seconds.
    /// </summary>
    public override int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the type of the command (Text, StoredProcedure, etc.).
    /// </summary>
    public override CommandType CommandType { get; set; } = CommandType.Text;

    /// <summary>
    /// Gets or sets the row source for the command (None, Output, Both).
    /// </summary>
    public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

    /// <summary>
    /// Gets the connection associated with this command.
    /// </summary>
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => throw new NotSupportedException("Setting DbConnection is not supported.");
    }

    /// <summary>
    /// Gets or sets the transaction associated with this command.
    /// </summary>
    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set => _transaction = value;
    }

    /// <summary>
    /// Gets the collection of parameters associated with the command.
    /// </summary>
    protected override DbParameterCollection DbParameterCollection { get; } = new HttpDbParameterCollection();

    /// <summary>
    /// Gets or sets a value that determines whether the command is visible at design time.
    /// </summary>
    public override bool DesignTimeVisible { get; set; } = false;

    /// <summary>
    /// Prepares the command for execution. In the case of an HTTP-based database, this method is a no-op.
    /// </summary>
    public override void Prepare()
    {
        // HTTP-based databases usually don't require preparation. Implemented as a no-op.
    }

    /// <summary>
    /// Cancels the command execution. Not supported for HTTP-based database commands.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown because cancellation is not supported.</exception>
    public override void Cancel()
    {
        throw new NotSupportedException("Cancellation is not supported.");
    }

    /// <summary>
    /// Executes the command asynchronously and returns the number of rows affected.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(CommandText))
        {
            return 0;
        }

        return await ExecuteNonQueryInternalAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected synchronously.
    /// </summary>
    /// <returns>The number of rows affected.</returns>
    public override int ExecuteNonQuery()
        => ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the command asynchronously and returns a single value.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value from the query.</returns>
    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var response = await ExecuteHttpRequestAsync(cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (!root.TryGetProperty("results", out var resultsElement))
        {
            return null;
        }

        var resultsArray = resultsElement.EnumerateArray().ToList();
        var sqlOperationTypes = GetSqlOperationTypes(CommandText);
        object? scalarResult = null;
        var sqlOperationIndex = 0;
        JsonElement? lastSelectResult = null;
        foreach (var result in resultsArray)
        {
            if (result.TryGetProperty("response", out var resp)
                && resp.TryGetProperty("type", out var type)
                && type.GetString() == "execute")
            {
                var sqlOperation = sqlOperationTypes.Count > sqlOperationIndex ? sqlOperationTypes[sqlOperationIndex] : "OTHER";
                if (sqlOperation != "OTHER")
                {
                    var updateResult = ProcessUpdateScalarResult(resultsArray, sqlOperation);
                    if (updateResult != null)
                    {
                        scalarResult = updateResult;
                        sqlOperationIndex++;
                        continue;
                    }
                }

                if (result.TryGetProperty("response", out var responseElement)
                    && responseElement.TryGetProperty("result", out var resultElement)
                    && resultElement.TryGetProperty("rows", out var rowsElement)
                    && rowsElement.GetArrayLength() > 0)
                {
                    lastSelectResult = resultElement;
                    sqlOperationIndex++;

                    continue;
                }
                else if (result.TryGetProperty("response", out var responseElement1)
                         && responseElement1.TryGetProperty("result", out var resultElement1))
                {
                    scalarResult = GetValue(resultElement1);
                    sqlOperationIndex++;
                    continue;
                }
            }

            if (result.TryGetProperty("response", out var closeResp)
                && closeResp.TryGetProperty("type", out var closeType)
                && closeType.GetString() == "close")
            {
                continue;
            }
        }

        if (lastSelectResult != null)
        {
            if (lastSelectResult.Value.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
            {
                var firstRow = rows.EnumerateArray().FirstOrDefault();

                if (firstRow.ValueKind != JsonValueKind.Undefined && firstRow.GetArrayLength() > 0)
                {
                    var value = firstRow[0];
                    scalarResult = GetValue(value);
                }
            }
            else
            {
                scalarResult = GetValue(lastSelectResult.Value);
            }
        }

        if (scalarResult == null && (CommandText.Contains("COUNT(*)") || CommandText.Contains("sqlite_master")))
        {
            return 0L;
        }

        return scalarResult;
    }

    private object? ProcessUpdateScalarResult(List<JsonElement> resultsArray, string sqlOperation)
    {
        if (sqlOperation == "INSERT")
        {
            return ProcessInsert(resultsArray);
        }

        if (sqlOperation is not ("UPDATE" or "DELETE"))
        {
            return null;
        }

        var rowsAffected = 0;

        var verifyResult = resultsArray.FirstOrDefault(r => r.TryGetProperty("response", out var vResp)
            && vResp.TryGetProperty("result", out var vResult)
            && vResult.TryGetProperty("rows", out var vRows)
            && vRows.GetArrayLength() > 0
            && vRows[0].GetArrayLength() > 0
            && vResp.TryGetProperty("type", out var vType)
            && vType.GetString() == "execute"
        );

        if (verifyResult.ValueKind != JsonValueKind.Undefined)
        {
            if (verifyResult.TryGetProperty("response", out var vResp)
                && vResp.TryGetProperty("result", out var vResult)
                && vResult.TryGetProperty("rows", out var vRows)
                && vRows.GetArrayLength() > 0
                && vRows[0].GetArrayLength() > 0)
            {
                var countValue = vRows[0][0].TryGetProperty("value", out var valueElement) ? valueElement.GetString() : null;
                var count = int.Parse(countValue ?? "0");
                if (count == 0)
                {
                    var idParam = Parameters.Cast<HttpDbParameter>()
                        .FirstOrDefault(p => p.ParameterName == "@p4");
                    var errorMessage = idParam != null
                        ? $"The record with ID '{idParam.Value}' was not found."
                        : "The record was not found.";
                    throw new DbUpdateConcurrencyException(errorMessage);
                }
            }
        }

        var changesResult = resultsArray.FirstOrDefault(r => r.TryGetProperty("response", out var cResp)
            && cResp.TryGetProperty("result", out var cResult)
            && cResult.TryGetProperty("rows", out var cRows)
            && cRows.GetArrayLength() > 0
            && cRows[0].GetArrayLength() > 0
            && cResp.TryGetProperty("type", out var cType)
            && cType.GetString() == "execute"
        );

        if (changesResult.ValueKind == JsonValueKind.Undefined)
        {
            return rowsAffected;
        }

        {
            if (!changesResult.TryGetProperty("response", out var cResp)
                || !cResp.TryGetProperty("result", out var cResult)
                || !cResult.TryGetProperty("rows", out var cRows)
                || cRows.GetArrayLength() <= 0
                || cRows[0].GetArrayLength() <= 0)
            {
                return rowsAffected;
            }

            var affectedValue = cRows[0][0].TryGetProperty("value", out var affectedElement) ? affectedElement.GetString() : null;
            rowsAffected = int.Parse(affectedValue ?? "0");
            if (rowsAffected == 0)
            {
                var idParam = Parameters.Cast<HttpDbParameter>()
                    .FirstOrDefault(p => p.ParameterName == "@p4");
                var errorMessage = idParam != null
                    ? $"The record with ID '{idParam.Value}' was modified or deleted by another process."
                    : "The record was modified or deleted by another process.";
                throw new DbUpdateConcurrencyException(errorMessage);
            }
        }

        return rowsAffected;
    }

    /// <summary>
    /// Executes the command and returns a single value synchronously.
    /// </summary>
    /// <returns>The value from the query.</returns>
    public override object? ExecuteScalar()
        => ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the command and returns a data reader for reading result rows.
    /// </summary>
    /// <param name="behavior">Specifies the behavior of the data reader.</param>
    /// <returns>A data reader representing the result set.</returns>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var response = ExecuteHttpRequestAsync(CancellationToken.None).GetAwaiter().GetResult();
        return ParseResponseToDataReader(response).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Parses the HTTP response and returns a DbDataReader for reading result rows.
    /// </summary>
    /// <param name="response">The HTTP response to parse.</param>
    /// <returns>A DbDataReader representing the result set.</returns>
    private async Task<DbDataReader> ParseResponseToDataReader(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var dataTable = new DataTable();
            var usedColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!root.TryGetProperty("results", out var resultsElement) || !resultsElement.EnumerateArray().Any())
            {
                dataTable.Columns.Add("Value", typeof(long));
                var row = dataTable.NewRow();
                row[0] = 0L;
                dataTable.Rows.Add(row);
                return new LibSqlDataReader(dataTable);
            }

            foreach (var result in resultsElement.EnumerateArray())
            {
                if (result.TryGetProperty("response", out var responseElement)
                    && responseElement.TryGetProperty("type", out var typeElement)
                    && typeElement.GetString() == "execute"
                    && responseElement.TryGetProperty("result", out var resultData))
                {
                    if (resultData.TryGetProperty("rows", out var rowsElement) && resultData.TryGetProperty("cols", out var colsElement))
                    {
                        var rows = rowsElement.EnumerateArray();
                        var cols = colsElement.EnumerateArray();

                        foreach (var col in cols.AsEnumerable())
                        {
                            if (col.TryGetProperty("name", out var nameElement) && col.TryGetProperty("decltype", out var typeElement1))
                            {
                                var baseColumnName = nameElement.GetString() ?? $"Column{dataTable.Columns.Count}";
                                var columnName = baseColumnName;
                                var suffix = 1;
                                while (usedColumnNames.Contains(columnName))
                                {
                                    columnName = $"{baseColumnName}_{suffix}";
                                    suffix++;
                                }

                                usedColumnNames.Add(columnName);
                                var sqliteType = typeElement1.GetString();
                                var columnType = sqliteType switch
                                {
                                    "INTEGER" => typeof(long),
                                    "REAL" => typeof(double),
                                    "TEXT" => typeof(string),
                                    "BLOB" => typeof(byte[]),
                                    _ => typeof(string)
                                };
                                dataTable.Columns.Add(columnName, columnType);
                            }
                        }

                        foreach (var row in rows)
                        {
                            var dataRow = dataTable.NewRow();
                            var columnIndex = 0;
                            foreach (var col in row.EnumerateArray().TakeWhile(col => columnIndex < dataTable.Columns.Count))
                            {
                                if (col.ValueKind == JsonValueKind.Object
                                    && col.TryGetProperty("type", out var typeElement2)
                                    && col.TryGetProperty("value", out var valueElement))
                                {
                                    var type = typeElement2.GetString()?.ToLowerInvariant();
                                    object? value = null;

                                    switch (type)
                                    {
                                        case "text":
                                        case "blob":
                                        case null:
                                            value = valueElement.GetString();
                                            break;
                                        case "integer" when valueElement.ValueKind == JsonValueKind.String:
                                            long.TryParse(valueElement.GetString(), out var longValue);
                                            value = longValue;
                                            break;
                                        case "integer":
                                            value = valueElement.GetInt64();
                                            break;
                                        case "float":
                                        {
                                            value = valueElement.ValueKind == JsonValueKind.String
                                                ? double.Parse(valueElement.GetString()!, CultureInfo.InvariantCulture)
                                                : valueElement.GetDouble();
                                            break;
                                        }
                                    }

                                    try
                                    {
                                        if (dataTable.Columns[columnIndex].DataType == typeof(long))
                                        {
                                            var parsedLong = value != null
                                                && long.TryParse(
                                                    value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var intResult)
                                                    ? intResult
                                                    : (object)DBNull.Value;
                                            dataRow[columnIndex] = parsedLong;
                                        }
                                        else if (dataTable.Columns[columnIndex].DataType == typeof(DateTimeOffset))
                                        {
                                            if (value != null
                                                && DateTimeOffset.TryParse(
                                                    value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                                                    out var dateTimeOffset))
                                            {
                                                dataRow[columnIndex] = dateTimeOffset;
                                            }
                                            else
                                            {
                                                dataRow[columnIndex] = DBNull.Value;
                                            }

                                            continue;
                                        }
                                        else
                                        {
                                            dataRow[columnIndex] = ParseColumnValue(type, value?.ToString());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        dataRow[columnIndex] = DBNull.Value;
                                        throw new LibSqlHttpException(
                                            $"Error parsing column value at index {columnIndex}",
                                            CommandText,
                                            null,
                                            ex.Message,
                                            null,
                                            ex);
                                    }
                                }
                                else
                                {
                                    dataRow[columnIndex] = DBNull.Value;
                                }

                                columnIndex++;
                            }

                            dataTable.Rows.Add(dataRow);
                        }
                    }
                    else if (resultData.ValueKind != JsonValueKind.Undefined)
                    {
                        dataTable.Columns.Add("Value", typeof(string));
                        var row = dataTable.NewRow();
                        row[0] = GetValue(resultData);
                        dataTable.Rows.Add(row);
                    }
                }
            }

            return new LibSqlDataReader(dataTable);
        }
        catch (HttpRequestException ex)
        {
            throw new LibSqlHttpException(
                "HTTP request failed while reading data",
                CommandText,
                response.StatusCode,
                await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                null,
                ex);
        }
        catch (JsonException ex)
        {
            throw new LibSqlHttpException(
                "Failed to parse JSON response while reading data",
                CommandText,
                response.StatusCode,
                await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                null,
                ex);
        }
        catch (Exception ex) when (!(ex is LibSqlHttpException))
        {
            throw new LibSqlHttpException(
                "An unexpected error occurred while reading data",
                CommandText,
                response.StatusCode,
                await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                null,
                ex);
        }
    }

    private object? ParseColumnValue(string? type, string? value)
    {
        if (type == null || string.IsNullOrEmpty(value))
        {
            return DBNull.Value;
        }

        try
        {
            switch (type)
            {
                case "integer":
                    return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var longResult)
                        ? longResult
                        : DBNull.Value;
                case "float":
                    return double.TryParse(value, out var doubleResult)
                        ? doubleResult
                        : DBNull.Value;
                case "text":
                    if (Guid.TryParse(value, out var guidResult))
                    {
                        return guidResult;
                    }

                    return value;
                case "blob":
                    return Convert.FromBase64String(value);
                default:
                    return value;
            }
        }
        catch (Exception ex)
        {
            throw new LibSqlHttpException(
                "Error parsing JSON element value",
                CommandText,
                null,
                "Failed to parse JSON element",
                null,
                ex);
        }
    }

    private object? GetValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("type", out var typeElement)
            && element.TryGetProperty("value", out var valueElement))
        {
            var type = typeElement.GetString()?.ToLowerInvariant();
            if (type == "null")
            {
                return DBNull.Value;
            }

            var value = valueElement.GetString();

            if (string.IsNullOrEmpty(value))
            {
                return DBNull.Value;
            }

            try
            {
                return type switch
                {
                    "integer" => long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var intResult)
                        ? intResult
                        : (object)DBNull.Value,
                    "float" => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleResult)
                        ? doubleResult
                        : (object)DBNull.Value,
                    "text" => value,
                    "blob" => Convert.FromBase64String(value),
                    _ => value
                };
            }
            catch (Exception ex)
            {
                throw new LibSqlHttpException(
                    $"Error parsing value of type '{type}'",
                    CommandText,
                    null,
                    $"Failed to parse value: {value}",
                    null,
                    ex);
            }
        }

        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetInt64().ToString(),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Null => DBNull.Value,
                _ => element.GetRawText()
            };
        }
        catch (Exception ex)
        {
            throw new LibSqlHttpException(
                "Error parsing JSON element value",
                CommandText,
                null,
                "Failed to parse JSON element",
                null,
                ex);
        }
    }

    private IEnumerable<string> SplitSqlStatements(string sql)
    {
        try
        {
            var statements = new List<string>();
            var currentStatement = new StringBuilder();
            var inString = false;
            var stringChar = '\0';

            for (int i = 0; i < sql.Length; i++)
            {
                var c = sql[i];

                if (c == '\'' || c == '"')
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (c == stringChar)
                    {
                        inString = false;
                    }
                }

                currentStatement.Append(c);

                if (c == ';' && !inString)
                {
                    var stmt = currentStatement.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        statements.Add(stmt);
                    }

                    currentStatement.Clear();
                }
            }

            var lastStmt = currentStatement.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(lastStmt))
            {
                statements.Add(lastStmt);
            }

            return statements;
        }
        catch (Exception ex)
        {
            throw new LibSqlHttpException(
                "Error splitting SQL statements",
                sql,
                null,
                "Failed to parse SQL into separate statements",
                null,
                ex);
        }
    }

    /// <summary>
    /// Custom exception class for LibSQL HTTP database operations.
    /// </summary>
    public class LibSqlHttpException : Exception
    {
        /// <summary>
        /// Gets the SQL command that caused the exception.
        /// </summary>
        public string? SqlCommand { get; }

        /// <summary>
        /// Gets the HTTP status code if applicable.
        /// </summary>
        public System.Net.HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// Gets the raw response content from the server.
        /// </summary>
        public string? ResponseContent { get; }

        /// <summary>
        /// Gets the request body that was sent to the server.
        /// </summary>
        public string? RequestBody { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibSqlHttpException"/> class.
        /// </summary>
        public LibSqlHttpException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibSqlHttpException"/> class with a specified error message and inner exception.
        /// </summary>
        public LibSqlHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibSqlHttpException"/> class with detailed information about the failed operation.
        /// </summary>
        public LibSqlHttpException(
            string message,
            string? sqlCommand,
            System.Net.HttpStatusCode? statusCode,
            string? responseContent,
            string? requestBody,
            Exception? innerException = null)
            : base(message, innerException)
        {
            SqlCommand = sqlCommand;
            StatusCode = statusCode;
            ResponseContent = responseContent;
            RequestBody = requestBody;
        }
    }

    private async Task<HttpResponseMessage> ExecuteHttpRequestAsync(
        CancellationToken cancellationToken = default,
        bool closeConnection = true)
    {
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("Command text cannot be null or empty.");
        }

        var statements = SplitSqlStatements(CommandText).ToList();
        if (statements.Count == 0)
        {
            throw new InvalidOperationException("No SQL statements to execute");
        }

        var httpClient = ((HttpDbConnection)Connection!).GetClient();
        if (httpClient == null)
        {
            throw new InvalidOperationException("Connection is not open.");
        }

        Dictionary<string, object> requestBody;
        StringContent requestContent;

        try
        {
            requestBody = new Dictionary<string, object>
            {
                {
                    "requests", statements.Select(sql =>
                    {
                        // 1. Extract unique parameters and maintain order:
                        var uniqueParameterNames = new List<string>();
                        var parameterMap = new Dictionary<string, int>(); // Map parameter names to positions
                        var parameterizedSqlBuilder = new StringBuilder();

                        var parts = Regex.Split(sql, @"(@[a-zA-Z0-9_]+)").Where(s => !string.IsNullOrEmpty(s)).ToList();
                        var parameterIndex = 1;
                        foreach (var part in parts)
                        {
                            if (Regex.IsMatch(part, @"(@[a-zA-Z0-9_]+)"))
                            {
                                if (!parameterMap.ContainsKey(part))
                                {
                                    parameterMap[part] = parameterIndex;
                                    uniqueParameterNames.Add(part);
                                    parameterIndex++;
                                }

                                parameterizedSqlBuilder.Append($"?{parameterMap[part]}");
                            }
                            else
                            {
                                parameterizedSqlBuilder.Append(part);
                            }
                        }

                        // 2. Map SQL Parameter Placeholders to Correct Positions:
                        var parameterizedSql = parameterizedSqlBuilder.ToString();

                        // 3. Pass Only Unique Parameters:
                        var args = uniqueParameterNames.Select(paramName =>
                        {
                            var parameter = Parameters.Cast<HttpDbParameter>().FirstOrDefault(p => p.ParameterName == paramName);
                            if (parameter == null)
                            {
                                throw new LibSqlHttpException($"Parameter {paramName} not found in parameters collection");
                            }

                            return ConvertParameterValue(parameter);
                        }).ToList();

                        var request = new { type = "execute", stmt = new { sql = parameterizedSql, args = args.ToArray() } };

                        return request;
                    }).Concat<object>(closeConnection ? new[] { new { type = "close" } } : Array.Empty<object>()).ToArray()
                }
            };

            // Serialize the request body
            requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");
        }
        catch (Exception ex)
        {
            throw new LibSqlHttpException(
                "Failed to prepare SQL request",
                CommandText,
                null,
                null,
                null,
                ex);
        }

        try
        {
            var response = await httpClient.PostAsync("", requestContent, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var requestBodyJson = JsonSerializer.Serialize(requestBody);
                throw new LibSqlHttpException(
                    $"HTTP request failed with status code {response.StatusCode}",
                    CommandText,
                    response.StatusCode,
                    responseContent,
                    requestBodyJson);
            }

            if (responseContent.Contains("\"type\":\"error\""))
            {
                var requestBodyJson = JsonSerializer.Serialize(requestBody);
                throw new LibSqlHttpException(
                    "Error in SQL execution",
                    CommandText,
                    response.StatusCode,
                    responseContent,
                    requestBodyJson);
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            var requestBodyJson = JsonSerializer.Serialize(requestBody);
            throw new LibSqlHttpException(
                "HTTP request failed",
                CommandText,
                null,
                ex.Message,
                requestBodyJson,
                ex);
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The database operation was canceled.", ex, cancellationToken);
        }
        catch (Exception ex) when (!(ex is LibSqlHttpException))
        {
            var requestBodyJson = JsonSerializer.Serialize(requestBody);
            throw new LibSqlHttpException(
                "An unexpected error occurred during database operation",
                CommandText,
                null,
                ex.Message,
                requestBodyJson,
                ex);
        }
    }

    private async Task<int> ExecuteNonQueryInternalAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            return 0;
        }

        var statements = SplitSqlStatements(CommandText).ToList();
        if (statements.Count == 0)
        {
            return 0;
        }

        var client = ((HttpDbConnection)Connection!).GetClient();
        if (client == null)
        {
            throw new InvalidOperationException("Connection is not open.");
        }

        var totalRowsAffected = 0;
        foreach (var sql in statements)
        {
            try
            {
                var sqlOperation = GetSqlOperationType(sql);
                var parameters = Parameters.Cast<HttpDbParameter>()
                    .Where(p => sql.Contains(p.ParameterName))
                    .OrderBy(p => p.ParameterName)
                    .Select(ConvertParameterValue)
                    .ToArray();

                var requests = BuildRequestBatch(sqlOperation, sql, parameters);

                var request = new Dictionary<string, object> { { "requests", requests.ToArray() } };
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync("", requestContent, cancellationToken).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                using var responseDoc = JsonDocument.Parse(responseJson);
                var responseRoot = responseDoc.RootElement;
                if (responseRoot.TryGetProperty("error", out var errorElement))
                {
                    var requestBodyJson = JsonSerializer.Serialize(request);
                    throw new LibSqlHttpException(
                        $"Error in SQL execution: {errorElement.GetString()}",
                        sql,
                        response.StatusCode,
                        responseJson,
                        requestBodyJson);
                }

                if (!responseRoot.TryGetProperty("results", out var resultsElement))
                {
                    var requestBodyJson = JsonSerializer.Serialize(request);
                    throw new LibSqlHttpException(
                        "Invalid response format: missing 'results' property",
                        sql,
                        response.StatusCode,
                        responseJson,
                        requestBodyJson);
                }

                totalRowsAffected += resultsElement.ValueKind switch
                {
                    JsonValueKind.Array => ProcessResults(sqlOperation, resultsElement, parameters),
                    JsonValueKind.Object => ProcessResults(
                        sqlOperation, JsonDocument.Parse(resultsElement.GetRawText()).RootElement, parameters),
                    _ => throw new LibSqlHttpException(
                        "Unexpected format for 'results' property, neither array nor object",
                        sql,
                        response.StatusCode,
                        responseJson,
                        JsonSerializer.Serialize(request))
                };
            }
            catch (HttpRequestException ex)
            {
                throw new LibSqlHttpException(
                    "HTTP request failed during command execution",
                    sql,
                    null,
                    ex.Message,
                    null,
                    ex);
            }
            catch (JsonException ex)
            {
                throw new LibSqlHttpException(
                    "Failed to parse JSON response",
                    sql,
                    null,
                    ex.Message,
                    null,
                    ex);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The database operation was canceled.", ex, cancellationToken);
            }
            catch (Exception ex) when (!(ex is LibSqlHttpException
                                           || ex is DbUpdateConcurrencyException
                                           || ex is OperationCanceledException))
            {
                throw new LibSqlHttpException(
                    "An unexpected error occurred during command execution",
                    sql,
                    null,
                    ex.Message,
                    null,
                    ex);
            }
        }

        return totalRowsAffected;
    }

    private string GetSqlOperationType(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return "OTHER";
        }

        var trimmedSql = sql.TrimStart();
        if (trimmedSql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase))
            return "UPDATE";
        if (trimmedSql.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
            return "INSERT";
        if (trimmedSql.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            return "DELETE";
        return "OTHER";
    }

    private List<string> GetSqlOperationTypes(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new List<string> { "OTHER" };
        }

        var statements = SplitSqlStatements(sql);
        var types = new List<string>();
        foreach (var statement in statements)
        {
            var trimmedSql = statement.TrimStart();
            if (trimmedSql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                types.Add("UPDATE");
                continue;
            }

            if (trimmedSql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                types.Add("INSERT");
                continue;
            }

            if (trimmedSql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                types.Add("DELETE");
                continue;
            }

            types.Add("OTHER");
        }

        return types;
    }

    private List<object> BuildRequestBatch(string operation, string sql, LibSqlParameter[] parameters)
    {
        var requests = new List<object>();
        var tableName = ExtractTableName(sql);
        var paramDict = parameters.ToDictionary(p => p.name, p => p);
        string? idParamName = null;
        LibSqlParameter? idParam = null;

        if (operation is "UPDATE" or "DELETE")
        {
            var whereMatch = Regex.Match(sql, @"WHERE\s+""Id""\s*=\s*(@p\d+)\b", RegexOptions.IgnoreCase);
            idParamName = whereMatch.Success ? whereMatch.Groups[1].Value : null;
            idParam = !string.IsNullOrEmpty(idParamName) ? paramDict.GetValueOrDefault(idParamName) : null;

            if (!string.IsNullOrEmpty(tableName) && idParam is not null && idParamName is not null && idParam.value is not null)
            {
                // Verify record exists
                requests.Add(
                    new ExecuteRequest(
                        $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = {idParamName};",
                        new[] { new LibSqlParameter(idParamName, idParam.value, "text") }
                    ));
            }
        }

        // Build main statement parameters
        var args = new List<LibSqlParameter>();
        var paramMatches = Regex.Matches(sql, @"(@p\d+)\b");
        foreach (Match match in paramMatches)
        {
            if (paramDict.TryGetValue(match.Value, out var param))
                args.Add(param);
        }

        requests.Add(new ExecuteRequest(sql, args.ToArray()));

        // Add changes() for modification operations
        if (operation is "UPDATE" or "DELETE")
        {
            requests.Add(new ExecuteRequest("SELECT changes();", Array.Empty<LibSqlParameter>()));
        }

        if (operation == "UPDATE"
            && !string.IsNullOrEmpty(tableName)
            && idParam != null
            && idParamName is not null
            && idParam.value is not null)
        {
            requests.Add(
                new ExecuteRequest(
                    $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = {idParamName};",
                    new[] { new LibSqlParameter(idParamName, idParam.value, "text") }
                ));
        }

        requests.Add(new CloseRequest());
        return requests;
    }

    private string ExtractTableName(string sql)
    {
        var match = Regex.Match(
            sql,
            @"(?:UPDATE|DELETE\s+FROM|INSERT\s+INTO)\s+""([^""]+)""|(\S+)",
            RegexOptions.IgnoreCase);

        return match.Success
            ? (string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[2].Value
                : match.Groups[1].Value)
            : string.Empty;
    }

    private int ProcessResults(string operation, JsonElement results, LibSqlParameter[] parameters)
    {
        var resultsArray = new List<JsonElement>();
        switch (results.ValueKind)
        {
            case JsonValueKind.Array:
                resultsArray = results.EnumerateArray().ToList();
                break;
            case JsonValueKind.Object:
                resultsArray.Add(results);
                break;
        }

        return operation switch
        {
            "INSERT" => ProcessInsert(resultsArray),
            "UPDATE" or "DELETE" => ProcessUpdateOrDelete(resultsArray, parameters),
            _ => ProcessGenericOperation(resultsArray)
        };
    }

    private int ProcessUpdateOrDelete(List<JsonElement> results, LibSqlParameter[] parameters)
    {
        // Process verification result first
        var verifyResult = results[0];
        if (verifyResult.TryGetProperty("response", out var vResp)
            && vResp.TryGetProperty("result", out var vResult)
            && vResult.TryGetProperty("rows", out var vRows)
            && vRows.GetArrayLength() > 0
            && vRows[0].GetArrayLength() > 0)
        {
            var countValue = vRows[0][0].TryGetProperty("value", out var valueElement) ? valueElement.GetString() : null;
            var count = int.Parse(countValue ?? "0");
            if (count == 0)
            {
                var idParam = parameters.FirstOrDefault(p => p.name == "@p4");
                var errorMessage = idParam != null
                    ? $"The record with ID '{idParam.value}' and version {parameters.FirstOrDefault(p => p.name == "@p5")?.value} was not found."
                    : "The record was not found.";
                throw new DbUpdateConcurrencyException(errorMessage);
            }
        }

        // Get the changes() result
        var changesResult = results[2];
        if (changesResult.TryGetProperty("response", out var cResp)
            && cResp.TryGetProperty("result", out var cResult)
            && cResult.TryGetProperty("rows", out var cRows)
            && cRows.GetArrayLength() > 0
            && cRows[0].GetArrayLength() > 0)
        {
            var affectedValue = cRows[0][0].TryGetProperty("value", out var affectedElement) ? affectedElement.GetString() : null;
            var rowsAffected = int.Parse(affectedValue ?? "0");
            if (rowsAffected == 0)
            {
                var idParam = parameters.FirstOrDefault(p => p.name == "@p4");
                var errorMessage = idParam != null
                    ? $"The record with ID '{idParam.value}' was modified or deleted by another process."
                    : "The record was modified or deleted by another process.";
                throw new DbUpdateConcurrencyException(errorMessage);
            }

            if (results.Count > 3)
            {
                var selectResult = results[3];
                if (selectResult.TryGetProperty("response", out var sResp)
                    && sResp.TryGetProperty("result", out var sResult)
                    && sResult.TryGetProperty("rows", out var sRows)
                    && sRows.GetArrayLength() > 0)
                {
                }
            }

            return rowsAffected;
        }

        return 0;
    }

    private static int ProcessInsert(List<JsonElement> results)
    {
        var executeResult = results[0];
        if (executeResult.TryGetProperty("response", out var resp)
            && resp.TryGetProperty("result", out var result)
            && result.TryGetProperty("affected_row_count", out var rowsAffectedElement))
        {
            var rowsAffected = rowsAffectedElement.GetInt32();
            return rowsAffected;
        }

        return 0;
    }

    private static int ProcessGenericOperation(List<JsonElement> results)
    {
        var executeResult = results.FirstOrDefault(r => r.TryGetProperty("response", out var resp)
            && resp.TryGetProperty("type", out var type)
            && type.GetString() == "execute");
        if (executeResult.ValueKind == JsonValueKind.Undefined)
        {
            return 0;
        }

        if (!executeResult.TryGetProperty("response", out var resp)
            || !resp.TryGetProperty("result", out var result)
            || !result.TryGetProperty("affected_row_count", out var rowsAffectedElement))
        {
            return 0;
        }

        var rowsAffected = rowsAffectedElement.GetInt32();
        return rowsAffected;
    }

    private string GetLibSqlType(DbType dbType)
        => dbType switch
        {
            DbType.Int16 => "integer",
            DbType.Int32 => "integer",
            DbType.Int64 => "integer",
            DbType.Double => "float",
            DbType.Decimal => "float",
            DbType.String => "text",
            DbType.Boolean => "integer",
            DbType.Binary => "blob",
            _ => "text"
        };

    private class LibSqlParameter
    {
        public LibSqlParameter()
        {
        }

        public LibSqlParameter(string parameterName, object parameterValue, string parameterType = "text")
        {
            name = parameterName;
            value = parameterValue;
            type = parameterType;
        }

        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public object? value { get; set; }
    }

    private LibSqlParameter ConvertParameterValue(HttpDbParameter p)
    {
        if (p.Value == null || p.Value == DBNull.Value)
        {
            return new LibSqlParameter
            {
                name = p.ParameterName,
                type = "null",
                value = null
            };
        }

        switch (p.DbType)
        {
            case DbType.Boolean:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "integer",
                    value = (p.Value is bool and true ? "1" : "0")
                };
            case DbType.String:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = p.Value.ToString()
                };
            case DbType.DateTime:
            case DbType.DateTime2:
                object? convertedDateTimeValue = p.Value switch
                {
                    string stringDate when DateTime.TryParse(
                            stringDate, CultureInfo.InvariantCulture, out var parsedDateTime)
                        => HttpDbDateTimeParser.FormatDateTime(parsedDateTime),
                    DateTime dateTimeValue => HttpDbDateTimeParser.FormatDateTime(dateTimeValue),
                    _ => throw new InvalidCastException(
                        $"Unable to cast object of type '{p.Value.GetType().FullName}' to type 'System.DateTime'.")
                };
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = convertedDateTimeValue
                };
            case DbType.DateTimeOffset:
                object? convertedDateTimeOffsetValue = p.Value switch
                {
                    string stringDateOffset when DateTimeOffset.TryParse(
                        stringDateOffset, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                        out var parsedDateTimeOffset) => HttpDbDateTimeParser.FormatDateTimeOffset(parsedDateTimeOffset),
                    string stringDateOffset => throw new InvalidCastException(
                        $"Unable to parse string '{stringDateOffset}' as a valid System.DateTimeOffset."),
                    DateTimeOffset dateTimeOffsetValue => HttpDbDateTimeParser.FormatDateTimeOffset(dateTimeOffsetValue),
                    _ => throw new InvalidCastException(
                        $"Unable to cast object of type '{p.Value.GetType().FullName}' to type 'System.DateTimeOffset'.")
                };
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = convertedDateTimeOffsetValue
                };
            case DbType.Time:
                object? convertedTimeSpanValue = p.Value switch
                {
                    string stringTimeSpan when TimeSpan.TryParse(stringTimeSpan, CultureInfo.InvariantCulture, out var parsedTimeSpan)
                        => parsedTimeSpan.ToString("c"),
                    TimeSpan timeSpanValue => timeSpanValue.ToString("c"),
                    _ => throw new InvalidCastException(
                        $"Unable to cast object of type '{p.Value.GetType().FullName}' to type 'System.TimeSpan'.")
                };
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = convertedTimeSpanValue
                };
            case DbType.Guid:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = p.Value.ToString()
                };
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "integer",
                    value = Convert.ToInt64(p.Value, CultureInfo.InvariantCulture).ToString()
                };
            case DbType.Double:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "float",
                    value = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture)
                };
            case DbType.Decimal:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "float",
                    value = Convert.ToDecimal(p.Value, CultureInfo.InvariantCulture)
                };
            case DbType.Binary:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "blob",
                    value = Convert.ToBase64String((byte[])p.Value)
                };
            case DbType.AnsiString:
            case DbType.Byte:
            case DbType.Currency:
            case DbType.Date:
            case DbType.Object:
            case DbType.SByte:
            case DbType.Single:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
            case DbType.VarNumeric:
            case DbType.AnsiStringFixedLength:
            case DbType.StringFixedLength:
            case DbType.Xml:
            default:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = GetLibSqlType(p.DbType),
                    value = p.Value?.ToString()
                };
        }
    }

    /// <summary>
    ///     Creates a new parameter.
    /// </summary>
    /// <returns>The new parameter.</returns>
    public new virtual HttpDbParameter CreateParameter()
        => new();

    /// <summary>
    /// Creates a new <see cref="HttpDbParameter"/> for the command.
    /// </summary>
    /// <returns>A new <see cref="HttpDbParameter"/> instance.</returns>
    protected override DbParameter CreateDbParameter()
        => CreateParameter();

    private class ExecuteRequest
    {
        public string type
            => "execute";

        public Statement stmt { get; }

        public ExecuteRequest(string sql, LibSqlParameter[] args)
        {
            stmt = new Statement { sql = sql, args = args };
        }
    }

    private class CloseRequest
    {
        public string type
            => "close";
    }

    private class Statement
    {
        public required string sql { get; set; }
        public required LibSqlParameter[] args { get; set; }
    }
}
