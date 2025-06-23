// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Connection;
using System.Text.Json;

namespace Microsoft.EntityFrameworkCore.Storage;

public class HttpDbCommandDataTypeTests(HttpDbFixture fixture) : IClassFixture<HttpDbFixture>
{
    private readonly string _testConnectionString = LibSqlTestSettings.ConnectionString;
    private readonly IHttpClientFactory _httpClientFactory = fixture.HttpClientFactory;

    [Fact]
    public async Task IntegerFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, _httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Create table
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestIntegers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            IntValue INTEGER,
            LongValue INTEGER,
            ShortValue INTEGER
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
        int intValue = 12345;
        long longValue = 9876543210;
        short shortValue = 123;

        command.CommandText = @"
                INSERT INTO TestIntegers (IntValue, LongValue, ShortValue)
                VALUES (@p1, @p2, @p3);
            ";
        var p1 = new HttpDbParameter("@p1", intValue);
        p1.DbType = DbType.Int32;
        command.Parameters.Add(p1);

        var p2 = new HttpDbParameter("@p2", longValue);
        p2.DbType = DbType.Int64;
        command.Parameters.Add(p2);

        var p3 = new HttpDbParameter("@p3", shortValue);
        p3.DbType = DbType.Int16;
        command.Parameters.Add(p3);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        //Get the record
        command.CommandText = "SELECT Id, IntValue, LongValue, ShortValue FROM TestIntegers;";

        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedIntValue = dataReader.GetInt32(1);
        var savedLongValue = dataReader.GetInt64(2);
        var savedShortValue = dataReader.GetInt16(3);

        Assert.Equal(intValue, savedIntValue);
        Assert.Equal(longValue, savedLongValue);
        Assert.Equal(shortValue, savedShortValue);

