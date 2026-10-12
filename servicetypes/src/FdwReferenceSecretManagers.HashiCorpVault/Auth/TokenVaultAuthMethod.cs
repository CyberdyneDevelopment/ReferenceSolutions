using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Auth;

/// <summary>
/// Authenticates with a Vault token supplied directly.
/// </summary>
/// <remarks>
/// The token IS the credential, so there is no login round trip — the client presents it as
/// <c>X-Vault-Token</c>. Simple, and the least good option in production: a Vault token held
/// somewhere is exactly the long-lived shared secret the rest of this work exists to remove. Prefer
/// <see cref="AppRoleVaultAuthMethod"/> or <see cref="JwtVaultAuthMethod"/> where the deployment
/// allows it.
/// </remarks>
[TypeOption(typeof(VaultAuthMethods), "Token")]
public sealed class TokenVaultAuthMethod : VaultAuthMethodBase
{
    /// <summary>Initializes a new instance of the <see cref="TokenVaultAuthMethod"/> class.</summary>
    public TokenVaultAuthMethod() : base("Token", defaultMount: "token", requiresStoredSecret: true)
    {
    }

    /// <inheritdoc/>
    // Why empty: this method never posts a login — the caller sends the token as a header. Returning
    // an empty payload rather than throwing keeps the shape uniform for callers that build a payload
    // before checking whether a login is needed.
    public override IReadOnlyDictionary<string, string> BuildLoginPayload(string? roleId, string secret)
        => new Dictionary<string, string>(StringComparer.Ordinal);
}
