using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

namespace ReferenceSettings.Endpoints;

/// <summary>
/// The settings domain's API surface.
/// </summary>
/// <remarks>
/// Declared here rather than in the framework because the concrete endpoint types live here — the
/// framework ships the abstract endpoint bases and the mechanism, and the host closes both.
///
/// The attribute registers this domain into ApiServiceTypes, which is the collection the
/// PlatformServices sweep drives. Nothing else refers to this class, so without it the domain is
/// declared and never reached.
/// </remarks>
// Why the name is spelled out in full: see ApiHostServiceType — the simple name binds to this
// file's own namespace, ReferenceSettings.Endpoints.
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Settings")]
public class SettingsApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsApiServiceType"/> class.
    /// </summary>
    public SettingsApiServiceType()
        : base("Settings", "Settings", "Settings API", "HTTP endpoints over server, tenant and role settings.")
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
            // Why an instance rather than the static collection: the sweep drives collections
            // polymorphically, and Members bridges to the generated static All().
            new ServerSettingEndpoints(),
        };
}
