using System.Collections.Generic;
using Fdw.Collections;

namespace FdwReferenceSecretManagers.HashiCorpVault.Auth;

/// <summary>
/// Interface for the ways this process can authenticate to Vault.
/// </summary>
/// <remarks>
/// <see cref="DefaultMount"/> and <see cref="RequiresStoredSecret"/> are declared here because
/// <c>ByName</c> hands back this interface; behavior a caller cannot reach through the type the
/// lookup returns would force a downcast at every call site.
/// </remarks>
public interface IVaultAuthMethod : ITypeOption<int, VaultAuthMethodBase>
{
    /// <summary>Gets the auth mount path Vault uses for this method by default (e.g. <c>approle</c>).</summary>
    string DefaultMount { get; }

    /// <summary>
    /// Gets a value indicating whether this method logs in with a secret that must be stored
    /// somewhere, as opposed to a workload assertion minted on demand.
    /// </summary>
    bool RequiresStoredSecret { get; }

    /// <summary>Gets the Vault login path segment appended to the auth mount (e.g. <c>login</c>).</summary>
    string LoginPath { get; }

    /// <summary>
    /// Builds the JSON login payload for this method.
    /// </summary>
    /// <param name="roleId">The role id / role name from configuration.</param>
    /// <param name="secret">The resolved secret or assertion; empty for methods that need none.</param>
    /// <returns>The field/value pairs Vault expects in the login body.</returns>
    IReadOnlyDictionary<string, string> BuildLoginPayload(string? roleId, string secret);
}
