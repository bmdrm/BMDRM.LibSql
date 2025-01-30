// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.Internal;
using Microsoft.Extensions.Configuration;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

using Microsoft.EntityFrameworkCore.LibSql.Internal;

public class LibSqlConventionSetBuilderTests : ConventionSetBuilderTests
{
    private string ConnectionString = LibSqlTestSettings.ConnectionString;
    public override IReadOnlyModel Can_build_a_model_with_default_conventions_without_DI()
    {
        var model = base.Can_build_a_model_with_default_conventions_without_DI();

        Assert.Equal("ProductTable", model.GetEntityTypes().Single().GetTableName());

        return model;
    }

    [ConditionalFact]
    public override IReadOnlyModel Can_build_a_model_with_default_conventions_without_DI_new()
    {
        var modelBuilder = GetModelBuilder();
        modelBuilder.Entity<Product>();

        var model = modelBuilder.Model;
        Assert.NotNull(model.GetEntityTypes().Single());

        return model;
    }
    protected override ConventionSet GetConventionSet()
    {
        return LibSqlConventionSetBuilder.Build(ConnectionString);
    }

    protected override ModelBuilder GetModelBuilder()
        => LibSqlConventionSetBuilder.CreateModelBuilder(ConnectionString);
}
