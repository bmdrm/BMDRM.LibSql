// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.LibSql.Connection.Internal;
using static SQLitePCL.raw;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// A custom DataReader that properly handles type conversions for LibSQL (SQLite) data types.
/// </summary>
internal class LibSqlDataReader : DbDataReader
{
    private readonly DataTableReader _inner;
    private readonly DataTable _table;

    public LibSqlDataReader(DataTable table)
    {
        _table = table;
        _inner = table.CreateDataReader();
    }

    /// <summary>
    /// Gets the value of a column as a specific type.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column converted to the specified type.</returns>
    /// <exception cref="InvalidCastException">The specified cast is not valid.</exception>
    public override T GetFieldValue<T>(int ordinal)
    {
        if (typeof(T) == typeof(Stream))
        {
            return (T)(object)GetStream(ordinal);
        }

        if (typeof(T) == typeof(TextReader))
        {
            return (T)(object)GetTextReader(ordinal);
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            var value = GetValue(ordinal);

            if (value == DBNull.Value)
            {
                return (T)(object)DBNull.Value;
            }

            switch (value)
            {
                case DateTimeOffset dateTimeOffsetValue:
                    return (T)(object)dateTimeOffsetValue;
                case string str when DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTimeOffset):
                    return (T)(object)parsedDateTimeOffset;
                case string str:
                    throw new InvalidCastException($"Unable to convert string '{str}' to DateTimeOffset.  Invalid format.");
                case DateTime dateTime:
                    return (T)(object)new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
                case long longValue:
                    try
                    {
                        return (T)(object)DateTimeOffset.FromUnixTimeSeconds(longValue);
                    }
                    catch (Exception)
                    {
                        throw new InvalidCastException($"Unable to convert long '{longValue}' to DateTimeOffset.  Invalid Unix timestamp.");
                    }
                default:
                    throw new InvalidCastException($"Unable to convert {value.GetType().Name} to DateTimeOffset");
            }
        }

        if (typeof(T) == typeof(TimeSpan))
        {
            var value = GetValue(ordinal);

            if (value == DBNull.Value)
            {
                throw new InvalidCastException("Cannot convert DBNull.Value to TimeSpan");
            }

            if (value is TimeSpan timeSpanValue)
            {
                return (T)(object)timeSpanValue;
            }

            if (value is not string str)
            {
                return base.GetFieldValue<T>(ordinal)!;
            }

            if (TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out var parsedTimeSpan))
            {
                return (T)(object)parsedTimeSpan;
            }

