using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to search the data catalog (FDW-1681).
/// </summary>
/// <remarks>
/// The search itself lives in <see cref="SearchCatalogEndpointBase"/>; this host only supplies the providers it reads: data sets
/// and their fields, data stores with their paths, containers and fields, calculations, transformations, pipelines and glossary terms.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SearchCatalogEndpoint : SearchCatalogEndpointBase
{
    /// <inheritdoc />
    public SearchCatalogEndpoint(
        ILogger<SearchCatalogEndpoint> logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms)
        : base(logger, dataSets, dataStores, pipelines, calculations, transformations, terms)
    {
    }
}