        await dataReader.CloseAsync();
        command.Parameters.Clear();
        //Cleanup
        command.CommandText = "DROP TABLE TestIntegers";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task StringFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Create table
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestStrings (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TextValue TEXT,
             GuidValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
        string textValue = "Hello, World!";
        Guid guidValue = Guid.NewGuid();

        command.CommandText = @"
                INSERT INTO TestStrings (TextValue, GuidValue)
                VALUES (@p1, @p2);
            ";
        var p1 = new HttpDbParameter("@p1", textValue);
        p1.DbType = DbType.String;
        command.Parameters.Add(p1);

        var p2 = new HttpDbParameter("@p2", guidValue.ToString());
        p2.DbType = DbType.String;
        command.Parameters.Add(p2);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        //Get the record
        command.CommandText = "SELECT Id, TextValue, GuidValue FROM TestStrings;";

        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedTextValue = dataReader.GetString(1);
        var savedGuidValue = dataReader.GetString(2);

        Assert.Equal(textValue, savedTextValue);
        Assert.Equal(guidValue.ToString(), savedGuidValue);

        await dataReader.CloseAsync();
        command.Parameters.Clear();
        //Cleanup
        command.CommandText = "DROP TABLE TestStrings";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task DecimalAndDoubleFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestDecimals (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DecimalValue REAL,
             DoubleValue REAL
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM TestDecimals;";
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
        const decimal decimalValue = 1234.567m;
        const double doubleValue = 987.654321;

        command.CommandText = @"
                INSERT INTO TestDecimals (DecimalValue, DoubleValue)
                VALUES (@p1, @p2);
            ";
        var p1 = new HttpDbParameter("@p1", DbType.Decimal, decimalValue);
        command.Parameters.Add(p1);

        var p2 = new HttpDbParameter("@p2", DbType.Double, doubleValue);
        command.Parameters.Add(p2);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        //Get the record
        command.CommandText = "SELECT Id, DecimalValue, DoubleValue FROM TestDecimals;";

        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedDecimalValue = dataReader.GetDecimal(1);
        var savedDoubleValue = dataReader.GetDouble(2);

        Assert.Equal(decimalValue, savedDecimalValue);
        Assert.Equal(doubleValue, savedDoubleValue);
        await dataReader.CloseAsync();
        command.Parameters.Clear();

        //Cleanup
        command.CommandText = "DROP TABLE TestDecimals";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task BooleanFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Create table
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestBooleans (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BooleanValue INTEGER
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM TestBooleans;";
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        // 1. Insert
        bool boolValue = true;

        command.CommandText = @"
                INSERT INTO TestBooleans (BooleanValue)
                VALUES (@p1);
            ";
        var p1 = new HttpDbParameter("@p1", boolValue);
        p1.DbType = DbType.Boolean;
        command.Parameters.Add(p1);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        //Get the record
        command.CommandText = "SELECT Id, BooleanValue FROM TestBooleans;";

        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedBoolValue = dataReader.GetBoolean(1);

        Assert.Equal(boolValue, savedBoolValue);
        await dataReader.CloseAsync();
        command.Parameters.Clear();
        //Cleanup
        command.CommandText = "DROP TABLE TestBooleans";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task DateTimeOffsetFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Create table
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestDateTimeOffsets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            OffsetValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
        var dateTimeOffsetValue = DateTimeOffset.UtcNow;
        command.CommandText = @"
                INSERT INTO TestDateTimeOffsets (OffsetValue)
                VALUES (@p1);
            ";
        var p1 = new HttpDbParameter("@p1", dateTimeOffsetValue.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)); // Explicitly formatting to string before passing to HttpDbParameter
        p1.DbType = DbType.DateTimeOffset;
        command.Parameters.Add(p1);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        //Get the record
        command.CommandText = "SELECT Id, OffsetValue FROM TestDateTimeOffsets;";
        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedOffsetValue = dataReader.GetString(1);
        var parsedDateTimeOffset = DateTimeOffset.Parse(savedOffsetValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        Assert.Equal(dateTimeOffsetValue.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), parsedDateTimeOffset.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)); // comparing string representation

        await dataReader.CloseAsync();
        command.Parameters.Clear();
        command.CommandText = "DROP TABLE TestDateTimeOffsets";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
    [Fact]
    public async Task Insert_WithGuidDoubleAndIntegerParameters_ShouldWorkCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS MonitoredNodes (
            Id TEXT PRIMARY KEY,
            Score REAL,
            VideoCount INTEGER
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.CommandText = "DELETE FROM MonitoredNodes;";
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        // Create a guid, a double and a integer
        var nodeId = Guid.NewGuid();
        const double score = 222;
        const long videoCount = 33;

        // Set up the SQL command and parameters
        command.CommandText = @"
            INSERT INTO ""MonitoredNodes"" (""Id"", ""Score"", ""VideoCount"")
            VALUES (@p0, @p1, @p2);
        ";

        var p0 = new HttpDbParameter("@p0", DbType.Guid, nodeId);
        command.Parameters.Add(p0);

        var p1 = new HttpDbParameter("@p1", DbType.Double , score);
        command.Parameters.Add(p1);

        var p2 = new HttpDbParameter("@p2", videoCount);
        p2.DbType = DbType.Int64;
        command.Parameters.Add(p2);

        // Act
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        // Assert
        command.CommandText = "SELECT Id, Score, VideoCount FROM MonitoredNodes;";
        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var savedNodeId = dataReader.GetString(0);
        var savedScore = dataReader.GetDouble(1);
        var savedVideoCount = dataReader.GetInt64(2);

        Assert.Equal(nodeId.ToString(), savedNodeId);
        Assert.Equal(score, savedScore);
        Assert.Equal(videoCount, savedVideoCount);
        await dataReader.CloseAsync();
        command.Parameters.Clear();
        command.CommandText = "DROP TABLE MonitoredNodes";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

    [Fact]
    public async Task DateTimeFields_ShouldHandleAllDateTimeKinds()
    {
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS TestDateTimes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UtcValue TEXT,
            LocalValue TEXT,
            UnspecifiedValue TEXT,
            OffsetValue TEXT
        );
    ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Test data
        var utcDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var localDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Local);
        var unspecifiedDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Unspecified);
        var offsetDateTime = new DateTimeOffset(2024, 1, 15, 14, 30, 45, 123, TimeSpan.FromHours(2));

        command.CommandText = @"
        INSERT INTO TestDateTimes (UtcValue, LocalValue, UnspecifiedValue, OffsetValue)
        VALUES (@p1, @p2, @p3, @p4);
    ";

