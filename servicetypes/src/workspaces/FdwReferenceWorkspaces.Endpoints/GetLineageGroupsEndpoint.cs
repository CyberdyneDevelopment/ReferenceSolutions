using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Tags;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class GetLineageGroupsEndpoint : GetLineageGroupsEndpointBase
{
    public GetLineageGroupsEndpoint(
        ILogger<GetLineageGroupsEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization,
        ITagService tags)
        : base(logger, dataSets, dataStores, pipelines, variations, categories, workspaces, access, authorization, tags)
    {
    }
}
