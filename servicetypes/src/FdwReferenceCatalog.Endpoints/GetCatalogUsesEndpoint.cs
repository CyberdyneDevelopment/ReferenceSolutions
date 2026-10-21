using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint that answers where a catalog thing is used (FDW-1683).
/// </summary>
/// <remarks>
/// The reading lives in <see cref="GetCatalogUsesEndpointBase"/>; this host only supplies the providers it reads: data sets, pipelines,
/// calculations, transformations and glossary terms.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetCatalogUsesEndpoint : GetCatalogUsesEndpointBase
{
    /// <inheritdoc />
    public GetCatalogUsesEndpoint(
        ILogger<GetCatalogUsesEndpoint> logger,
        IDataSetConfigurationProvider dataSets,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms)
        : base(logger, dataSets, pipelines, calculations, transformations, terms)
    {
    }
}
