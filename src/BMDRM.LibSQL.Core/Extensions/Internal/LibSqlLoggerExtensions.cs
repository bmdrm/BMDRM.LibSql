// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.LibSql.Diagnostics;
using Microsoft.EntityFrameworkCore.LibSql.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.LibSql.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class LibSqlLoggerExtensions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void SchemaConfiguredWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics,
        IEntityType entityType,
        string schema)
    {
        var definition = LibSqlResources.LogSchemaConfigured(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, entityType.DisplayName(), schema);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new EntityTypeSchemaEventData(
                definition,
                SchemaConfiguredWarning,
                entityType,
                schema);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string SchemaConfiguredWarning(EventDefinitionBase definition, EventData payload)
    {
        var d = (EventDefinition<string, string>)definition;
        var p = (EntityTypeSchemaEventData)payload;
        return d.GenerateMessage(
            p.EntityType.DisplayName(),
            p.Schema);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void SequenceConfiguredWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics,
        IReadOnlySequence sequence)
    {
        var definition = LibSqlResources.LogSequenceConfigured(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, sequence.Name);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new SequenceEventData(
                definition,
                SequenceConfiguredWarning,
                sequence);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string SequenceConfiguredWarning(EventDefinitionBase definition, EventData payload)
    {
        var d = (EventDefinition<string>)definition;
        var p = (SequenceEventData)payload;
        return d.GenerateMessage(p.Sequence.Name);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void ColumnFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? tableName,
        string? columnName,
        string? dataTypeName,
        bool notNull,
        string? defaultValue)
    {
        var definition = LibSqlResources.LogFoundColumn(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, columnName, dataTypeName, notNull, defaultValue);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void SchemasNotSupportedWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics)
    {
        var definition = LibSqlResources.LogUsingSchemaSelectionsWarning(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void ForeignKeyReferencesMissingTableWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? id,
        string? tableName,
        string? principalTableName)
    {
        var definition = LibSqlResources.LogForeignKeyScaffoldErrorPrincipalTableNotFound(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, id, tableName, principalTableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void TableFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? tableName)
    {
        var definition = LibSqlResources.LogFoundTable(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void MissingTableWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? tableName)
    {
        var definition = LibSqlResources.LogMissingTable(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void ForeignKeyPrincipalColumnMissingWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? foreignKeyName,
        string? tableName,
        string? principalColumnName,
        string? principalTableName)
    {
        var definition = LibSqlResources.LogPrincipalColumnNotFound(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, foreignKeyName, tableName, principalColumnName, principalTableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void IndexFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? indexName,
        string? tableName,
        bool? unique)
    {
        var definition = LibSqlResources.LogFoundIndex(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, indexName, tableName, unique);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void ForeignKeyFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? tableName,
        long id,
        string? principalTableName,
        string? deleteAction)
    {
        var definition = LibSqlResources.LogFoundForeignKey(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName, id, principalTableName, deleteAction);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void PrimaryKeyFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? primaryKeyName,
        string? tableName)
    {
        var definition = LibSqlResources.LogFoundPrimaryKey(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, primaryKeyName, tableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void UniqueConstraintFound(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? uniqueConstraintName,
        string? tableName)
    {
        var definition = LibSqlResources.LogFoundUniqueConstraint(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, uniqueConstraintName, tableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void UnexpectedConnectionTypeWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics,
        Type connectionType)
    {
        var definition = LibSqlResources.LogUnexpectedConnectionType(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, connectionType.ShortDisplayName());
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new UnexpectedConnectionTypeEventData(
                definition,
                UnexpectedConnectionTypeWarning,
                connectionType);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string UnexpectedConnectionTypeWarning(EventDefinitionBase definition, EventData payload)
    {
        var d = (EventDefinition<string>)definition;
        var p = (UnexpectedConnectionTypeEventData)payload;

        return d.GenerateMessage(p.ConnectionType.ShortDisplayName());
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void TableRebuildPendingWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics,
        Type operationType,
        string tableName)
    {
        var definition = LibSqlResources.LogTableRebuildPendingWarning(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, operationType.ShortDisplayName(), tableName);
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new TableRebuildEventData(
                definition,
                TableRebuildPendingWarning,
                operationType,
                tableName);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string TableRebuildPendingWarning(EventDefinitionBase definition, EventData payload)
    {
        var d = (EventDefinition<string, string?>)definition;
        var p = (TableRebuildEventData)payload;
        return d.GenerateMessage(p.OperationType.ShortDisplayName(), p.TableName);
    }

    /// <summary>
    ///     Logs the <see cref="LibSqlEventId.CompositeKeyWithValueGeneration" /> event.
    /// </summary>
    /// <param name="diagnostics">The diagnostics logger to use.</param>
    /// <param name="key">The key.</param>
    public static void CompositeKeyWithValueGeneration(
        this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics,
        IKey key)
    {
        var definition = LibSqlResources.LogCompositeKeyWithValueGeneration(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(
                diagnostics,
                key.DeclaringEntityType.DisplayName(),
                key.Properties.Format());
        }

        if (diagnostics.NeedsEventData(definition, out var diagnosticSourceEnabled, out var simpleLogEnabled))
        {
            var eventData = new KeyEventData(
                definition,
                CompositeKeyWithValueGeneration,
                key);

            diagnostics.DispatchEventData(definition, eventData, diagnosticSourceEnabled, simpleLogEnabled);
        }
    }

    private static string CompositeKeyWithValueGeneration(EventDefinitionBase definition, EventData payload)
    {
        var d = (EventDefinition<string?, string?>)definition;
        var p = (KeyEventData)payload;
        return d.GenerateMessage(
            p.Key.DeclaringEntityType.DisplayName(),
            p.Key.Properties.Format());
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void InferringTypes(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? tableName)
    {
        var definition = LibSqlResources.LogInferringTypes(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, tableName);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void OutOfRangeWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? columnName,
        string? tableName,
        string? type)
    {
        var definition = LibSqlResources.LogOutOfRangeWarning(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, columnName, tableName, type);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static void FormatWarning(
        this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics,
        string? columnName,
        string? tableName,
        string? type)
    {
        var definition = LibSqlResources.LogFormatWarning(diagnostics);

        if (diagnostics.ShouldLog(definition))
        {
            definition.Log(diagnostics, columnName, tableName, type);
        }

        // No DiagnosticsSource events because these are purely design-time messages
    }
}
