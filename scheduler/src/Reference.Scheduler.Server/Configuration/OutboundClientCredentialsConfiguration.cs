using System;
using System.Diagnostics.CodeAnalysis;

namespace Reference.Scheduler.Server.Configuration;

/// <summary>
/// Configuration for the service-to-service (client-credentials) identity the scheduler uses to
/// authenticate its outbound pipeline-dispatch calls to the ETL server. The OpenIddict
/// client-credentials grant runs against the API's <c>/connect/token</c> endpoint (resolved from
/// the OpenIddict <c>Authority</c> shipped in <c>appsettings.json</c>).
/// </summary>
/// <remarks>
/// The client secret is NEVER carried in this object. Only the names needed to resolve it through a
/// secret manager are configured here (<see cref="SecretManagerName"/> + <see cref="SecretKeyName"/>);
/// the value is read at runtime through <c>ISecretManager</c>, mirroring how the OpenIddict signing
/// key is resolved.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class OutboundClientCredentialsConfiguration
{
    /// <summary>Configuration section name bound from appsettings.</summary>
    public const string SectionName = "OutboundClientCredentials";

    /// <summary>Gets or sets the OAuth 2.0 client identifier (e.g. <c>fdw.scheduler</c>).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret manager (e.g. <c>EnvSecrets</c>) that resolves the client
    /// secret. Resolved through the same <c>ISecretManager</c> path as the signing key.
    /// </summary>
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key the secret manager reads (e.g. <c>SCHEDULER_CLIENT_SECRET</c>,
    /// which the EnvironmentVariable secret manager maps to <c>FDW_SECRET_SCHEDULER_CLIENT_SECRET</c>).
    /// </summary>
    public string SecretKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scopes requested on the outbound token. Must be a registered OpenIddict
    /// scope the <see cref="ClientId"/> is permitted to request (e.g. <c>fdw.api</c> — <c>fdw.scheduler</c>
    /// is permitted <c>fdw.api</c>). <c>pipelines:execute</c> is a permission, not a scope; it rides
    /// in the token's <c>perm</c> claim baked from the client's role regardless of the scopes requested.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}
