// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents a custom SQL function that can be registered with a LibSQL database connection.
/// </summary>
public interface ILibSqlFunction
{
    /// <summary>
    /// Gets the name of the function.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the function is deterministic.
    /// </summary>
    bool IsDeterministic { get; }

    /// <summary>
    /// Invokes the function with the given arguments.
    /// </summary>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the function invocation.</returns>
    object? Invoke(params object?[] args);
}
