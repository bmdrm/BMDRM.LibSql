// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Microsoft.EntityFrameworkCore.Connection;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using System;

namespace Microsoft.EntityFrameworkCore.Storage;

public class GuidsCommandTest : IClassFixture<HttpDbFixture>
{
    private readonly string _testConnectionString = LibSqlTestSettings.ConnectionString;
    private readonly IHttpClientFactory _httpClientFactory;

    public GuidsCommandTest(HttpDbFixture fixture)
    {
        this._httpClientFactory = fixture.HttpClientFactory;
    }


    [Fact]
    public async Task GuidField_Insert_WithLowercaseString_ShouldBeCorrectlySaved()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestGuidsLowerCase(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GuidValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        // 1. Insert
        string guidValue = "15c7b691-7a52-4fa6-9395-79f52a63df0b";
        command.CommandText = @"
                INSERT INTO TestGuidsLowerCase (GuidValue)
                VALUES (@p1);
            ";
        var p1 = new HttpDbParameter("@p1", DbType.String, guidValue);
        command.Parameters.Add(p1);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Get the record
        command.CommandText = "SELECT Id, GuidValue FROM TestGuidsLowerCase;";
        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedGuidValue = dataReader.GetString(1);

        // Assert: GUID string should save as lowercase
        Assert.Equal(guidValue, savedGuidValue);
        await dataReader.CloseAsync();
        command.Parameters.Clear();

        // Cleanup
        command.CommandText = "DROP TABLE TestGuidsLowerCase";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task GuidField_Insert_WithUpperCaseString_ShouldBeCorrectlySavedAsLowerCase()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestGuidsUpperCaseString(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GuidValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        // 1. Insert
        string guidValue = "15C7B691-7A52-4FA6-9395-79F52A63DF0B";
        command.CommandText = @"
                INSERT INTO TestGuidsUpperCaseString (GuidValue)
                VALUES (@p1);
            ";

        // Enforce lowercase during the insert
        var p1 = new HttpDbParameter("@p1", DbType.String, guidValue.ToLowerInvariant());
        command.Parameters.Add(p1);


        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Get the record
        command.CommandText = "SELECT Id, GuidValue FROM TestGuidsUpperCaseString;";
        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedGuidValue = dataReader.GetString(1);

        // Assert: GUID string should save as lowercase
        Assert.Equal(guidValue.ToLowerInvariant(), savedGuidValue);
        await dataReader.CloseAsync();
        command.Parameters.Clear();
        // Cleanup
        command.CommandText = "DROP TABLE TestGuidsUpperCaseString";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task GuidField_InsertAndRetrieve_WithMixedCaseString_ShouldBeLowercase()
    {
        // Arrange
         await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestGuidsMixedCase(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GuidValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
        string guidValue = "15c7B691-7A52-4fa6-9395-79F52a63DF0b";

        command.CommandText = @"
                INSERT INTO TestGuidsMixedCase (GuidValue)
                VALUES (@p1);
            ";

        var p1 = new HttpDbParameter("@p1", DbType.String, guidValue.ToLowerInvariant());
        command.Parameters.Add(p1);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Get the record
         command.CommandText = "SELECT Id, GuidValue FROM TestGuidsMixedCase;";
         using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedGuidValue = dataReader.GetString(1);

        Assert.Equal(guidValue.ToLowerInvariant(), savedGuidValue);

        await dataReader.CloseAsync();
        command.Parameters.Clear();

         // Cleanup
        command.CommandText = "DROP TABLE TestGuidsMixedCase";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
}
