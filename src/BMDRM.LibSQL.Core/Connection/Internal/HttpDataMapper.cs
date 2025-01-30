// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection.Internal;

internal sealed class HttpDataMapper
{
     public static string GetLibSqlType(DbType dbType)
    {
        return dbType switch
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
    }
    public static LibSqlParameter ConvertParameterValue(HttpDbParameter p)
    {
        if (p.Value == null || p.Value == DBNull.Value)
            return new LibSqlParameter
            {
                name = p.ParameterName,
                type = "null",
                value = null
            };

        switch (p.DbType)
        {
            case DbType.Boolean:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "integer",
                    value = ((p.Value is bool b1 && b1) ? 1 : 0).ToString(CultureInfo.InvariantCulture) // Changed
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
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "text",
                    value = ((DateTime)p.Value).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
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
                    value = Convert.ToInt64(p.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) // Changed
                };
            case DbType.Double:
            case DbType.Decimal:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "float",
                    value = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture)
                };
            case DbType.Binary:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = "blob",
                    value = Convert.ToBase64String((byte[])p.Value)
                };
            default:
                return new LibSqlParameter
                {
                    name = p.ParameterName,
                    type = GetLibSqlType(p.DbType),
                    value = p.Value?.ToString()
                };
        }
    }
      public static List<string> GetSqlOperationTypes(string sql)
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
     public static string GetSqlOperationType(string sql)
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
    public static IEnumerable<string> SplitSqlStatements(string sql)
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
    public static object? GetValue(JsonElement element)
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
            catch
            {
                return DBNull.Value;
            }
        }

        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetInt64().ToString(CultureInfo.InvariantCulture),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Null => DBNull.Value,
                _ => element.GetRawText()
            };
        } catch {
            return DBNull.Value;
        }
    }
    public static object? ParseColumnValue(string? type, string? value)
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
                case "real":
                    return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleResult)
                        ? doubleResult
                        : DBNull.Value;
                case "text":
                    if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTimeOffset))
                    {
                        return dateTimeOffset;
                    }
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                    {
                        return dateTime;
                    }
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
        catch
        {
            return DBNull.Value;
        }
    }
}
