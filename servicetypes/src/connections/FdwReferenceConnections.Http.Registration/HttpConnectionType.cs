using Fdw.Services.Connections.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceConnections.Http;

/// <summary>
/// Service type definition for generic HTTP connections.
/// Provides metadata, factory creation, and registration capabilities for HTTP connections.
/// </summary>
/// <remarks>
/// <para>
/// This connection type uses the <see cref="HttpConnectionConfiguration"/> class
/// which implements <c>IConnectionImplementationConfiguration</c>.
/// </para>
/// <para>
/// Connection configuration lives in PlatformConfiguration and is read through the domain provider —
/// nothing is bound from appsettings. The protocol (REST, OData, GraphQL, SOAP) is selected by the
/// configuration row rather than by a separate connection type per protocol.
/// </para>
/// </remarks>
[Implementation(typeof(ConnectionTypes), "Http")]
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class HttpConnectionType : ConnectionTypeBase<IGenericConnection, IHttpConnectionFactory, HttpConnectionConfiguration>,
    ISupportsProtocols,
    ISupportsAuthenticationMethods
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
        defaultContainerName: "HttpConnectionConfiguration")
    {
        // Endpoint behavior: the flat request's Authentication is this type's AdditionalProperties. The method's
        // credential references are shown masked and the secret manager's name and its settings as they are
        // (FDW-1336); headers are shown masked (the default for a dictionary: the flat detail never showed them at
        // all). The configuration's own Validate() decides what is acceptable.
        EndpointAlias("authentication", nameof(HttpConnectionConfiguration.AdditionalProperties));
        EndpointMask(nameof(HttpConnectionConfiguration.AdditionalProperties), (configuration, entries) =>
            HttpAuthenticationTypes.ByName(configuration.AuthenticationType).MaskSecrets(entries, ConnectionSecretMask.Value));
        EndpointValidation(CheckConfiguration);

        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var logger = (services.GetService<ILoggerFactory>()?.CreateLogger<HttpConnectionType>())
                ?? NullLogger<HttpConnectionType>.Instance;

            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. HttpConnectionConfigurationBase no longer inherits
            // IConnectionImplementationConfiguration — it implements IConnectionImplementationConfiguration directly.
            // Why: RegisterTypedProvider requires a concrete type that implements IConnectionImplementationConfiguration.
            // HttpConnectionConfigurationBase is abstract; HttpConnectionConfiguration is the concrete type
            // that HttpConnectionConfigurationProvider is typed for. The abstract base is not directly usable here.
            services.GetRequiredService<IConnectionConfigurationProvider>().Register(Name, services.GetRequiredService<IHttpConnectionConfigurationProvider>());

            // The configuration provider above tells the domain how to READ this option's rows; this
            // hands the runtime connection provider what it BUILDS a connection with. Without it
            // IConnectionProvider's registry stays empty and every connection of this kind fails with
            // "no factory for service option" -- which is how the credential vault lost AuthDb.
            var implementationRegistered = services.GetRequiredService<IConnectionProvider>()
                .Register(Name, () => services.GetRequiredService<IHttpConnectionProvider>());
            if (!implementationRegistered.IsSuccess) return implementationRegistered.ToNewResult<IHost>();

    
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
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHttpConnectionFactory),
                sp => new HttpConnectionFactory(
                sp.GetRequiredService<ILogger<HttpConnectionFactory>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<ISecretManagerProvider>()),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHttpConnectionProvider),
                sp => new HttpConnectionProvider(
                    sp.GetRequiredService<IHttpConnectionFactory>(),
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    sp.GetService<ILogger<HttpConnectionProvider>>()),
                ServiceLifetime.Singleton));
            // Why: Factory lambda captures DataStore so cfg queries hit the correct DataStore.
            builder.Services.AddSingleton<IHttpConnectionConfigurationProvider, HttpConnectionConfigurationProvider>(sp => new HttpConnectionConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<HttpConnectionConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConnectionTypes.ConfigurationConnection, PathName, Container));
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

    // ── ISupportsAuthenticationMethods ─────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>Read from the HttpAuthenticationTypes options' own declarations each time, never cached at construction.</remarks>
    public IReadOnlyList<ConnectionAuthenticationMethod> AuthenticationMethods
        => HttpAuthenticationTypes.All().Select(method => ConnectionAuthenticationMethod.Of(method)).ToList();

    /// <summary>
    /// The wire protocols this connection type can be configured with, resolved from the
    /// registered <see cref="HttpProtocols"/> TypeCollection rather than a hardcoded list —
    /// a protocol added to that collection is offered here without a change to this file.
    /// </summary>
    public IReadOnlyList<string> SupportedProtocols =>
        HttpProtocols.All().Select(p => p.Name).ToList();

    // The configuration's own rules (a base URL, a protocol, a positive timeout), plus the rules the flat
    // request validator enforced: the base URL is an absolute http or https address, and the timeout is at
    // most 300 seconds.
    private static IGenericResult CheckConfiguration(HttpConnectionConfiguration configuration, ILogger logger)
    {
        var problems = new List<string>();
        var validated = configuration.Validate();
        if (validated.IsFailure)
            return validated;

        problems.AddRange(validated.Value!.Errors.Select(error => error.ErrorMessage));
        if (!string.IsNullOrWhiteSpace(configuration.BaseUrl)
            && !(Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out var address)
                 && (string.Equals(address.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))))
        {
            problems.Add("baseUrl must be an absolute http or https address");
        }

        if (configuration.TimeoutSeconds > 300)
            problems.Add("timeoutSeconds cannot exceed 300");

        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(ConnectionEndpointBehaviorLog.ConfigurationRejected(logger, "Http", string.Join("; ", problems)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names, which states its own answer
    /// (<c>ConnectionAuthenticationTypeBase.UsesHostIdentity</c>). A method this kind does not know cannot be
    /// answered for, so it is refused rather than read as "no".
    /// </remarks>
    protected override IGenericResult<bool> DecideHostIdentity(HttpConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = HttpAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a Http authentication method"));
        }

        return GenericResult<bool>.Success(authentication.UsesHostIdentity(
            new System.Collections.Generic.Dictionary<string, string?>(configuration.AdditionalProperties, System.StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names: its credential fields, read against the connection's
    /// authentication values. Names only; a credential's value is never read here (FDW-1086).
    /// </remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(HttpConnectionConfiguration configuration)
    {
        var credentials = CredentialsOf(configuration);
        return credentials.IsFailure
            ? credentials.ToNewResult<ConnectionCredentialSlots>()
            : GenericResult<ConnectionCredentialSlots>.Success(
                ConnectionCredentialSlots.From(credentials.Value.Method, credentials.Value.Credentials, configuration.AdditionalProperties));
    }

    /// <inheritdoc/>
    /// <remarks>Refuses a field the connection's authentication method does not use rather than storing a reference nothing reads.</remarks>
    protected override IGenericResult BindCredentialReferences(
        HttpConnectionConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
    {
        var credentials = CredentialsOf(configuration);
        if (credentials.IsFailure)
            return credentials;

        var unused = secretKeyByField.Keys
            .Where(field => !credentials.Value.Credentials.Any(credential => string.Equals(credential.Field, field, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (unused.Count > 0)
        {
            return GenericResult.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' uses no credential named {string.Join(", ", unused)}"));
        }

        ConnectionCredentialSlots.Bind(configuration.AdditionalProperties, secretManagerName, secretKeyByField);
        return GenericResult.Success();
    }

    // The authentication method the connection names and its credentials; a connection that names none, or
    // one this kind does not know, cannot say which credentials it uses. The method travels with the credentials so a
    // caller never reads the configuration's AuthenticationType again after this has checked it.
    private static IGenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)> CredentialsOf(HttpConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = HttpAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a Http authentication method"));
        }

        return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Success((configuration.AuthenticationType, authentication.Credentials));
    }
}
