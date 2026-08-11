using System;
using System.Collections.Generic;
using Fdw.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceAuth.Endpoints.AgentKeyEndpointOptions;
using ReferenceAuth.Endpoints.AuthEndpointOptions;
using ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

namespace ReferenceAuth.Endpoints;

/// <summary>The auth domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Auth")]
public class AuthApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AuthApiServiceType"/> class.</summary>
    public AuthApiServiceType()
        : base("Auth", "Auth", "Auth API", "HTTP endpoints for the auth domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            // Why here and not in the host: TokenSwitchEndpoint, in this package, resolves
            // CreateClient("TokenSwitch"). The client is this API calling itself — the base address
            // is the host's own listening URL — so it cannot be a normal ApiClientType pointed at a
            // configured endpoint, but it still belongs to the domain whose endpoint names it
            // rather than to a Program.cs that has no other reason to know the name exists.
            builder.Services.AddHttpClient("TokenSwitch", (sp, client) =>
            {
                // Two keys an operator wrote, most specific first - a declared-override
                // hierarchy, not a default. When a host declares neither there is no address to
                // invent: a guessed localhost would point token switching at the wrong host and
                // fail on the first call rather than at startup.
                var urls = builder.Configuration["ASPNETCORE_URLS"]
                    ?? builder.Configuration["Kestrel:Endpoints:Http:Url"]
                    ?? throw new InvalidOperationException(
                        "The TokenSwitch client calls this host's own API, so it needs the host's "
                        + "listening URL. Declare ASPNETCORE_URLS or Kestrel:Endpoints:Http:Url.");

                client.BaseAddress = new Uri(urls.Split(';')[0].TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return RegisterEndpoints(builder, loggerFactory);
        });

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            new AuthEndpoints(),
            new AgentKeyEndpoints(),
            new PersonalAccessTokenEndpoints(),
        };
}
