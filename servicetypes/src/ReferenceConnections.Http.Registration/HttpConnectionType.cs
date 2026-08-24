using System;
using Fdw.Services.Results;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Logging;

using Fdw.Services.Connections.Http.Commands;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;

using Fdw.Services.Connections.Http.Security;

namespace ReferenceConnections.Http;

/// <summary>
/// Service type definition for generic HTTP connections.
/// Provides metadata, factory creation, and registration capabilities for HTTP connections.
/// </summary>
/// <remarks>
/// <para>
/// This connection type uses the <see cref="HttpConnectionConfiguration"/> class
/// which implements <c>IConnectionConfiguration</c>.
/// </para>
/// <para>
/// Connection configuration lives in ConfigurationDb and is read through the domain provider —
/// nothing is bound from appsettings. The protocol (REST, OData, GraphQL, SOAP) is selected by the
/// configuration row rather than by a separate connection type per protocol.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(ConnectionTypes), "Http")]
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class HttpConnectionType : ConnectionTypeBase<IGenericConnection, IHttpConnectionFactory, HttpConnectionConfiguration>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionType"/> class.
    /// Instance is created by source generator in ConnectionTypes collection.
    /// </summary>
    public HttpConnectionType() : base(
        name: "Http",
        sectionName: "Http",
        displayName: "HTTP Connection",
        description: "Generic HTTP connection for REST, OData, GraphQL, and other HTTP-based APIs",
        category: "HTTP",
        defaultContainerName: "HttpConnection")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var logger = (services.GetService<ILoggerFactory>()?.CreateLogger<HttpConnectionType>())
                ?? NullLogger<HttpConnectionType>.Instance;

            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. HttpConnectionConfigurationBase no longer inherits
            // ConnectionConfiguration — it implements IConnectionConfiguration directly.
            var headerProvider = services.GetRequiredService<ConnectionConfigurationProvider>();
            var configProvider = services.GetRequiredService<HttpConnectionConfigurationProvider>();
            // Why: RegisterTypedProvider requires a concrete type that implements IConnectionConfiguration.
            // HttpConnectionConfigurationBase is abstract; HttpConnectionConfiguration is the concrete type
            // that HttpConnectionConfigurationProvider is typed for. The abstract base is not directly usable here.
            headerProvider.Register(Name, configProvider);

    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Append and not Registration: Registration REPLACES the phase body, and
        // ConnectionTypeBase's constructor has already prepended this option's factory registration
        // onto it. Replacing therefore silently discards the base's contribution — which is exactly
        // how every connection kind stopped being creatable while each option's own wiring kept
        // working and logging success. Appending composes onto what the base put there.
        Registration((builder, loggerFactory) =>
        {

            // HTTP client factory is typically already registered, but ensure it's available
            builder.Services.AddHttpClient();

            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS — the HTTP client factory
            // above, and the secret-manager provider for the security configurations (WS-Security
            // certificate, Basic/UsernameToken password, API key) that carry a secret reference.
            builder.Services.AddSingleton<IHttpConnectionFactory>(sp => new HttpConnectionFactory(
                sp.GetRequiredService<ILogger<HttpConnectionFactory>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<ISecretManagerProvider>()));
            // Why: Factory lambda captures DataStore so cfg queries hit the correct DataStore.
            builder.Services.TryAddSingleton<HttpConnectionConfigurationProvider>(sp =>
                new HttpConnectionConfigurationProvider(
                    sp.GetService<ILogger<HttpConnectionConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<Fdw.Services.Abstractions.IServiceConfigurationProvider<HttpConnectionConfiguration>>(
                sp => sp.GetRequiredService<HttpConnectionConfigurationProvider>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

    /// <summary>
    /// HTTP responses are JSON by default; a container may override via its own <c>Format</c>.
    /// </summary>
    // Why: declares the transport's default response format so containers that omit Format
    // resolve to Json (not a silent Tabular). See ConnectionTypeBase.DefaultResponseFormat.
    public override IFormatType DefaultResponseFormat => FormatTypes.Json;



    /// <summary>
    /// HTTP connections support the HttpRequest and HttpRecordWriter capabilities.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    // HttpRecordWriter is declared in Http.Abstractions and is registered by its TypeOption module
    // initializer; no explicit opt-in is needed here.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("HttpRequest"),
        CommandCapabilityTypes.ByName("HttpRecordWriter"),
    ];

}
