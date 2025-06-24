// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.TestUtilities
{
    internal class LibSqlTestStoreFactory : RelationalTestStoreFactory
    {
        public static LibSqlTestStoreFactory Instance { get; set; } = default!;
        private string TestConnectionString = LibSqlTestSettings.ConnectionString;
        private readonly IHttpClientFactory _httpClientFactory;
        public static LibSqlTestStoreFactory CreateInstance(IHttpClientFactory httpClientFactory)
            => new LibSqlTestStoreFactory(httpClientFactory);

        protected LibSqlTestStoreFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            Instance = new (httpClientFactory);
        }

        public override TestStore Create(string storeName)
            => LibSqlTestStore.Create(storeName, _httpClientFactory);

        public override TestStore GetOrCreate(string storeName)
            => LibSqlTestStore.GetOrCreate(storeName, _httpClientFactory);

        public TestStore GetOrCreate<TContext>(string storeName) where TContext : DbContext
        {
            var store = LibSqlTestStore.GetOrCreate(storeName, _httpClientFactory);
            return store;
        }

        public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
            => serviceCollection.AddEntityFrameworkLibSql();
    }
}

