using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The DataLineageEndpoints endpoint group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataLineageEndpoints")]
[TypeCollection(typeof(DataLineageEndpointBase), typeof(IEndpointTypeOption), typeof(DataLineageEndpoints))]
public partial class DataLineageEndpoints : EndpointTypeCollectionBase<DataLineageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

    /// <summary>Initializes a new instance of the <see cref="DataLineageEndpoints"/> class.</summary>
    public DataLineageEndpoints()
        => Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<DataflowGraphConfigurationProvider>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
}
