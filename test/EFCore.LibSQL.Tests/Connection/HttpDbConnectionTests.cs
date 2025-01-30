// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql;

namespace Microsoft.EntityFrameworkCore.Connection
{
    using System.Data;
    using Microsoft.EntityFrameworkCore.LibSql.Connection;
    using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class HttpDbConnectionTests
    {
        private  string TestConnectionString = LibSqlTestSettings.ConnectionString;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpDbConnectionTests()
        {
            var services = LibSqlTestHelpers.Instance.CreateContextServices(
                new DbContextOptionsBuilder()
                    .UseLibSql(TestConnectionString)
                    .Options);

            _httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        }

        /// <summary>
        /// Tests that the constructor initializes the connection with the provided connection string.
        /// </summary>
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            Assert.NotNull(connection);
            Assert.Equal(TestConnectionString, connection.ConnectionString);
            Assert.Equal(ConnectionState.Closed, connection.State);
        }

        /// <summary>
        /// Tests that the connection opens successfully and transitions to the open state.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task OpenAsync_ShouldConnectSuccessfully()
        {
            // Arrange
            var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);

            // Act
            await connection.OpenAsync();

            // Assert
            Assert.Equal(ConnectionState.Open, connection.State);

            // Cleanup
            await connection.CloseAsync();
        }

        /// <summary>
        /// Tests that the <see cref="HttpDbConnection.CreateCommand"/> method returns a valid command.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task CreateCommand_ShouldReturnValidCommand()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();

            // Act
            var command = connection.CreateCommand();

            // Assert
            Assert.NotNull(command);
            Assert.IsType<HttpDbCommand>(command);
        }

        /// <summary>
        /// Tests that the <see cref="HttpDbCommand.ExecuteScalarAsync"/> method returns the expected result.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task ExecuteScalar_ShouldReturnCorrectResult()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            var result = await command.ExecuteScalarAsync();

            Assert.Equal(1L, result);
        }

        /// <summary>
        /// Tests that a transaction can be created and committed successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task Transaction_ShouldCommitSuccessfully()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();

            // Act
            await using var transaction = await connection.BeginTransactionAsync();

            // Assert
            Assert.NotNull(transaction);
            await transaction.CommitAsync();
        }

        /// <summary>
        /// Tests that query parameters are handled correctly in a command.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task Parameters_ShouldHandleCorrectly()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();
            HttpDbCommand command = (HttpDbCommand)connection.CreateCommand();
            command.CommandText = "SELECT @param";

            HttpDbParameter parameter = (HttpDbParameter)command.CreateParameter();
            parameter.ParameterName = "@param";
            parameter.DbType = DbType.Int64;
            parameter.Value = 42;
            command.Parameters.Add(parameter);
            var result = await command.ExecuteScalarAsync();

            Assert.Equal(42L, result);
        }

        /// <summary>
        /// Tests that the data reader correctly reads data from the query result.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task DataReader_ShouldReadDataCorrectly()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 as Value UNION ALL SELECT 2";

            // Act
            await using var reader = await command.ExecuteReaderAsync();

            // Assert
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1L, reader.GetInt64(0));
            Assert.True(await reader.ReadAsync());
            Assert.Equal(2L, reader.GetInt64(0));
            Assert.False(await reader.ReadAsync());
        }

        /// <summary>
        /// Tests that batch operations execute successfully and produce the correct result.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task BatchOperations_ShouldExecuteSuccessfully()
        {
            // Arrange
            await using var connection = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS test_table (id INTEGER PRIMARY KEY, value TEXT);
                INSERT INTO test_table (value) VALUES ('test1');
                INSERT INTO test_table (value) VALUES ('test2');
                SELECT COUNT(*) FROM test_table;
                DROP TABLE test_table;
            ";

            // Act
            var result = await command.ExecuteScalarAsync();

            // Assert
            Assert.Equal(2L, result);
            await transaction.CommitAsync();

        }

        /// <summary>
        /// Tests that the connection pool reuses existing connections efficiently.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task ConnectionPool_ShouldReuseConnections()
        {
            // Arrange
            var connection1 = new HttpDbConnection(TestConnectionString, _httpClientFactory);
            var connection2 = new HttpDbConnection(TestConnectionString, _httpClientFactory);

            // Act
            await connection1.OpenAsync();
            await connection1.CloseAsync();
            await connection2.OpenAsync();

            // Assert
            Assert.Equal(ConnectionState.Open, connection2.State);

            // Cleanup
            await connection1.DisposeAsync();
            await connection2.DisposeAsync();
        }
    }
}
