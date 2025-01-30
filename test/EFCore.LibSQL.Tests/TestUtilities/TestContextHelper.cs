// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public static class TestContextHelper
{
    public static TContext CreateContext<TContext>(IServiceProvider serviceProvider) where TContext : DbContext
    {
        var context = serviceProvider.GetService<TContext>() ?? throw new InvalidOperationException($"Context must be of type {typeof(TContext).Name}");
        return context;
    }

    public static TContext AsContext<TContext>(this DbContext context) where TContext : DbContext
    {
        if (context is TContext typedContext)
        {
            return typedContext;
        }

        throw new InvalidOperationException($"Context must be of type {typeof(TContext).Name}");
    }
}
