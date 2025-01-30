// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Base implementation of a LibSQL function.
/// </summary>
internal class LibSqlFunction<TResult> : ILibSqlFunction
{
    private readonly Func<object?[], object?> _implementation;

    public string Name { get; }
    public bool IsDeterministic { get; }

    public LibSqlFunction(string name, Func<object?[], object?> implementation, bool isDeterministic)
    {
        Name = name;
        _implementation = implementation;
        IsDeterministic = isDeterministic;
    }

    public object? Invoke(params object?[] args) => _implementation(args);
}

/// <summary>
/// Implementation of a LibSQL function with one parameter.
/// </summary>
internal class LibSqlFunction<T1, TResult> : ILibSqlFunction
{
    private readonly Func<object?[], object?> _implementation;

    public string Name { get; }
    public bool IsDeterministic { get; }

    public LibSqlFunction(string name, Func<object?[], object?> implementation, bool isDeterministic)
    {
        Name = name;
        _implementation = implementation;
        IsDeterministic = isDeterministic;
    }

    public object? Invoke(params object?[] args) => _implementation(args);
}

/// <summary>
/// Implementation of a LibSQL function with two parameters.
/// </summary>
internal class LibSqlFunction<T1, T2, TResult> : ILibSqlFunction
{
    private readonly Func<object?[], object?> _implementation;

    public string Name { get; }
    public bool IsDeterministic { get; }

    public LibSqlFunction(string name, Func<object?[], object?> implementation, bool isDeterministic)
    {
        Name = name;
        _implementation = implementation;
        IsDeterministic = isDeterministic;
    }

    public object? Invoke(params object?[] args) => _implementation(args);
}
