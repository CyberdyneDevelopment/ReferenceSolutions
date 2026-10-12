using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Engines;

/// <summary>
/// The Vault secret engines this secret manager can read from.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(VaultSecretEngineBase), typeof(IVaultSecretEngine), typeof(VaultSecretEngines))]
public abstract partial class VaultSecretEngines : TypeCollectionBase<VaultSecretEngineBase, IVaultSecretEngine>
{
}
