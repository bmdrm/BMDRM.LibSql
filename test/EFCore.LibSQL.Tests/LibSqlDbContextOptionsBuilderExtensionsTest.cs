// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.LibSql;
using Microsoft.EntityFrameworkCore.TestUtilities;

public class LibSqlDbContextOptionsBuilderExtensionsTest
{
    [ConditionalFact]
    public void Can_add_extension_with_command_timeout()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseLibSql(LibSqlTestSettings.ConnectionString, b => b.SetTimeout(30));

        var extension = optionsBuilder.Options.Extensions.OfType<LibSqlOptionsExtension>().Single();

        Assert.Equal(30, extension.CommandTimeout);
    }

    [ConditionalFact]
    public void Can_add_extension_with_connection_string()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseLibSql(LibSqlTestSettings.ConnectionString);

        var extension = optionsBuilder.Options.Extensions.OfType<LibSqlOptionsExtension>().Single();

        Assert.Equal(LibSqlTestSettings.ConnectionString, extension.ConnectionString);
        Assert.Null(extension.Connection);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Can_add_extension_with_connection_string_using_generic_options(bool nullConnectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>();

        if (nullConnectionString)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                optionsBuilder.UseLibSql(null);
            });

            Assert.Equal($"Connection string '' is invalid. It should be in the form https://url/v2/pipeline;token", ex.Message);
        }
        else
        {
            optionsBuilder.UseLibSql(LibSqlTestSettings.ConnectionString);

            var extension = optionsBuilder.Options.Extensions
                .OfType<LibSqlOptionsExtension>()
                .Single();

            Assert.Equal(LibSqlTestSettings.ConnectionString, extension.ConnectionString);
            Assert.Null(extension.Connection);
        }
    }

}
