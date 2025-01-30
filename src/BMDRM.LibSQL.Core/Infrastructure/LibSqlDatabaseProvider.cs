// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



namespace Microsoft.EntityFrameworkCore.LibSql.Infrastructure;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
/// <summary>
/// Represents a provider for LibSQL database configurations.
/// Implements <see cref="IDatabaseProvider"/> to determine if the database is properly configured and to provide the database provider's name.
/// </summary>
public class LibSqlDatabaseProvider : IDatabaseProvider
{
    /// <summary>
    /// Determines if the LibSQL database is configured by checking if the <see cref="LibSqlOptionsExtension"/> exists in the provided <see cref="IDbContextOptions"/>.
    /// </summary>
    /// <param name="options">The database context options to check for the configuration.</param>
    /// <returns><see langword="true"/> if the LibSQL configuration is present, otherwise <see langword="false"/>.</returns>
    public bool IsConfigured(IDbContextOptions options)
    {
        return options.Extensions.OfType<LibSqlOptionsExtension>().Any();
    }

    /// <summary>
    /// Gets the name of the LibSQL database provider.
    /// </summary>
    public string Name => "LibSQL";
}
