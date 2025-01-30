// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.LibSql.Connection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Http;

namespace Microsoft.EntityFrameworkCore.LibSql.Connection
{
    /// <summary>
    /// Represents a connection to a LibSQL database that extends the relational connection functionality.
    /// This class overrides the <see cref="CreateDbConnection"/> method to create an instance of <see cref="HttpDbConnection"/>.
    /// </summary>
    public class LibSqlConnection : RelationalConnection
    {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibSqlConnection"/> class with the specified dependencies.
        /// </summary>
        /// <param name="dependencies">The dependencies required for the relational connection.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public LibSqlConnection(RelationalConnectionDependencies dependencies, IHttpClientFactory httpClientFactory)
            : base(dependencies)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a new <see cref="DbConnection"/> instance using the connection string.
        /// This method is overridden to return an <see cref="HttpDbConnection"/> instance.
        /// </summary>
        /// <returns>A new instance of <see cref="HttpDbConnection"/> initialized with the connection string.</returns>
        protected override DbConnection CreateDbConnection()
        {
            return new HttpDbConnection(ConnectionString!, _httpClientFactory);
        }
    }
}
