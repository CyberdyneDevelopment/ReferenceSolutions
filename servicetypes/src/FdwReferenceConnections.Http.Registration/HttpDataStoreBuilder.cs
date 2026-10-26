using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Http.Paths;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Http;

/// <summary>
/// The Http data store's builder. It builds the same containers as the generic builder, except that each container's address is an <see cref="HttpPath"/>, which a
/// run can replace: a source reads the declared container at another request path, query string included, so the symbols, sites and dates of a request come from
/// the run's parameters.
/// </summary>
public sealed class HttpDataStoreBuilder : DataStoreBuilderBase
{
    private readonly IFormatType _defaultResponseFormat;

    /// <summary>Initializes a new instance of the <see cref="HttpDataStoreBuilder"/> class.</summary>
    /// <param name="defaultResponseFormat">The format a container is read as when its own configuration declares none.</param>
    /// <param name="logger">Logger for build diagnostics.</param>
    public HttpDataStoreBuilder(IFormatType defaultResponseFormat, ILogger? logger = null)
        : base(logger)
    {
        _defaultResponseFormat = defaultResponseFormat;
    }

    /// <inheritdoc/>
    protected override IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
    {
        var containerType = string.IsNullOrEmpty(containerCfg.TypeId)
            ? ContainerTypes.ByName("Endpoint")
            : ContainerTypes.ByName(containerCfg.TypeId);

        DataStoreLoaderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, containerType.Name);
        DataStoreLoaderLog.ContainerFieldsBuilt(Logger, containerCfg.Name, fields.Count);

        var format = ContainerComposition.ResolveFormat(containerCfg, _defaultResponseFormat);

        return new DataContainer(
            containerCfg.Name,
            containerCfg.Description,
            parent,
            fields,
            keys,
            referencingKeys,
            containerType,
            format,
            new HttpPath(parent.Name),
            ["Query"],
            ContainerComposition.BuildMetadata(containerCfg, format),
            Logger);
    }
}
