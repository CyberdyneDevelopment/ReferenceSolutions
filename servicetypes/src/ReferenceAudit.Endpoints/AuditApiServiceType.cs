using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceAudit.Endpoints.AuditEndpointOptions;

namespace ReferenceAudit.Endpoints;

/// <summary>The audit domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Audit")]
public class AuditApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AuditApiServiceType"/> class.</summary>
    public AuditApiServiceType()
        : base("Audit", "Audit", "Audit API", "HTTP endpoints for the audit domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new AuditEndpoints() };
}
