using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Schema;
using Fdw.Services.Connections.MsSql.Discovery;

namespace FdwReferenceConnections.MsSql.Discovery;

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
        var nativeType = MsSqlNativeTypeResolver.Resolve(discovered.SqlType);

        if (discovered.IsNullable && clrType.IsValueType)
            clrType = typeof(Nullable<>).MakeGenericType(clrType);

        return new Field
        {
            Name = discovered.Name,
            // The column's type as SQL Server reports it, recorded as the container field's DataType, and the portable type its
            // native type normalizes to, recorded as its FrameworkDataType; a type FDW has no native type for stays unstated (FDW-1588).
            FieldType = new SimpleFieldType
            {
                TypeName = discovered.SqlType,
                ClrType = clrType
            },
            ExplicitType = nativeType == MsSqlNativeTypes.NotFound ? null : nativeType.AbstractType,
            // Why: IsPrimaryKey removed from Field — PK identity carried in KeyField tables. A primary key
            // is Identifier and never DbKey (FDW-988): the catalog cannot tell a meaningless row number from
            // a business id the database assigns (an OrderId IDENTITY), and DbKey withholds the column from
            // every default read. Hiding a key is a person's decision, not discovery's. Every other column
            // stays NotYetTyped: the catalog cannot tell a measure from an attribute.
            Role = discovered.IsPrimaryKey ? DatumTypes.Identifier : DatumTypes.NotYetTyped,
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
