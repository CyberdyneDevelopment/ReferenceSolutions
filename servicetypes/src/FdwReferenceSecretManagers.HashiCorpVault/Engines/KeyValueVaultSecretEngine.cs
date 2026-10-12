using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Engines;

/// <summary>
/// Vault's KV version 2 engine — reads a secret somebody stored.
/// </summary>
/// <remarks>
/// KV v2 nests the payload under <c>data.data</c> and its own metadata under <c>data.metadata</c>,
/// which is why the read path carries the extra <c>data</c> segment. This is the general-purpose
/// engine: a database password kept in Vault rather than an environment variable lands here.
/// </remarks>
[TypeOption(typeof(VaultSecretEngines), "KeyValue")]
public sealed class KeyValueVaultSecretEngine : VaultSecretEngineBase
{
    /// <summary>Initializes a new instance of the <see cref="KeyValueVaultSecretEngine"/> class.</summary>
    public KeyValueVaultSecretEngine() : base("KeyValue", issuesCredential: false)
    {
    }

    /// <inheritdoc/>
    public override string BuildReadPath(string mount, string secretKey) => $"{mount}/data/{secretKey}";

    /// <inheritdoc/>
    public override string ValueField => "value";
}
