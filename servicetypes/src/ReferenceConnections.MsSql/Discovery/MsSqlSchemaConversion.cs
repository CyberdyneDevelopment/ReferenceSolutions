using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Schema;

using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Discovery;

namespace ReferenceConnections.MsSql.Discovery;

/// <summary>
/// Shared static helpers for converting MsSql schema discovery results to FDW container/field types.
/// Used by <c>MsSqlConnectionType</c> for the direct-connection discovery path.
/// </summary>
[ExcludeFromCodeCoverage]
public static class MsSqlSchemaConversion
{
    /// <summary>Maps a <see cref="DiscoveredField"/> to an <see cref="IField"/>.</summary>
    public static IField MapToField(DiscoveredField discovered)
    {
        var converter = MsSqlConverters.BySourceType(discovered.SqlType.ToLowerInvariant());
        var clrType = converter.TargetClrType;

        if (discovered.IsNullable && clrType.IsValueType)
            clrType = typeof(Nullable<>).MakeGenericType(clrType);

        return new Field
        {
            Name = discovered.Name,
            FieldType = new SimpleFieldType
            {
                TypeName = clrType.Name,
                ClrType = clrType
            },
            // Why: IsPrimaryKey removed from Field — PK identity carried in KeyField tables.
            // Role = Surrogate signals this is the PK field for downstream consumers.
            Role = discovered.IsPrimaryKey
                ? PropertyRoles.ByName("Surrogate")
                : PropertyRoles.ByName("Attribute"),
            IsNullable = discovered.IsNullable,
            IsIdentity = discovered.IsIdentity,
            IsComputed = discovered.IsComputed,
            TypeSystemId = "MsSql",
            ConverterTypeId = converter.Id
        };
    }

    /// <summary>Resolves an <see cref="IContainerType"/> by name from <see cref="ContainerTypes"/>.</summary>
    public static IContainerType GetContainerType(string containerTypeName)
        => ContainerTypes.ByName(containerTypeName);
}
