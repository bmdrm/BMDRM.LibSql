// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.LibSql;

/// <summary>
/// Provides extension methods to configure LibSQL specific options for a <see cref="DbContext"/>.
/// </summary>
public class LibSqlDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<LibSqlDbContextOptionsBuilder, LibSqlOptionsExtension>
{
    private readonly DbContextOptionsBuilder _optionsBuilder;

    /// <summary>
    /// Initializes a new instance of <see cref="LibSqlDbContextOptionsBuilder"/>.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/> instance to configure.</param>
    public LibSqlDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder) :base(optionsBuilder)
    {
        _optionsBuilder = optionsBuilder;
    }

    /// <summary>
    /// Adds or updates LibSQL specific options to the <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <param name="optionsAction">Action to configure LibSQL options.</param>
    /// <returns>The updated <see cref="DbContextOptionsBuilder"/>.</returns>
    public DbContextOptionsBuilder UseLibSql(Action<LibSqlDbContextOptionsBuilder> optionsAction)
    {
        optionsAction?.Invoke(this);
        return _optionsBuilder;
    }

    /// <summary>
    /// Sets a custom connection string for the LibSQL database.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    public void SetConnectionString(string connectionString)
    {
        var extension = GetOrCreateExtension();
        extension.WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)_optionsBuilder).AddOrUpdateExtension(extension);
    }

    /// <summary>
    /// Sets a timeout value for database operations.
    /// </summary>
    /// <param name="timeoutInSeconds">The timeout in seconds.</param>
    public void SetTimeout(int timeoutInSeconds)
    {
        var extension = GetOrCreateExtension();
        extension = extension.WithTimeout(timeoutInSeconds);
        ((IDbContextOptionsBuilderInfrastructure)_optionsBuilder).AddOrUpdateExtension(extension);
    }

    /// <summary>
    /// Gets or creates the LibSQL options extension.
    /// </summary>
    private LibSqlOptionsExtension GetOrCreateExtension()
    {
        var extension = _optionsBuilder.Options.FindExtension<LibSqlOptionsExtension>();
        if (extension == null)
        {
            extension = new LibSqlOptionsExtension(string.Empty);
        }
        return extension;
    }
}
