using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>
/// What reading an OIDC authority reports.
/// </summary>
/// <remarks>
/// EventIds here match <c>ForeignAuthorityLog</c>'s wherever the meaning is the same read, declared,
/// missing, ambiguous, or unusable-URL check on a row of the same shape. Only checks with no
/// ForeignAuthority analog (the redirect-specific endpoints, client id, scopes, subject claim) get a
/// number of their own.
/// </remarks>
public static partial class OidcAuthorityLog
{
    /// <summary>Logs a provider that was read and will be redirected to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="issuer">The issuer it names.</param>
    /// <param name="audiences">How many audiences it accepts.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62001, Level = LogLevel.Information,
        Message = "OIDC authority '{name}' read: callers may be sent to '{issuer}' for {audiences} audience(s)")]
    public static partial IGenericMessage Read(ILogger logger, string name, string issuer, int audiences);

    /// <summary>Logs that no provider is declared.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62002, Level = LogLevel.Error,
        Message = "A flow names an OidcRedirect step but no OIDC authority is declared, so there is no provider to send a caller to")]
    public static partial IGenericMessage NoneDeclared(ILogger logger);

    /// <summary>Logs more than one declared provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="names">The providers declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62003, Level = LogLevel.Error,
        Message = "More than one OIDC authority is declared ({names}) and an OidcRedirect step cannot choose between them. Which provider a caller is sent to is the decision this row records, so declare exactly one")]
    public static partial IGenericMessage Ambiguous(ILogger logger, string names);

    /// <summary>Logs a provider with no issuer.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62004, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no Issuer. It must be the value the provider puts in a token's iss claim, matched literally")]
    public static partial IGenericMessage IssuerMissing(ILogger logger, string name);

    /// <summary>Logs a JwksUri that is not an absolute URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="value">What was declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62005, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares JwksUri '{value}', which is not an absolute URL")]
    public static partial IGenericMessage JwksUriUnusable(ILogger logger, string name, string value);

    /// <summary>Logs a provider with no audiences.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62006, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no ValidAudiences. Without one, every token this provider mints for any client would be accepted")]
    public static partial IGenericMessage AudiencesMissing(ILogger logger, string name);

    /// <summary>Logs a provider with no algorithms.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62007, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no ValidAlgorithms. The accepted algorithms are the host's decision, not the token's - a token that chooses its own can choose one its key trivially satisfies")]
    public static partial IGenericMessage AlgorithmsMissing(ILogger logger, string name);

    /// <summary>Logs a provider with no assertable methods.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62008, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no AssertableMethods. These are the RFC 8176 methods this provider is trusted to have actually verified, so an empty list is a statement that it verified nothing")]
    public static partial IGenericMessage MethodsMissing(ILogger logger, string name);

    /// <summary>Logs an AuthorizationEndpoint that is not an absolute URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="value">What was declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62009, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares AuthorizationEndpoint '{value}', which is not an absolute URL")]
    public static partial IGenericMessage AuthorizationEndpointUnusable(ILogger logger, string name, string value);

    /// <summary>Logs a TokenEndpoint that is not an absolute URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="value">What was declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62010, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares TokenEndpoint '{value}', which is not an absolute URL")]
    public static partial IGenericMessage TokenEndpointUnusable(ILogger logger, string name, string value);

    /// <summary>Logs a provider with no client id.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62011, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no ClientId. This is the client identifier this platform registered with the provider")]
    public static partial IGenericMessage ClientIdMissing(ILogger logger, string name);

    /// <summary>Logs a provider whose client authentication type is blank or names no option.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="value">What was declared.</param>
    /// <param name="known">The names an authority may declare.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62015, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares ClientAuthenticationType '{value}', which is not one of: {known}. How this platform authenticates at the token endpoint is the row's decision, so no type is assumed")]
    public static partial IGenericMessage ClientAuthenticationTypeUnknown(ILogger logger, string name, string value, string known);

    /// <summary>Logs a provider whose client authentication properties do not satisfy its type.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="type">The client authentication type declared.</param>
    /// <param name="required">The property keys that type requires.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62016, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares ClientAuthenticationType '{type}' but its authentication properties are incomplete. That type requires a value for each of: {required}")]
    public static partial IGenericMessage ClientAuthenticationPropertiesInvalid(ILogger logger, string name, string type, string required);

    /// <summary>Logs a RedirectUri that is not an absolute URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <param name="value">What was declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62012, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares RedirectUri '{value}', which is not an absolute URL. RFC 9700 requires an exact registered match, so this cannot be inferred")]
    public static partial IGenericMessage RedirectUriUnusable(ILogger logger, string name, string value);

    /// <summary>Logs a provider with no scopes.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62013, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no Scopes. Without one, the authorization request would ask the provider for nothing")]
    public static partial IGenericMessage ScopesMissing(ILogger logger, string name);

    /// <summary>Logs a provider with no subject claim.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The provider's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62014, Level = LogLevel.Error,
        Message = "OIDC authority '{name}' declares no SubjectClaim. Most providers use 'sub', but Entra needs 'oid' - this is the host's decision, not a default to assume")]
    public static partial IGenericMessage SubjectClaimMissing(ILogger logger, string name);
}
