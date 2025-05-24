 alt# Running EFCore.LibSQL Tests

This document explains how to run the EFCore.LibSQL test suite.

## Prerequisites

- .NET SDK 6.0 or later
- Access to a LibSQL database

## Configuration

The tests require a LibSQL connection string in the following format:
```
url;apikey
```

For example:
```
https://your-database.turso.io/v2/pipeline;eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCJ9...
```

You can provide this connection string in two ways:

### 1. Using Environment Variables

Set the `LIBSQL_TEST_CONNECTION` environment variable before running the tests:

```bash
export LIBSQL_TEST_CONNECTION="your-connection-string"
dotnet test
```

Or in a single line:

```bash
LIBSQL_TEST_CONNECTION="your-connection-string" dotnet test
```

### 2. Using RunSettings File

1. Edit the `test.runsettings` file in the test project directory:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <LIBSQL_TEST_CONNECTION>your-connection-string-here</LIBSQL_TEST_CONNECTION>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

2. Run the tests using the settings file:

```bash
dotnet test --settings test.runsettings
```

## Running Specific Tests

To run a specific test or test class:

```bash
dotnet test --filter "FullyQualifiedName=Namespace.TestClass.TestMethod"
```

## Default Configuration

If no connection string is provided, the tests will use a default connection string pointing to a test database. However, it's recommended to provide your own connection string for testing.

## Troubleshooting

If you encounter connection issues:
1. Verify your connection string format
2. Ensure you have network access to the LibSQL server
3. Check that your authentication credentials are correct

For more information about the LibSQL provider, visit the [project repository](https://github.com/bmdrm/efcore.libsql).
