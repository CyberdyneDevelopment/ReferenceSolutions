using Fdw.Collections;

namespace FdwReferenceSecretManagers.HashiCorpVault.Engines;

/// <summary>
/// Interface for the Vault secret engines this secret manager can read from.
/// </summary>
public interface IVaultSecretEngine : ITypeOption<int, VaultSecretEngineBase>
{
    /// <summary>
    /// Gets a value indicating whether reading from this engine ISSUES a new credential rather than
    /// returning one somebody stored.
    /// </summary>
    /// <remarks>
    /// This is the difference that matters operationally: an issued credential is leased, expires,
    /// and is different on every read, so it must never be cached as if it were a stored value.
    /// </remarks>
    bool IssuesCredential { get; }

    /// <summary>Gets the name of the JSON field inside <c>data</c> carrying the value this engine returns.</summary>
    string ValueField { get; }

    /// <summary>
    /// Builds the Vault API path a read of <paramref name="secretKey"/> goes to.
    /// </summary>
    /// <param name="mount">The engine mount path.</param>
    /// <param name="secretKey">The secret path (KV) or role name (database).</param>
    /// <returns>The path below <c>/v1/</c>.</returns>
    string BuildReadPath(string mount, string secretKey);
}
