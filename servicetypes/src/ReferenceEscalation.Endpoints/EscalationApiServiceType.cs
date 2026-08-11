using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEscalation.Endpoints.EscalationEndpointOptions;

namespace ReferenceEscalation.Endpoints;

/// <summary>The escalation domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Escalation")]
public class EscalationApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="EscalationApiServiceType"/> class.</summary>
    public EscalationApiServiceType()
        : base("Escalation", "Escalation", "Escalation API", "HTTP endpoints for the escalation domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new EscalationEndpoints() };
}
