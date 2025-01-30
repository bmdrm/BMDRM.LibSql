// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Connection
{
    public class HttpDbFixture : IAsyncLifetime
    {
        public IHttpClientFactory HttpClientFactory { get; private set; }

        public async Task InitializeAsync()
        {
            // Create a basic service provider to create our HttpClientFactory
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            HttpClientFactory = serviceProvider.GetService<IHttpClientFactory>()!;
            await Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
