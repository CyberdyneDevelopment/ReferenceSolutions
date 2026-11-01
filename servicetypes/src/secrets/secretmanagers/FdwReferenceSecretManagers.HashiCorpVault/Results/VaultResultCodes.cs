using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// TypeCollection for HashiCorp Vault result codes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(VaultResultCodeBase), typeof(IResultCode), typeof(VaultResultCodes))]
public abstract partial class VaultResultCodes : TypeCollectionBase<VaultResultCodeBase, IResultCode>
{
}
