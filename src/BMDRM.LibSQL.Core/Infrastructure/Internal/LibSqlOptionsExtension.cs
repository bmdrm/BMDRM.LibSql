// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.EntityFrameworkCore.LibSql.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.LibSql.Infrastructure.Internal;

/// <summary>
/// Represents the options extension for LibSQL, which configures the database connection and services.
/// Inherits from <see cref="RelationalOptionsExtension"/> to configure LibSQL-specific services and settings.
/// </summary>
public class LibSqlOptionsExtension : RelationalOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    private bool _loadSpatialite = true;
    private readonly string _url = "";
    private readonly string _apiAuthentication= "";
    private int? _commandTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlOptionsExtension"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to the LibSQL database in the format https://url/v2/pipeline;token".</param>
    public LibSqlOptionsExtension(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            _url = string.Empty;
            _apiAuthentication = string.Empty;
            throw new InvalidOperationException($"Connection string '{connectionString}' is invalid. It should be in the form https://url/v2/pipeline;token");
        }

        var parts = connectionString.Split(';');

        if (parts.Length == 2)
        {
            _url = parts[0];
            _apiAuthentication = parts[1];
        }
        else
        {
            _url = string.Empty;
            _apiAuthentication = string.Empty;
            throw new InvalidOperationException($"Connection string '{connectionString}' is invalid. It should be in the form https://url/v2/pipeline;token");
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="LibSqlOptionsExtension"/> class.
    /// </summary>
    public LibSqlOptionsExtension()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionString}' is invalid. It should be in the form https://url/v2/pipeline;token");
        }
    }
    /// <summary>
    /// Gets the URL for the LibSQL database.
    /// </summary>
    public string Url => _url;

    /// <summary>
    /// Gets the API authentication part of the connection string.
    /// </summary>
    public string ApiAuthentication => _apiAuthentication;

    /// <summary>
    /// Gets information about this options extension.
    /// </summary>
    public override DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    /// <summary>
    /// Configures the services required for LibSQL support.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public override void ApplyServices(IServiceCollection services)
    {
        services.ConfigureLibSql(
            _url,
            _apiAuthentication);
        services.AddEntityFrameworkLibSql();
    }

    /// <summary>
    /// Creates a copy of the current <see cref="LibSqlOptionsExtension"/> instance.
    /// </summary>
    /// <returns>A new instance of <see cref="LibSqlOptionsExtension"/> with the same URL and API authentication.</returns>
    protected override RelationalOptionsExtension Clone()
        => new LibSqlOptionsExtension($"{_url};{_apiAuthentication}");

    /// <summary>
    /// Gets the value indicating whether to load Spatialite.
    /// </summary>
    public virtual bool LoadSpatialite => _loadSpatialite;

    /// <summary>
    /// Creates a copy of the extension with the specified spatialite loading option.
    /// </summary>
    /// <param name="loadSpatialite">The flag to indicate whether to load Spatialite.</param>
    /// <returns>A new instance of <see cref="LibSqlOptionsExtension"/> with the updated option.</returns>
    public virtual LibSqlOptionsExtension WithLoadSpatialite(bool loadSpatialite)
    {
        var clone = (LibSqlOptionsExtension)Clone();
        clone._loadSpatialite = loadSpatialite;
        return clone;
    }

    /// <summary>
    /// Gets the command timeout value in seconds for database operations.
    /// </summary>
    public override int? CommandTimeout => _commandTimeout;

    /// <summary>
    /// Creates a copy of the extension with the specified command timeout value.
    /// </summary>
    /// <param name="timeout">The command timeout value in seconds for database operations.</param>
    /// <returns>A new instance of <see cref="LibSqlOptionsExtension"/> with the updated timeout.</returns>
    public virtual LibSqlOptionsExtension WithTimeout(int timeout)
    {
        var clone = (LibSqlOptionsExtension)Clone();
        clone._commandTimeout = timeout;
        return clone;
    }

    private sealed class ExtensionInfo : RelationalExtensionInfo
    {
        private string? _logFragment;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionInfo"/> class.
        /// </summary>
        /// <param name="extension">The extension to create the info for.</param>
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        private new LibSqlOptionsExtension Extension
            => (LibSqlOptionsExtension)base.Extension;

        /// <summary>
        /// Gets a value indicating whether this extension is the database provider.
        /// </summary>
        public override bool IsDatabaseProvider => true;

        /// <summary>
        /// Determines whether the same service provider should be used for another extension.
        /// </summary>
        /// <param name="other">The other extension to compare.</param>
        /// <returns><see langword="true"/> if the same service provider should be used; otherwise, <see langword="false"/>.</returns>
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        /// <summary>
        /// Gets the log fragment for this extension, used for logging purposes.
        /// </summary>
        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();

                    builder.Append(base.LogFragment);

                    if (Extension._loadSpatialite)
                    {
                        builder.Append("LoadSpatialite ");
                    }

                    if (Extension._commandTimeout.HasValue)
                    {
                        builder.Append($"CommandTimeout={Extension._commandTimeout} ");
                    }

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }

        /// <summary>
        /// Populates the debug information for this extension.
        /// </summary>
        /// <param name="debugInfo">The dictionary to populate with debug info.</param>
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["LibSQL"] = "1";
    }
}
