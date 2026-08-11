using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceExecution.Endpoints.ExecutionEndpointOptions;
using ReferenceExecution.Endpoints.WorkflowEndpointOptions;

namespace ReferenceExecution.Endpoints;

/// <summary>The execution domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Execution")]
public class ExecutionApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecutionApiServiceType"/> class.</summary>
    public ExecutionApiServiceType()
        : base("Execution", "Execution", "Execution API", "HTTP endpoints for the execution domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            new ExecutionEndpoints(),
            new WorkflowEndpoints(),
        };
}
