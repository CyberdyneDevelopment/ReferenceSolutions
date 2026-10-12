using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Auth;

/// <summary>
/// Authenticates with Vault's AppRole method — a role id from configuration plus a secret id
/// resolved at login time — and exchanges them for a short-lived Vault token.
/// </summary>
/// <remarks>
/// The role id identifies the workload and is not sensitive; the secret id is, and is resolved
/// through another secret manager rather than stored on the configuration row. What Vault returns is
/// a lease-bound token, so the long-lived material never travels with each request.
/// </remarks>
[TypeOption(typeof(VaultAuthMethods), "AppRole")]
public sealed class AppRoleVaultAuthMethod : VaultAuthMethodBase
{
    /// <summary>Initializes a new instance of the <see cref="AppRoleVaultAuthMethod"/> class.</summary>
    public AppRoleVaultAuthMethod() : base("AppRole", defaultMount: "approle", requiresStoredSecret: true)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string> BuildLoginPayload(string? roleId, string secret)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["role_id"] = roleId ?? string.Empty,
            ["secret_id"] = secret,
        };
}
