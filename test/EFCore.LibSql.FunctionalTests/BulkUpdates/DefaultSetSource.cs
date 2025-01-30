// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.BulkUpdates;

/// <summary>
/// A default implementation of <see cref="ISetSource"/> that provides empty collections
/// for testing purposes.
/// </summary>
public class DefaultSetSource : ISetSource
{
    /// <summary>
    /// Gets an empty set of entities of the specified type.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities in the set.</typeparam>
    /// <returns>An empty <see cref="IQueryable{TEntity}"/>.</returns>
    public IQueryable<TEntity> GetSet<TEntity>()
        where TEntity : class
        => Array.Empty<TEntity>().AsQueryable();

    /// <summary>
    /// Gets an empty set of entities of the specified type.
    /// </summary>
    /// <typeparam name="TEntity">The type of entities in the set.</typeparam>
    /// <returns>An empty <see cref="IQueryable{TEntity}"/>.</returns>
    public IQueryable<TEntity> Set<TEntity>()
        where TEntity : class
        => Array.Empty<TEntity>().AsQueryable();
}
