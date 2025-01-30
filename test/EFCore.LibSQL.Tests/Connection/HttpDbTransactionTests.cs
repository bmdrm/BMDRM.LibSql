// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Connection;
using System.Data;
using Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Contains unit tests for the <see cref="HttpDbTransaction"/> class.
/// Tests ensure that transactions behave correctly, including commit, rollback,
/// and handling exceptional cases.
/// </summary>
public class HttpDbTransactionTests : IClassFixture<HttpDbFixture>
{
    private string TestConnectionString = LibSqlTestSettings.ConnectionString;
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDbTransactionTests"/> class.
    /// </summary>
    /// <param name="fixture">Http client fixture.</param>
    public HttpDbTransactionTests(HttpDbFixture fixture)
    {
        this.httpClientFactory = fixture.HttpClientFactory;
    }

    /// <summary>
    /// Tests that calling Commit after a transaction has been rolled back throws an exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CommitAfterRollback_ShouldThrowException()
    {
        // Arrange
        await using var connection = new HttpDbConnection(TestConnectionString,  this.httpClientFactory);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        // Act
        await transaction.RollbackAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transaction.CommitAsync());
    }

    /// <summary>
    /// Tests that calling Rollback after a transaction has been committed throws an exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RollbackAfterCommit_ShouldThrowException()
    {
        // Arrange
        await using var connection = new HttpDbConnection(TestConnectionString,  this.httpClientFactory);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        // Act
        await transaction.CommitAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transaction.RollbackAsync());
    }

    /// <summary>
    /// Tests that a transaction prevents concurrent access to uncommitted changes.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TransactionIsolation_ShouldPreventConcurrentAccess()
    {
        // Arrange
        await using var connection = new HttpDbConnection(TestConnectionString,  this.httpClientFactory);
        await connection.OpenAsync();

        // Create table outside the transaction so that both connections have access
        await using var setupCommand = connection.CreateCommand();
        setupCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS test_isolation (id INTEGER PRIMARY KEY, value TEXT);
                INSERT INTO test_isolation (value) VALUES ('initial');
            ";
        await setupCommand.ExecuteNonQueryAsync();

        await using var command1 = connection.CreateCommand();
        command1.CommandText = "UPDATE test_isolation SET value = 'updated'";
        await command1.ExecuteNonQueryAsync();

        // Try to read from the same connection while the transaction is active
        using var command2 = connection.CreateCommand();
        command2.CommandText = "SELECT value FROM test_isolation";
        var value = await command2.ExecuteScalarAsync();


        command1.Transaction = null;
        command2.Transaction = null;
        command1.CommandText = "DROP TABLE test_isolation";
        await command1.ExecuteNonQueryAsync();

        // Assert - should see the updated value within the active transaction
        Assert.Equal("updated", value);
        connection.Close();
    }
}