            return base.GetFieldValue<T>(ordinal)!;
        }

        return base.GetFieldValue<T>(ordinal)!;
    }

    public override object GetValue(int ordinal)
        => _inner.GetValue(ordinal);

    public override int GetValues(object[] values)
        => _inner.GetValues(values);

    public override bool IsDBNull(int ordinal)
        => _inner.IsDBNull(ordinal);

    public override int FieldCount
        => _inner.FieldCount;

    public override bool HasRows
        => _inner.HasRows;

    public override bool IsClosed
        => _inner.IsClosed;

    public override int Depth
        => _inner.Depth;

    public override bool Read()
        => _inner.Read();

    public override bool NextResult()
        => _inner.NextResult();

    public override int RecordsAffected
        => _inner.RecordsAffected;

    public override string GetName(int ordinal)
        => _inner.GetName(ordinal);

    public override string GetDataTypeName(int ordinal)
        => _inner.GetDataTypeName(ordinal);

    public override Type GetFieldType(int ordinal)
        => _inner.GetFieldType(ordinal);

    public override int GetOrdinal(string name)
        => _inner.GetOrdinal(name);

    // Implement type-specific getters with proper conversions, handling SQLite's type affinity
    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Boolean");
        if (value is long longValue)
            return longValue != 0;
        if (value is int intValue)
            return intValue != 0;
        if (value is string strValue)
            return strValue == "1" || strValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        return Convert.ToBoolean(value);
    }

    public override byte GetByte(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Byte");
        if (value is long longValue)
            return checked((byte)longValue);
        if (value is int intValue)
            return checked((byte)intValue);
        return Convert.ToByte(value);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return 0;
        if (value is not byte[] bytes)
            return 0;
        if (buffer == null)
            return bytes.Length;

        var bytesToCopy = Math.Min(length, bytes.Length - (int)dataOffset);
        Array.Copy(bytes, dataOffset, buffer, bufferOffset, bytesToCopy);
        return bytesToCopy;
    }

    public override char GetChar(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Char");
        if (value is string str && str.Length > 0)
            return str[0];
        return Convert.ToChar(value);
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return 0;
        if (value is not string str)
            return 0;
        if (buffer == null)
            return str.Length;

        var charsToCopy = Math.Min(length, str.Length - (int)dataOffset);
        str.CopyTo((int)dataOffset, buffer, bufferOffset, charsToCopy);
        return charsToCopy;
    }

    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert DBNull.Value to DateTime");
        }

        if (value is string str)
        {
            return DateTime.SpecifyKind(HttpDbDateTimeParser.ParseSqliteDateTime(str), DateTimeKind.Unspecified);
        }
        else if (value is long longValue)
        {
            return DateTime.SpecifyKind(
                DateTimeOffset.FromUnixTimeSeconds(longValue).ToOffset(TimeSpan.Zero).DateTime, DateTimeKind.Unspecified);
        }
        else if (value is double doubleValue)
        {
            return DateTime.SpecifyKind(HttpDbDateTimeParser.JulianToDateTime(doubleValue), DateTimeKind.Unspecified);
        }
        else if (value is DateTime dateTime)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        }
        else
        {
            throw new InvalidCastException($"Unable to convert {value.GetType().Name} to DateTime");
        }
    }

    public override decimal GetDecimal(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Decimal");
        if (value is long longValue)
            return longValue;
        if (value is double doubleValue)
            return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
        if (value is string str && decimal.TryParse(str, CultureInfo.InvariantCulture, out var decimalValue))
            return decimalValue;
        return Convert.ToDecimal(value);
    }

    public override double GetDouble(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Double");
        if (value is string str && double.TryParse(str, CultureInfo.InvariantCulture, out var doubleValue))
            return doubleValue;
        if (value is long longValue)
            return longValue;
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    public override float GetFloat(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Single");
        if (value is long longValue)
            return longValue;
        if (value is double doubleValue)
            return (float)doubleValue;
        if (value is string str && float.TryParse(str, CultureInfo.InvariantCulture, out var floatValue))
            return floatValue;
        return Convert.ToSingle(value);
    }

    public override short GetInt16(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Int16");
        if (value is long longValue)
            return checked((short)longValue);
        if (value is int intValue)
            return checked((short)intValue);
        return Convert.ToInt16(value);
    }

    public override int GetInt32(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Int32");
        if (value is long longValue)
            return checked((int)longValue);
        return Convert.ToInt32(value);
    }

    public override long GetInt64(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert DBNull.Value to Int64");
        return Convert.ToInt64(value);
    }

    public override string GetString(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return string.Empty;
        return Convert.ToString(value) ?? string.Empty;
    }

    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);

        if (value == DBNull.Value)
        {
            return Guid.Empty;
        }

        if (value is Guid guidValue)
        {
            return guidValue;
        }

        if (value is byte[] bytes)
        {
            return bytes.Length == 16
                ? new Guid(bytes)
                : new Guid(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
        }

        var stringValue = GetString(ordinal);

        if (string.IsNullOrEmpty(stringValue))
        {
            return Guid.Empty;
        }

        if (Guid.TryParse(stringValue, out var result))
        {
            return result;
        }

        if (long.TryParse(stringValue, out var longResult))
        {
            return new Guid(longResult.ToString());
        }

        throw new FormatException($"Unrecognized Guid format value: {stringValue}");
    }

    protected virtual T? GetNull<T>(int ordinal)
        => typeof(T) == typeof(DBNull)
            ? (T)(object)DBNull.Value
            : default;

    private int GetSqliteType(int ordinal)
    {
        var fieldType = _inner.GetFieldType(ordinal);
        if (fieldType == typeof(string))
        {
            return SQLITE_TEXT;
        }

        if (fieldType == typeof(int)
            || fieldType == typeof(long)
            || fieldType == typeof(short)
            || fieldType == typeof(byte)
            || fieldType == typeof(bool))
        {
            return SQLITE_INTEGER;
        }

        if (fieldType == typeof(double) || fieldType == typeof(float) || fieldType == typeof(decimal))
        {
            return SQLITE_FLOAT;
        }

        if (fieldType == typeof(byte[]))
        {
            return SQLITE_BLOB;
        }

        if (fieldType == typeof(DBNull))
        {
            return SQLITE_NULL;
        }

        //Default to text if all others don't match
        return SQLITE_TEXT;
    }

    public override object this[int ordinal]
        => GetValue(ordinal);

    public override object this[string name]
        => GetValue(GetOrdinal(name));

    public override IEnumerator GetEnumerator()
        => _inner.GetEnumerator();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
            _table.Dispose();
        }

        base.Dispose(disposing);
    }
}