        command.Parameters.Add(new HttpDbParameter("@p1", DbType.DateTime, utcDateTime));
        command.Parameters.Add(new HttpDbParameter("@p2", DbType.DateTime, localDateTime));
        command.Parameters.Add(new HttpDbParameter("@p3", DbType.DateTime, unspecifiedDateTime));
        command.Parameters.Add(new HttpDbParameter("@p4", DbType.DateTimeOffset, offsetDateTime));

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Verify storage
        command.CommandText = "SELECT UtcValue, LocalValue, UnspecifiedValue, OffsetValue, typeof(UtcValue) FROM TestDateTimes;";
       await using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var utcStoredValue = dataReader.GetDateTime(0);
        var localStoredValue = dataReader.GetDateTime(1);
        var unspecifiedStoredValue = dataReader.GetDateTime(2);
        var offsetStoredValue = dataReader.GetDateTime(3);
        var storedType = dataReader.GetString(4);

        Assert.Equal("text", storedType.ToLower());

        Assert.Equal(DateTimeKind.Unspecified, utcStoredValue.Kind);
        Assert.Equal(DateTimeKind.Unspecified, localStoredValue.Kind);
        Assert.Equal(DateTimeKind.Unspecified, unspecifiedStoredValue.Kind);


        void VerifyDateTime(DateTime expected, DateTime actual)
        {
            Assert.Equal(expected.Year, actual.Year);
            Assert.Equal(expected.Month, actual.Month);
            Assert.Equal(expected.Day, actual.Day);
            Assert.Equal(expected.Hour, actual.Hour);
            Assert.Equal(expected.Minute, actual.Minute);
            Assert.Equal(expected.Second, actual.Second);
            Assert.Equal(expected.Millisecond, actual.Millisecond);
        }

