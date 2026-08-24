using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging;

using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;



namespace ReferenceConnections.MsSql;

/// <summary>
/// The SQL Server per-transport <see cref="DataStoreBuilderBase"/>. Builds
/// <see cref="MsSqlTableContainer"/>/<see cref="MsSqlViewContainer"/> nodes with
/// <see cref="MsSqlDataField"/> children and a <see cref="DatabasePath"/> physical address.
/// </summary>
/// <remarks>
/// Why: each <c>DataStoreType</c> supplies its own builder; the MsSql option supplies this one,
/// replacing the never-called <c>DataStoreTypeBase.Build</c> and the hardcoded if-MsSql branch of
/// the three deleted tree builders. Container construction (table vs view) dispatches on the
/// container config's <c>TypeId</c> discriminator — the single permitted storage-type branch, here
/// at the transport boundary where typed variants are created.
/// </remarks>
public sealed class MsSqlDataStoreBuilder : DataStoreBuilderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataStoreBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for build diagnostics.</param>
    public MsSqlDataStoreBuilder(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override IDataField BuildField(DataContainerFieldConfiguration fieldCfg)
    {
        // Why: DataType carries the SQL native type name (e.g. "nvarchar", "int"). NotFound sentinel
        // for an unrecognised type — ExplicitType returns null, observable, never a guessed type.
        var nativeType = fieldCfg.DataType is null
            ? MsSqlNativeTypes.NotFound
            : MsSqlNativeTypes.ByName(fieldCfg.DataType);

        MsSqlDataStoreBuilderLog.FieldNativeTypeResolved(Logger, fieldCfg.Name, nativeType.Name);

        return new MsSqlDataField(
            name: fieldCfg.Name,
            description: fieldCfg.Description,
            ordinal: fieldCfg.Ordinal,
            isNullable: fieldCfg.IsNullable,
            nativeType: (DataTypeOptionBase)nativeType,
            precision: null,
            scale: null,
            maxLength: null,
            collation: null,
            isSystemProvided: fieldCfg.IsSystemProvided);
    }

    /// <inheritdoc />
    protected override IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataPath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
    {
        // Why: two-part [schema].[object] address — the connection already specifies the database.
        // parent.Name is the schema (the tree-nav path name).
        var dbPath = new DatabasePath(null, parent.Name, containerCfg.Name);

        // Why: MsSql containers expose typed fields; the schema projects MsSqlDataField children
        // (which implement IField). Format defaults Tabular for SQL — the SQL transport's response
        // shape. (FOR JSON/FOR XML output is separate translator feature work, out of scope here.)
        var msSqlFields = fields.OfType<IMsSqlDataField>().ToList();
        var metadata = new Dictionary<string, object>(System.StringComparer.Ordinal);

        MsSqlDataStoreBuilderLog.FieldsResolved(Logger, containerCfg.Name, msSqlFields.Count);

        // Why: TypeId discriminator drives table vs view at the transport boundary — the one
        // permitted storage-type branch. View is read-only; Table is full CRUD.
        if (string.Equals(containerCfg.TypeId, "View", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(containerCfg.TypeId, "MsSqlView", System.StringComparison.OrdinalIgnoreCase))
        {
            MsSqlDataStoreBuilderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, "MsSqlView");
            return new MsSqlViewContainer(
                containerCfg.Name, containerCfg.Description, parent,
                msSqlFields, keys, referencingKeys, dbPath, FormatTypes.Tabular, metadata, Logger);
        }

        MsSqlDataStoreBuilderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, "MsSqlTable");
        return new MsSqlTableContainer(
            containerCfg.Name, containerCfg.Description, parent,
            msSqlFields, keys, referencingKeys, dbPath, FormatTypes.Tabular, metadata, Logger);
    }
}
