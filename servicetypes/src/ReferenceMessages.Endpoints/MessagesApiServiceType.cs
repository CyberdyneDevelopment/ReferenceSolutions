using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceMessages.Endpoints.AccessRequestEndpointOptions;
using ReferenceMessages.Endpoints.MessageEndpointOptions;

namespace ReferenceMessages.Endpoints;

/// <summary>The messages domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Messages")]
public class MessagesApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MessagesApiServiceType"/> class.</summary>
    public MessagesApiServiceType()
        : base("Messages", "Messages", "Messages API", "HTTP endpoints for the messages domain.")
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
            new MessageEndpoints(),
            new AccessRequestEndpoints(),
        };
}
