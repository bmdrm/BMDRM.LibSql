// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics.CodeAnalysis;
using Humanizer.Localisation;
using Microsoft.Data.Sqlite;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents a parameter used in HTTP-based database commands.
/// This class allows for defining the parameter's data type, size, direction,
/// value, and other related properties for HTTP database operations.
/// </summary>
public class HttpDbParameter : SqliteParameter
{
    private string _parameterName = string.Empty;
    private string _sourceColumn = string.Empty;
    private int _size = 0;
    private bool _sizeInitialized = false; // Flag to check if size was initialized explicitly

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbParameter"/> class.
    /// </summary>
    public HttpDbParameter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbParameter"/> class with the parameter name and value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    public HttpDbParameter(string? parameterName, object? value)
        : base(parameterName, value)
    {
        ParameterName = parameterName;
        Value = value;

        if (value != null && !_sizeInitialized)
        {
             if (value is string str)
                {
                    Size = str.Length;
                }
            else if (value is byte[] bytes)
                {
                    Size = bytes.Length;
                }
        }
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbParameter"/> class with the parameter name and value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="dataType">The Bbtype of the parameter.</param>
    public HttpDbParameter(string? parameterName, DbType dataType ,object? value)
        : base(parameterName, value)
    {
        ParameterName = parameterName;
        Value = value;
        DbType = dataType;
        if (value != null && !_sizeInitialized)
        {
            if (value is string str)
            {
                Size = str.Length;
            }
            else if (value is byte[] bytes)
            {
                Size = bytes.Length;
            }
        }
    }
    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpDbParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    public HttpDbParameter(string? name, SqliteType type, int size)
        : base(name, type, size)
    {
        ParameterName = name;
        SqliteType = type;
        _size = size;
        _sizeInitialized = true;
    }
    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpDbParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    public HttpDbParameter(string name, SqliteType type)
    : base(name, type)
    {
        ParameterName = name;
        SqliteType = type;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpDbParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <param name="sourceColumn">The source column used for loading the value. Can be null.</param>
    public HttpDbParameter(string? name, SqliteType type, int size, string? sourceColumn)
        : base(name, type, size)
    {
        ParameterName = name;
        SqliteType = type;
        _size = size;
       _sizeInitialized = true;
        SourceColumn = sourceColumn;
    }
    /// <inheritdoc />
    public override DbType DbType { get; set; } = DbType.String;

    /// <inheritdoc />
   public override int Size
    {
        get => _size;
        set
        {
           _size = value;
           _sizeInitialized = true;
        }
    }

    /// <inheritdoc />
    public override bool IsNullable { get; set; }

    /// <inheritdoc />
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    /// <inheritdoc />
    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    /// <inheritdoc />
    public override object? Value
    {
        get => base.Value;
         set
        {
            base.Value = value;

             if (value != null && !_sizeInitialized)
            {
               if (value is string str)
                    {
                        Size = str.Length;
                    }
                else if (value is byte[] bytes)
                    {
                        Size = bytes.Length;
                    }
            }
        }
    }

     /// <inheritdoc/>
    public override void ResetDbType()
    {
        DbType = DbType.String;
        _size = 0;
        _sizeInitialized = false;
    }

    /// <inheritdoc />
    public override bool SourceColumnNullMapping { get; set; }

    /// <inheritdoc />
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

}
