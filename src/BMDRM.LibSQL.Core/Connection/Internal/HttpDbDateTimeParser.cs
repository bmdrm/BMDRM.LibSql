// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection.Internal;

/// <summary>
/// Provides methods for parsing and converting DateTime values to and from SQLite compatible formats.
/// </summary>
public static class HttpDbDateTimeParser
{
    /// <summary>
    /// Parses a date/time string from a SQLite format into a DateTime object.
    /// </summary>
    /// <param name="text">The input date/time string, potentially in a SQLite format.</param>
    /// <returns>A DateTime object representing the parsed date/time.</returns>
    /// <exception cref="FormatException">Thrown when the input string cannot be parsed as a valid SQLite datetime.</exception>
    public static DateTime ParseSqliteDateTime(string text)
    {
        // SQLite datetime formats to try
        string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "yyyy-MM-dd'T'HH:mm:ss.fff",
            "yyyy-MM-dd'T'HH:mm:ss",
             "HH:mm:ss",
             "yyyy-MM-dd'T'HH:mm:ss.fffZ"
        };

        if (DateTime.TryParseExact(
                text, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        // Fallback to general parsing if specific formats fail
        if (DateTime.TryParse(
                text, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result))
        {
            return result;
        }

        throw new FormatException($"Unable to parse '{text}' as a valid SQLite datetime");
    }


    /// <summary>
    /// Converts a Julian date to a .NET DateTime object.
    /// </summary>
    /// <param name="julianDate">The Julian date to convert.</param>
    /// <returns>A DateTime object representing the corresponding date and time.</returns>
    public static DateTime JulianToDateTime(double julianDate)
    {
        // Julian date starts from noon on January 1, 4713 BC
        return DateTime.FromOADate(julianDate - 2415018.5);
    }
    /// <summary>
    /// Formats the date time
    /// </summary>
    /// <param name="dateTime">the dateTime</param>
    /// <returns>normalizedDateTime.</returns>
    public static string FormatDateTime(DateTime dateTime)
    {
        // Handle different DateTime kinds
        DateTime normalizedDateTime = dateTime.Kind switch
        {
            // Convert Local to UTC then to unspecified
            DateTimeKind.Local => DateTime.SpecifyKind(
                dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified),

            // Convert UTC to unspecified
            DateTimeKind.Utc => DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Unspecified),

            // Keep unspecified as is
            DateTimeKind.Unspecified => dateTime,

            _ => dateTime
        };

        return normalizedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Formats the date time offset to string.
    /// </summary>
    /// <param name="dateTimeOffset">Date time offset.</param>
    /// <returns>Stringified dateTimeOffset</returns>
    public static string FormatDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToOffset(TimeSpan.Zero).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
