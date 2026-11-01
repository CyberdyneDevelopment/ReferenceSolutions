using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication;

/// <summary>
/// Base class for the ways this platform authenticates itself at an OIDC provider's token endpoint.
/// Each concrete type (None, ClientSecret) is a singleton behavior carrier - no mutable state.
/// </summary>
/// <remarks>
/// Shaped like a connection's authentication type: the authority row names a type and carries a
/// key/value set, and the type says which keys it expects, which are required, which hold the name
/// of a secret, and whether the set is usable. The values arrive as an
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> loaded from the <c>auth.OidcAuthorityProperty</c>
/// child table. The values are names - a secret manager and a key within it - never secret values.
/// </remarks>
public abstract class OidcClientAuthenticationConfiguration
    : ConnectionAuthenticationTypeBase<OidcClientAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected OidcClientAuthenticationConfiguration()
        : base(string.Empty, string.Empty, string.Empty, string.Empty, null, requiresSecret: false)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected OidcClientAuthenticationConfiguration(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties,
        IReadOnlyList<string> secretPropertyNames,
        bool requiresSecret)
        : base(name, $"OidcClientAuthentication:{name}", displayName, description, "OidcClientAuthentication", requiresSecret)
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
        SecretPropertyNames = secretPropertyNames;
    }

    /// <summary>Gets every property key this type reads.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <summary>Gets the property keys that must carry a value.</summary>
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <summary>
    /// Gets the subset of <see cref="ExpectedProperties"/> whose values name a secret
    /// (e.g. <c>SecretKeyName</c>) and must be masked before they are surfaced in an API response.
    /// </summary>
    public IReadOnlyList<string> SecretPropertyNames { get; } = [];

    /// <summary>Gets whether this is the Empty/NotFound sentinel, returned for a name no option answers to.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>Checks that <paramref name="values"/> satisfy this type.</summary>
    /// <param name="values">The authority row's client authentication properties.</param>
    /// <returns>Success, or a failure naming what is missing.</returns>
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);

    /// <summary>Reports the secret this type needs read before the code exchange.</summary>
    /// <param name="values">The authority row's client authentication properties.</param>
    /// <returns>
    /// The manager and key to read, or <see cref="SecretRequirement.None"/> when this type needs no
    /// secret - which is the default.
    /// </returns>
    /// <remarks>
    /// Deciding which manager and key belong here because this type owns the property set they come
    /// from; reading the secret does not. The step does that, for each exchange, and keeps nothing.
    /// </remarks>
    public virtual IGenericResult<SecretRequirement> RequiredSecret(IReadOnlyDictionary<string, string?> values)
        => GenericResult<SecretRequirement>.Success(SecretRequirement.None);
}
