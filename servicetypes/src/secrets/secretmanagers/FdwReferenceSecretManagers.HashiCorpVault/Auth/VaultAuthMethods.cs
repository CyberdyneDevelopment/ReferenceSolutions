using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Auth;

/// <summary>
/// The ways this process can authenticate to Vault.
/// </summary>
/// <remarks>
/// Extensible by design: a deployment using an auth method FDW does not ship adds a
/// <c>[TypeOption]</c> against this collection in its own assembly. Nothing here enumerates the
/// known methods, so nothing has to change when one is added.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(VaultAuthMethodBase), typeof(IVaultAuthMethod), typeof(VaultAuthMethods))]
public abstract partial class VaultAuthMethods : TypeCollectionBase<VaultAuthMethodBase, IVaultAuthMethod>
{
}
