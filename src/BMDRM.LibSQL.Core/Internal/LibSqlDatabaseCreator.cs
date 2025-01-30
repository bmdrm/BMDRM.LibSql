// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
namespace Microsoft.EntityFrameworkCore.LibSql.Internal;

/// <summary>
///     An internal class that supports the Entity Framework Core infrastructure for LibSQL database.
///     This class should only be used with caution as it may be changed or removed without notice.
/// </summary>
public class LibSqlDatabaseCreator : RelationalDatabaseCreator
{
    // ReSharper disable once InconsistentNaming
    private const int LIBSQL_CANTOPEN = 14;

    private readonly IRelationalConnection _connection;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

    /// <summary>
    ///     Initializes a new instance of <see cref="LibSqlDatabaseCreator"/>.
    /// </summary>
    public LibSqlDatabaseCreator(
        RelationalDatabaseCreatorDependencies dependencies,
        IRelationalConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder)
        : base(dependencies)
    {
        _connection = connection;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
    }

    /// <summary>
    ///     Creates the database if it doesn't exist.
    /// </summary>
    public override void Create()
    {
        Dependencies.Connection.Open();

        _rawSqlCommandBuilder.Build("PRAGMA journal_mode = 'wal';")
            .ExecuteNonQuery(
                new RelationalCommandParameterObject(
                    Dependencies.Connection,
                    null,
                    null,
                    null,
                    Dependencies.CommandLogger,
                    CommandSource.Migrations));

        Dependencies.Connection.Close();
    }

    /// <summary>
    ///     Checks if the database exists.
    /// </summary>
    public override bool Exists()
        => true;

    /// <summary>
    ///     Checks if the database has tables.
    /// </summary>
    public override bool HasTables()
    {
        var count = (long)_rawSqlCommandBuilder
            .Build("SELECT COUNT(*) FROM \"sqlite_master\" WHERE \"type\" = 'table' AND \"rootpage\" IS NOT NULL;")
            .ExecuteScalar(
                new RelationalCommandParameterObject(
                    Dependencies.Connection,
                    null,
                    null,
                    null,
                    Dependencies.CommandLogger,
                    CommandSource.Migrations))!;

        return count != 0;
    }

    /// <summary>
    ///     Deletes the database.
    /// </summary>
    public override void Delete()
    {
        string? path = null;

        Dependencies.Connection.Open();
        var dbConnection = Dependencies.Connection.DbConnection;
        try
        {
            path = dbConnection.DataSource;
        }
        catch
        {
            // Any exceptions here can be ignored
        }
        finally
        {
            Dependencies.Connection.Close();
        }

        if (!string.IsNullOrEmpty(path))
        {
            // This would be specific to the database pool management for LibSQL
            // SqliteConnection.ClearPool(new SqliteConnection(Dependencies.Connection.ConnectionString));
            File.Delete(path); // Delete database file
        }
        else if (dbConnection.State == ConnectionState.Open)
        {
            dbConnection.Close();
            // SqliteConnection.ClearPool(new SqliteConnection(Dependencies.Connection.ConnectionString)); // Clear pool
            dbConnection.Open();
        }
    }
}
