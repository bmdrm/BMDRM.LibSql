// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Diagnostics;
// using Microsoft.EntityFrameworkCore.Infrastructure;
// using Microsoft.EntityFrameworkCore.Metadata;
// using Microsoft.EntityFrameworkCore.Query;
// using Microsoft.EntityFrameworkCore.Storage;
// using System;
// using System.Linq.Expressions;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore.Query.Internal;
//
// namespace Microsoft.EntityFrameworkCore.LibSql.Infrastructure
// {
//     /// <summary>
//     /// A custom query compiler for LibSQL, implementing <see cref="IQueryCompiler"/>.
//     /// This class is responsible for compiling and executing queries specific to the LibSQL provider.
//     /// </summary>
//     public class LibSqlQueryCompiler : IQueryCompiler
//     {
//         private readonly DatabaseProvider<LibSqlOptionsExtension> _database;
//         private readonly IQueryContextFactory _queryContextFactory;
//         private readonly ICompiledQueryCache _compiledQueryCache;
//         private readonly ICompiledQueryCacheKeyGenerator _compiledQueryCacheKeyGenerator;
//         private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
//         private readonly IEvaluatableExpressionFilter _evaluatableExpressionFilter;
//
//         /// <summary>
//         /// Initializes a new instance of the <see cref="LibSqlQueryCompiler"/> class.
//         /// </summary>
//         /// <param name="queryContextFactory">The factory used to create query context instances.</param>
//         /// <param name="compiledQueryCache">The cache that stores compiled queries.</param>
//         /// <param name="compiledQueryCacheKeyGenerator">The key generator used for compiled query cache.</param>
//         /// <param name="database">The database to execute queries against.</param>
//         /// <param name="logger">The logger used for logging query-related events.</param>
//         /// <param name="evaluatableExpressionFilter">The filter used to evaluate expressions in the query.</param>
//         public LibSqlQueryCompiler(
//             IQueryContextFactory queryContextFactory,
//             ICompiledQueryCache compiledQueryCache,
//             ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
//             DatabaseProvider<LibSqlOptionsExtension> database,
//             IDiagnosticsLogger<DbLoggerCategory.Query> logger,
//             IEvaluatableExpressionFilter evaluatableExpressionFilter)
//         {
//             _queryContextFactory = queryContextFactory;
//             _compiledQueryCache = compiledQueryCache;
//             _compiledQueryCacheKeyGenerator = compiledQueryCacheKeyGenerator;
//             _database = database;
//             _logger = logger;
//             _evaluatableExpressionFilter = evaluatableExpressionFilter;
//         }
//
//         /// <summary>
//         /// Executes a query synchronously and retrieves the result using LibSQL.
//         /// </summary>
//         /// <typeparam name="TResult">The type of the result.</typeparam>
//         /// <param name="query">The query expression to execute.</param>
//         /// <returns>The result of the executed query.</returns>
//         public TResult Execute<TResult>(Expression query)
//         {
//             var queryContext = _queryContextFactory.Create();
//             query = ExtractParameters(query, queryContext);
//
//             var compiledQuery = _compiledQueryCache.GetOrAddQuery(
//                 _compiledQueryCacheKeyGenerator.GenerateCacheKey(query, async: false),
//                 () => CompileQueryCore<TResult>(_database, query, false));
//
//             return compiledQuery(queryContext);
//         }
//
//         /// <summary>
//         /// Executes a query asynchronously and retrieves the result using LibSQL.
//         /// </summary>
//         /// <typeparam name="TResult">The type of the result.</typeparam>
//         /// <param name="query">The query expression to execute.</param>
//         /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
//         /// <returns>A task representing the asynchronous query execution.</returns>
//         public TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
//         {
//             var queryContext = _queryContextFactory.Create();
//             query = ExtractParameters(query, queryContext);
//
//             var compiledQuery = _compiledQueryCache.GetOrAddQuery(
//                 _compiledQueryCacheKeyGenerator.GenerateCacheKey(query, async: true),
//                 () => CompileQueryCore<TResult>(_database, query, async: true));
//
//             // Execute synchronously, respecting the interface contract
//             return compiledQuery(queryContext);
//         }
//         /// <summary>
//         /// Creates a compiled query delegate for the given query expression.
//         /// </summary>
//         /// <typeparam name="TResult">The type of the result.</typeparam>
//         /// <param name="query">The query expression to compile.</param>
//         /// <returns>A compiled query delegate.</returns>
//         public Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
//         {
//             return CompileQueryCore<TResult>(_database, query, false);
//         }
//
//         /// <summary>
//         /// Creates an asynchronous compiled query delegate for the given query expression.
//         /// </summary>
//         /// <typeparam name="TResult">The type of the result.</typeparam>
//         /// <param name="query">The query expression to compile.</param>
//         /// <returns>An asynchronous compiled query delegate.</returns>
//         public Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query)
//         {
//             return CompileQueryCore<TResult>(_database, query, true);
//         }
//
//         /// <summary>
//         /// Compiles the query expression into a function for execution.
//         /// </summary>
//         /// <typeparam name="TResult">The type of the result.</typeparam>
//         /// <param name="database">The database to execute queries against.</param>
//         /// <param name="query">The query expression to compile.</param>
//         /// <param name="async">Indicates whether the query is asynchronous.</param>
//         /// <returns>A function that compiles the query.</returns>
//         private Func<QueryContext, TResult> CompileQueryCore<TResult>(DatabaseProvider<LibSqlOptionsExtension> database, Expression query, bool async)
//         {
//             return database.CompileQuery<TResult>(query, async);
//         }
//
//         /// <summary>
//         /// Extracts parameters from the query expression for LibSQL's execution.
//         /// </summary>
//         /// <param name="query">The query expression.</param>
//         /// <param name="queryContext">The query context.</param>
//         /// <returns>The modified query expression with parameters extracted.</returns>
//         private Expression ExtractParameters(Expression query, QueryContext queryContext)
//         {
//             // Add custom parameter extraction logic here, if needed.
//             return query;
//         }
//     }
// }
