using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication.Types;

/// <summary>
/// Public client: the code exchange is proved by PKCE alone and sends no client secret.
/// No properties.
/// </summary>
[TypeOption(typeof(OidcClientAuthenticationTypes), "None")]
public sealed class NoneConfiguration : OidcClientAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NoneConfiguration"/> class.</summary>
    public NoneConfiguration()
        : base("None",
               "Public client",
               "No client secret; the code exchange is proved by PKCE only",
               [], [], [], requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>A public client presents no identity of its own and none of the host's.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => false;
}
