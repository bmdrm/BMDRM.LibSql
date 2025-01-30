// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents a collection of <see cref="DbParameter"/> objects used in HTTP-based database commands.
/// This class provides methods for managing parameters, including adding, removing, and accessing them by index or name.
/// </summary>
public class HttpDbParameterCollection : DbParameterCollection
{
    private readonly List<HttpDbParameter> _parameters = new();
    /// <summary>
    /// List of parameters;
    /// </summary>
    public List<HttpDbParameter> Parameters
    {
        get => _parameters;
    }

    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public override int Count
        => _parameters.Count;

    /// <summary>
    /// Gets the synchronization root for the collection.
    /// </summary>
    public override object SyncRoot
        => ((ICollection)_parameters).SyncRoot;

    /// <summary>
    /// Adds a <see cref="DbParameter"/> to the collection.
    /// </summary>
    /// <param name="value">The <see cref="DbParameter"/> to add.</param>
    /// <returns>The index of the added parameter.</returns>
    public override int Add(object value)
    {
        _parameters.Add((HttpDbParameter)value);
        return _parameters.Count - 1;
    }
    /// <summary>
    /// Adds a <see cref="DbParameter"/> to the collection.
    /// </summary>
    /// <param name="value">The <see cref="DbParameter"/> to add.</param>
    /// <returns>The index of the added parameter.</returns>
    public virtual HttpDbParameter Add(HttpDbParameter value)
    {
        _parameters.Add((HttpDbParameter)value);
        return value;
    }
    /// <summary>
    /// Adds an array of <see cref="DbParameter"/> objects to the collection.
    /// </summary>
    /// <param name="values">The array of <see cref="DbParameter"/> objects to add.</param>
    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value);
        }
    }

    /// <summary>
    /// Clears all parameters from the collection.
    /// </summary>
    public override void Clear()
        => _parameters.Clear();

    /// <summary>
    /// Determines whether a specific <see cref="DbParameter"/> is in the collection.
    /// </summary>
    /// <param name="value">The <see cref="DbParameter"/> to locate.</param>
    /// <returns><see langword="true"/> if the parameter is found; otherwise, <see langword="false"/>.</returns>
    public override bool Contains(object value)
        => _parameters.Contains((HttpDbParameter)value);

    /// <summary>
    /// Determines whether a parameter with a specific name exists in the collection.
    /// </summary>
    /// <param name="value">The name of the parameter to locate.</param>
    /// <returns><see langword="true"/> if the parameter is found; otherwise, <see langword="false"/>.</returns>
    public override bool Contains(string value)
        => _parameters.Any(p => p.ParameterName == value);
    /// <summary>
    ///     Adds a parameter to the collection.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    /// <returns>The parameter that was added.</returns>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public virtual HttpDbParameter AddWithValue(string? parameterName, object? value)
        => Add(new HttpDbParameter(parameterName, value));

    /// <summary>
    /// Finds the index of the parameter with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to locate.</param>
    /// <returns>The index of the parameter, or -1 if not found.</returns>
    public override int IndexOf(string parameterName)
        => _parameters.FindIndex(p => p.ParameterName == parameterName);

    /// <summary>
    /// Finds the index of a specific <see cref="DbParameter"/> in the collection.
    /// </summary>
    /// <param name="value">The <see cref="DbParameter"/> to locate.</param>
    /// <returns>The index of the parameter, or -1 if not found.</returns>
    public override int IndexOf(object value)
        => _parameters.IndexOf((HttpDbParameter)value);

    /// <summary>
    /// Inserts a <see cref="DbParameter"/> at the specified index in the collection.
    /// </summary>
    /// <param name="index">The index at which to insert the parameter.</param>
    /// <param name="value">The <see cref="DbParameter"/> to insert.</param>
    public override void Insert(int index, object value)
        => _parameters.Insert(index, (HttpDbParameter)value);

    /// <summary>
    /// Removes a specific <see cref="DbParameter"/> from the collection.
    /// </summary>
    /// <param name="value">The <see cref="DbParameter"/> to remove.</param>
    public override void Remove(object value)
        => _parameters.Remove((HttpDbParameter)value);

    /// <summary>
    /// Removes the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter to remove.</param>
    public override void RemoveAt(int index)
        => _parameters.RemoveAt(index);

    /// <summary>
    /// Removes the parameter with the specified name from the collection.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to remove.</param>
    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            RemoveAt(index);
    }

    /// <summary>
    /// Gets the <see cref="DbParameter"/> at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter to retrieve.</param>
    /// <returns>The <see cref="DbParameter"/> at the specified index.</returns>
    protected override DbParameter GetParameter(int index)
        => _parameters[index];

    /// <summary>
    /// Gets the <see cref="DbParameter"/> with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>The <see cref="DbParameter"/> with the specified name.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the parameter is not found.</exception>
    protected override DbParameter GetParameter(string parameterName)
        => _parameters.FirstOrDefault(p => p.ParameterName == parameterName)
            ?? throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");

    /// <summary>
    /// Sets the <see cref="DbParameter"/> at the specified index.
    /// </summary>
    /// <param name="index">The index at which to set the parameter.</param>
    /// <param name="value">The <see cref="DbParameter"/> to set.</param>
    protected override void SetParameter(int index, DbParameter value)
        => _parameters[index] = (HttpDbParameter)value;

    /// <summary>
    /// Sets the <see cref="DbParameter"/> with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to set.</param>
    /// <param name="value">The <see cref="DbParameter"/> to set.</param>
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters[index] = (HttpDbParameter)value;
        else
            Add(value);
    }

    /// <summary>
    /// Gets a value indicating whether the collection has a fixed size.
    /// </summary>
    public override bool IsFixedSize
        => false;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public override bool IsReadOnly
        => false;

    /// <summary>
    /// Gets a value indicating whether the collection is synchronized (thread-safe).
    /// </summary>
    public override bool IsSynchronized
        => false;

    /// <summary>
    /// Copies the elements of the collection to an array, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The index in the array to start copying.</param>
    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public override IEnumerator GetEnumerator()
        => _parameters.GetEnumerator();
}

