using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions.Discovery;
using Fdw.Services.Data.Results;

using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Discovery;
using Fdw.Services.Connections.MsSql.Logging;

using ReferenceConnections.MsSql;

using ReferenceConnections.MsSql.Discovery;

namespace ReferenceConnections.MsSql.Discovery;

/// <summary>
/// Adapts the typed <see cref="IMsSqlSchemaDiscoverer"/> to the
/// connection-type-agnostic <see cref="ISchemaDiscoverer"/> contract so the
/// CLI/web UI can drive discovery uniformly across connection types.
/// </summary>
// Why: The MsSql discoverer was written before ISchemaDiscoverer existed and uses
// MsSqlConnection-typed signatures. Wrapping it lets the cross-cutting factory
// dispatch without leaking MsSql types into Services.Data.
public sealed class MsSqlSchemaDiscovererAdapter : ISchemaDiscoverer
{
    private readonly IMsSqlSchemaDiscoverer _inner;

    /// <summary>Initializes a new instance wrapping the supplied typed discoverer.</summary>
    /// <param name="inner">The <see cref="IMsSqlSchemaDiscoverer"/> resolved from DI.</param>
    public MsSqlSchemaDiscovererAdapter(IMsSqlSchemaDiscoverer inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDiscoveredContainer>>> DiscoverContainers(
        IGenericConnection connection,
        CancellationToken cancellationToken = default)
    {
        if (connection is not MsSqlConnection msSql)
        {
            return GenericResult<IReadOnlyList<IDiscoveredContainer>>.Failure(
                DataServiceResultCodes.ByName("DiscovererNotFound"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlConnection))
                    .With("ActualType", connection?.GetType().Name ?? "(null)"));
        }

        var result = await _inner.DiscoverSchema(msSql, options: null, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
            return result.ToNewResult<IReadOnlyList<IDiscoveredContainer>>();

        // Flatten paths → containers and project into the abstraction shape.
        var flat = result.Value.Paths
            .SelectMany(path => path.Containers.Select(c => Project(path.SchemaName, c)))
            .ToList();

        return GenericResult<IReadOnlyList<IDiscoveredContainer>>.Success(flat);
    }

    private static DiscoveredContainerDto Project(string pathName, DiscoveredContainer source)
    {
        var fields = source.Fields
            .Select(f => (IDiscoveredField)new DiscoveredFieldDto(
                f.Name, f.SqlType, f.IsNullable, f.Ordinal,
                f.MaxLength, f.Precision, f.Scale))
            .ToList();

        return new DiscoveredContainerDto(pathName, source.Name, source.ContainerType, fields);
    }

    private sealed record DiscoveredContainerDto(
        string PathName,
        string Name,
        string ContainerType,
        IReadOnlyList<IDiscoveredField> Fields) : IDiscoveredContainer;

    private sealed record DiscoveredFieldDto(
        string Name,
        string DataType,
        bool IsNullable,
        int Ordinal,
        int? MaxLength,
        int? Precision,
        int? Scale) : IDiscoveredField;
}