        var expectedUtc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Unspecified);
        var expectedLocal = DateTime.SpecifyKind(localDateTime.ToUniversalTime(), DateTimeKind.Unspecified);
        var expectedUnspecified = unspecifiedDateTime;
        var expectedOffset = offsetDateTime.ToOffset(TimeSpan.Zero).DateTime;

        VerifyDateTime(expectedUtc, utcStoredValue);
        VerifyDateTime(expectedLocal, localStoredValue);
        VerifyDateTime(expectedUnspecified, unspecifiedStoredValue);

        Assert.Equal(expectedOffset.Year, offsetStoredValue.Year);
        Assert.Equal(expectedOffset.Month, offsetStoredValue.Month);
        Assert.Equal(expectedOffset.Day, offsetStoredValue.Day);
        Assert.Equal(expectedOffset.Hour, offsetStoredValue.Hour);
        Assert.Equal(expectedOffset.Minute, offsetStoredValue.Minute);
        Assert.Equal(expectedOffset.Second, offsetStoredValue.Second);
        Assert.Equal(expectedOffset.Millisecond, offsetStoredValue.Millisecond);


        await dataReader.CloseAsync();
        command.Parameters.Clear();

        // Cleanup
        command.CommandText = "DROP TABLE TestDateTimes";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
    [Fact]
    public async Task DateTimeFieldsUpdate_ShouldBeHandledCorrectly()
    {
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS TestEntity (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ChangedAt TEXT,
        StartedAt TEXT,
        Name TEXT,
        Version INTEGER
        );
    ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Use specific datetimes for predictable testing
        var initialChangedAt = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var initialStartedAt = new DateTime(2024, 1, 15, 15, 30, 45, 123, DateTimeKind.Utc);
        var expectedChangedAtString = "2024-01-15 14:30:45.123";
        var expectedStartedAtString = "2024-01-15 15:30:45.123";
        var entityName = "Test Entity";
        var initialVersion = 1;

        // Insert
        command.CommandText = @"
            INSERT INTO TestEntity (ChangedAt, StartedAt, Name, Version)
            VALUES (@p1, @p2, @p3, @p4);
        ";
        command.Parameters.Add(new HttpDbParameter("@p1", DbType.DateTime, initialChangedAt));
        command.Parameters.Add(new HttpDbParameter("@p2", DbType.DateTime, initialStartedAt));
        command.Parameters.Add(new HttpDbParameter("@p3", DbType.String, entityName));
        command.Parameters.Add(new HttpDbParameter("@p4", DbType.Int32, initialVersion));

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Verify initial insert with raw text values
        command.CommandText = "SELECT Id, ChangedAt, StartedAt, typeof(ChangedAt), typeof(StartedAt) FROM TestEntity WHERE Name = @p1;";
        command.Parameters.Add(new HttpDbParameter("@p1", DbType.String, entityName));

        using var dataReader = await command.ExecuteReaderAsync();
        Assert.True(await dataReader.ReadAsync(), "No record found after insert");

        var recordId = dataReader.GetInt32(0);
        var rawChangedAt = dataReader.GetString(1);
        var rawStartedAt = dataReader.GetString(2);
        var changedAtType = dataReader.GetString(3);
        var startedAtType = dataReader.GetString(4);

        // Verify proper SQLite storage
        Assert.Equal("text", changedAtType.ToLower());
        Assert.Equal("text", startedAtType.ToLower());
        Assert.Equal(expectedChangedAtString, rawChangedAt);
        Assert.Equal(expectedStartedAtString, rawStartedAt);

        // Verify GetDateTime conversion
        var savedChangedAt = dataReader.GetDateTime(1);
        var savedStartedAt = dataReader.GetDateTime(2);
        Assert.Equal(DateTimeKind.Unspecified, savedChangedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, savedStartedAt.Kind);

        await dataReader.CloseAsync();
        command.Parameters.Clear();

        // Update with new datetime
        var updatedChangedAt = new DateTime(2024, 1, 15, 16, 30, 45, 123, DateTimeKind.Utc);
        var expectedUpdatedString = "2024-01-15 16:30:45.123";
        var updatedVersion = 2;

        command.CommandText = @"
            UPDATE TestEntity
            SET ChangedAt = @p1, Version = @p2
            WHERE Id = @p3 AND Version = @p4
        ";
        command.Parameters.Add(new HttpDbParameter("@p1", DbType.DateTime, updatedChangedAt));
        command.Parameters.Add(new HttpDbParameter("@p2", DbType.Int32, updatedVersion));
        command.Parameters.Add(new HttpDbParameter("@p3", DbType.Int32, recordId));
        command.Parameters.Add(new HttpDbParameter("@p4", DbType.Int32, initialVersion));

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Verify update with raw text values
        command.CommandText = "SELECT ChangedAt, StartedAt, typeof(ChangedAt), typeof(StartedAt) FROM TestEntity WHERE Id = @p1";
        command.Parameters.Add(new HttpDbParameter("@p1", DbType.Int32, recordId));

        await using var dataReader2 = await command.ExecuteReaderAsync();
        Assert.True(await dataReader2.ReadAsync(), "No record found after update");

        var rawUpdatedChangedAt = dataReader2.GetString(0);
        var rawUpdatedStartedAt = dataReader2.GetString(1);

        // Verify stored format
        Assert.Equal(expectedUpdatedString, rawUpdatedChangedAt);
        Assert.Equal(expectedStartedAtString, rawUpdatedStartedAt);

        // Verify GetDateTime conversion
        var retrievedChangedAt = dataReader2.GetDateTime(0);
        var retrievedStartedAt = dataReader2.GetDateTime(1);
        Assert.Equal(DateTimeKind.Unspecified, retrievedChangedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, retrievedStartedAt.Kind);

        await dataReader2.CloseAsync();
        command.Parameters.Clear();

        // Cleanup
        command.CommandText = "DROP TABLE TestEntity";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
[Fact]
    public async Task DateTimeFields_ShouldHandleAllDateTimeKinds_WithFullStringVerification()
    {
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS TestDateTimesFullVerification (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UtcValue TEXT,
            LocalValue TEXT,
            UnspecifiedValue TEXT,
            OffsetValue TEXT
        );
    ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Test data
        var utcDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var localDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 456, DateTimeKind.Local);
        var unspecifiedDateTime = new DateTime(2024, 1, 15, 14, 30, 45, 789, DateTimeKind.Unspecified);
        var offsetDateTime = new DateTimeOffset(2024, 1, 15, 14, 30, 45, 321, TimeSpan.FromHours(2));

          command.CommandText = @"
        INSERT INTO TestDateTimesFullVerification (UtcValue, LocalValue, UnspecifiedValue, OffsetValue)
        VALUES (@p1, @p2, @p3, @p4);
    ";
        // Insert the date as string to test the whole pipeline
          command.Parameters.Add(new HttpDbParameter("@p1", DbType.DateTime, utcDateTime));
        command.Parameters.Add(new HttpDbParameter("@p2", DbType.DateTime, localDateTime));
        command.Parameters.Add(new HttpDbParameter("@p3", DbType.DateTime, unspecifiedDateTime));
         command.Parameters.Add(new HttpDbParameter("@p4", DbType.DateTimeOffset, offsetDateTime));

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Verify storage
        command.CommandText = "SELECT UtcValue, LocalValue, UnspecifiedValue, OffsetValue FROM TestDateTimesFullVerification;";
        await using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var utcStoredValue = dataReader.GetString(0);
        var localStoredValue = dataReader.GetString(1);
        var unspecifiedStoredValue = dataReader.GetString(2);
        var offsetStoredValue = dataReader.GetString(3);

      var expectedUtcString  =  utcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      var expectedLocalString = localDateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      var expectedUnspecifiedString = unspecifiedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      var expectedOffsetString = offsetDateTime.ToOffset(TimeSpan.Zero).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);


      Assert.Equal(expectedUtcString, utcStoredValue);
      Assert.Equal(expectedLocalString, localStoredValue);
      Assert.Equal(expectedUnspecifiedString, unspecifiedStoredValue);
      Assert.Equal(expectedOffsetString, offsetStoredValue);
      // Verify GetDateTime conversion and DateTimeKind
      var retrievedUtcDateTime  = dataReader.GetDateTime(0);
      var retrievedLocalDateTime = dataReader.GetDateTime(1);
      var retrievedUnspecifiedDateTime = dataReader.GetDateTime(2);

      Assert.Equal(DateTimeKind.Unspecified, retrievedUtcDateTime.Kind);
      Assert.Equal(DateTimeKind.Unspecified, retrievedLocalDateTime.Kind);
      Assert.Equal(DateTimeKind.Unspecified, retrievedUnspecifiedDateTime.Kind);

      var parsedOffsetValue = DateTimeOffset.Parse(offsetStoredValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
      Assert.Equal(offsetDateTime, parsedOffsetValue);

        await dataReader.CloseAsync();
        command.Parameters.Clear();

        // Cleanup
        command.CommandText = "DROP TABLE TestDateTimesFullVerification";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
    [Fact]
    public async Task DoubleField_ShouldBeHandledCorrectly()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestDoubles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
             DoubleValue REAL
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM TestDoubles;";
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        const double doubleValue = 0.03939814814814815;

        command.CommandText = @"
                INSERT INTO TestDoubles (DoubleValue)
                VALUES (@p1);
            ";


        var p1 = new HttpDbParameter("@p1", DbType.Double, doubleValue);
        command.Parameters.Add(p1);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        //Get the record
        command.CommandText = "SELECT Id, DoubleValue FROM TestDoubles;";

        await using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedDoubleValue = dataReader.GetDouble(1);

        Assert.Equal(doubleValue, savedDoubleValue);
        await dataReader.CloseAsync();
        command.Parameters.Clear();

        // Cleanup
        command.CommandText = "DROP TABLE TestDoubles";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }
    [Fact]
    public async Task GuidField_InsertAndRetrieve_ShouldPreserveCase()
    {
        // Arrange
        await using var connection = new HttpDbConnection(_testConnectionString, this._httpClientFactory);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
           CREATE TABLE IF NOT EXISTS TestGuids (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GuidValue TEXT
            );
        ";

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // 1. Insert
         string guidValue = "15c7b691-7a52-4fa6-9395-79f52a63df0b";


         command.CommandText = @"
                INSERT INTO TestGuids (GuidValue)
                VALUES (@p1);
            ";
        var p1 = new HttpDbParameter("@p1", DbType.Guid, Guid.Parse(guidValue));
        command.Parameters.Add(p1);

        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        // Get the record
        command.CommandText = "SELECT Id, GuidValue FROM TestGuids;";
        using var dataReader = await command.ExecuteReaderAsync();
        await dataReader.ReadAsync();

        var recordId = dataReader.GetInt32(0);
        var savedGuidValue = dataReader.GetString(1);

        Assert.Equal(guidValue, savedGuidValue);


        await dataReader.CloseAsync();
        command.Parameters.Clear();

        command.CommandText = "DROP TABLE TestGuids";
        await command.ExecuteNonQueryAsync();
        connection.Close();
    }

}
