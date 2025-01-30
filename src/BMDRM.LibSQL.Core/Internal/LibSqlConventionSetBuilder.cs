// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.LibSql.Internal
{
    /// <summary>
    ///     A builder for building conventions for LibSQL.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Scoped" /> and multiple registrations
    ///         are allowed. This means that each <see cref="DbContext" /> instance will use its own
    ///         set of instances of this service.
    ///         The implementations may depend on other services registered with any lifetime.
    ///         The implementations do not need to be thread-safe.
    ///     </para>
    ///     <para>
    ///         For more information and examples on conventions and LibSQL, refer to the appropriate documentation for your custom solution.
    ///     </para>
    /// </remarks>
    public class LibSqlConventionSetBuilder : RelationalConventionSetBuilder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LibSqlConventionSetBuilder> _logger;

        /// <summary>
        ///     Creates a new <see cref="LibSqlConventionSetBuilder" /> instance.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> used for HTTP-based SQL execution.</param>
        /// <param name="dependencies">The core dependencies for this service.</param>
        /// <param name="relationalDependencies">The relational dependencies for this service.</param>
        /// <param name="logger">The logger instance for logging.</param>
        public LibSqlConventionSetBuilder(
            IHttpClientFactory httpClientFactory,
            ProviderConventionSetBuilderDependencies dependencies,
            RelationalConventionSetBuilderDependencies relationalDependencies,
            ILogger<LibSqlConventionSetBuilder> logger)
            : base(dependencies, relationalDependencies)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Log constructor entry
            _logger.LogInformation("LibSqlConventionSetBuilder initialized with HTTP client factory and logger.");
        }

        /// <summary>
        ///     Builds and returns the convention set for the LibSQL database provider.
        /// </summary>
        /// <returns>The convention set for LibSQL.</returns>
        public override ConventionSet CreateConventionSet()
        {
            _logger.LogInformation("Building convention set for LibSQL.");

            var conventionSet = base.CreateConventionSet();

            // Log when the conventions are being replaced
            _logger.LogInformation("Replacing conventions with LibSQL-specific conventions.");

            // Replace conventions with LibSQL-specific ones
            conventionSet.Replace<SharedTableConvention>(new LibSqlSharedTableConvention(Dependencies, RelationalDependencies));
            conventionSet.Replace<RuntimeModelConvention>(new LibSqlRuntimeModelConvention(Dependencies, RelationalDependencies));

            // Additional custom conventions for LibSQL can be added here
            _logger.LogInformation("LibSQL-specific conventions have been applied.");

            return conventionSet;
        }

        /// <summary>
        ///     Builds a <see cref="ConventionSet" /> for LibSQL when using
        ///     the <see cref="ModelBuilder" /> outside of <see cref="DbContext.OnModelCreating" />.
        /// </summary>
        /// <remarks>
        ///     Note that it is unusual to use this method.
        ///     Consider using <see cref="DbContext" /> in the normal way instead.
        /// </remarks>
        /// <returns>The convention set for LibSQL.</returns>
        public static ConventionSet Build(string connectionString)
        {
            using var serviceScope = CreateServiceScope(connectionString);
            using var context = serviceScope.ServiceProvider.GetRequiredService<DbContext>();
            return ConventionSet.CreateConventionSet(context);
        }

        /// <summary>
        ///     Builds a <see cref="ModelBuilder" /> for LibSQL outside of <see cref="DbContext.OnModelCreating" />.
        /// </summary>
        /// <remarks>
        ///     Note that it is unusual to use this method.
        ///     Consider using <see cref="DbContext" /> in the normal way instead.
        /// </remarks>
        /// <returns>The model builder for LibSQL.</returns>
        public static ModelBuilder CreateModelBuilder(string connectionString)
        {
            using var serviceScope = CreateServiceScope(connectionString);
            using var context = serviceScope.ServiceProvider.GetRequiredService<DbContext>();
            return new ModelBuilder(ConventionSet.CreateConventionSet(context), context.GetService<ModelDependencies>());
        }

        private static IServiceScope CreateServiceScope(string connectionString = "")
        {
            Console.WriteLine("Connection String loaded from appsettings.json.");

            var serviceProvider = new ServiceCollection()
                .AddLibSql<DbContext>(connectionString)
                .AddDbContext<DbContext>(
                    (p, o) =>
                        o.UseLibSql(connectionString)
                            .UseInternalServiceProvider(p))
                .AddHttpClient()
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
