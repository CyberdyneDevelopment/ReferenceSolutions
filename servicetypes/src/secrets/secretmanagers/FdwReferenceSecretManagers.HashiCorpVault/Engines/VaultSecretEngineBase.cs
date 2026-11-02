using Fdw.Collections;

namespace FdwReferenceSecretManagers.HashiCorpVault.Engines;

/// <summary>
/// Base class for Vault secret engines. Each option knows how to build its own read path and how to
/// interpret the response, so adding an engine is a new TypeOption rather than a branch in the client.
/// </summary>
public abstract class VaultSecretEngineBase : TypeOptionBase<VaultSecretEngineBase>, IVaultSecretEngine
{
    /// <summary>Initializes a new instance of the <see cref="VaultSecretEngineBase"/> class.</summary>
    /// <param name="name">The name of this engine.</param>
    /// <param name="issuesCredential">Whether a read issues a new credential rather than returning a stored one.</param>
    protected VaultSecretEngineBase(string name, bool issuesCredential) : base(name)
    {
        IssuesCredential = issuesCredential;
    }

    /// <inheritdoc/>
    public bool IssuesCredential { get; }

    /// <summary>
    /// Builds the Vault API path a read of <paramref name="secretKey"/> goes to.
    /// </summary>
    /// <param name="mount">The engine mount path.</param>
    /// <param name="secretKey">The secret path (KV) or role name (database).</param>
    /// <returns>The path below <c>/v1/</c>.</returns>
    public abstract string BuildReadPath(string mount, string secretKey);

    /// <summary>Gets the name of the JSON field inside <c>data</c> carrying the value this engine returns.</summary>
    public abstract string ValueField { get; }
}
