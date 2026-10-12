using System.Collections.Generic;
using Fdw.Collections;

namespace FdwReferenceSecretManagers.HashiCorpVault.Auth;

/// <summary>
/// Base class for Vault auth methods. Each option knows its own mount, whether it needs a stored
/// secret, and how to shape its login request body — so adding a method is a new TypeOption rather
/// than a branch in the client.
/// </summary>
public abstract class VaultAuthMethodBase : TypeOptionBase<VaultAuthMethodBase>, IVaultAuthMethod
{
    /// <summary>Initializes a new instance of the <see cref="VaultAuthMethodBase"/> class.</summary>
    /// <param name="name">The name of this auth method.</param>
    /// <param name="defaultMount">The default Vault auth mount path.</param>
    /// <param name="requiresStoredSecret">Whether logging in needs a secret held somewhere.</param>
    /// <param name="loginPath">The path segment appended to the auth mount to log in.</param>
    protected VaultAuthMethodBase(string name, string defaultMount, bool requiresStoredSecret, string loginPath = "login")
        : base(name)
    {
        DefaultMount = defaultMount;
        RequiresStoredSecret = requiresStoredSecret;
        LoginPath = loginPath;
    }

    /// <inheritdoc/>
    public string DefaultMount { get; }

    /// <inheritdoc/>
    public bool RequiresStoredSecret { get; }

    /// <inheritdoc/>
    public string LoginPath { get; }

    /// <summary>
    /// Builds the JSON login payload for this method.
    /// </summary>
    /// <param name="roleId">The role id / role name from configuration.</param>
    /// <param name="secret">The resolved secret or assertion; empty for methods that need none.</param>
    /// <returns>The field/value pairs Vault expects in the login body.</returns>
    public abstract IReadOnlyDictionary<string, string> BuildLoginPayload(string? roleId, string secret);
}
