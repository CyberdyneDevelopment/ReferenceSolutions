using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

namespace ReferenceSessionState.Endpoints;

/// <summary>The session-state domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "SessionState")]
public class SessionStateApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SessionStateApiServiceType"/> class.</summary>
    public SessionStateApiServiceType()
        : base("SessionState", "SessionState", "SessionState API", "HTTP endpoints for the session-state domain.")
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
            new SessionStateEndpoints(),
        };
}
