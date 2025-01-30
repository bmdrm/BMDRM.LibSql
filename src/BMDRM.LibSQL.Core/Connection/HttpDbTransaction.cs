// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.LibSql.Connection;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Represents a transaction for the HTTP database connection.
/// </summary>
public class HttpDbTransaction : DbTransaction
{
    private readonly HttpDbConnection _connection;
    private bool _isDisposed;
    private bool _isCommitted;
    private bool _isRolledBack;

    /// <summary>
    /// Gets the <see cref="HttpDbConnection"/> associated with this transaction.
    /// </summary>
    protected override DbConnection DbConnection => _connection;

    /// <summary>
    /// Gets the isolation level for this transaction.
    /// </summary>
    public override IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets the transaction mode for this transaction.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the transaction ID for this transaction.
    /// </summary>
    public string? TransactionId { get; internal set; }

    internal HttpDbTransaction(HttpDbConnection connection, IsolationLevel isolationLevel, string mode)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        IsolationLevel = isolationLevel;
        Mode = mode;
    }

    /// <summary>
    /// Commits the database transaction.
    /// </summary>
    public override void Commit()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(HttpDbTransaction));
        }

        if (_isCommitted)
        {
            throw new InvalidOperationException("The transaction has already been committed.");
        }

        if (_isRolledBack)
        {
            throw new InvalidOperationException("The transaction has already been rolled back.");
        }
        _isCommitted = true;
    }

    /// <summary>
    /// Rolls back the database transaction.
    /// </summary>
    public override void Rollback()
    {
        if (!_isDisposed)
        {
            if (_isCommitted)
            {
                throw new InvalidOperationException("The transaction has already been committed.");
            }

            if (_isRolledBack)
            {
                throw new InvalidOperationException("The transaction has already been rolled back.");
            }
            _isRolledBack = true;
        }
        else
        {
            throw new ObjectDisposedException(nameof(HttpDbTransaction));
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the transaction and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (!_isCommitted && !_isRolledBack)
                {
                    try
                    {
                        Rollback();
                    }
                    catch
                    {
                    }
                }
            }
            _isDisposed = true;
        }
        base.Dispose(disposing);
    }
}
