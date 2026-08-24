using Fdw.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.UI.Themes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferenceEndpoints;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The endpoints over the shared surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SharedEndpoints")]
[TypeCollection(typeof(SharedEndpointBase), typeof(IEndpointTypeOption), typeof(SharedEndpoints))]
public partial class SharedEndpoints : EndpointTypeCollectionBase<SharedEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

    /// <summary>Registers the providers the endpoints in this group are constructed with.</summary>
    /// <remarks>
    /// These belong to the group that serves them rather than to whichever host happens to mount it.
    /// A host registering them on the group's behalf has to know which providers each endpoint takes,
    /// and a host that forgets cannot activate the endpoint at all.
    /// </remarks>
    public SharedEndpoints()
        => Registration((builder, loggerFactory) =>
        {
            DataflowGraphConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Why written out rather than a RegisterDomainConfiguration call: this one has no such
            // method. Its gateway is Lazy because the provider is a singleton and the gateway is scoped.
            builder.Services.TryAddSingleton<ThemeConfigurationProvider>(sp =>
                new ThemeConfigurationProvider(
                    sp.GetService<ILogger<ThemeConfigurationProvider>>(),
                    new Lazy<IConfigurationGateway>(sp.GetRequiredService<IConfigurationGateway>)));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
}
