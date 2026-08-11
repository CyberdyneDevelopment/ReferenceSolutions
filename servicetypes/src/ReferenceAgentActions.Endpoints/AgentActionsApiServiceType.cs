using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

namespace ReferenceAgentActions.Endpoints;

/// <summary>The agentactions domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "AgentActions")]
public class AgentActionsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentActionsApiServiceType"/> class.</summary>
    public AgentActionsApiServiceType()
        : base("AgentActions", "AgentActions", "AgentActions API", "HTTP endpoints for the agentactions domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new AgentActionsEndpoints() };
}
