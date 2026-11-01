using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;

namespace FdwReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// TypeCollection of Azure Key Vault command handlers.
/// Each handler processes a specific command type (GetSecret, SetSecret, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(AzureKeyVaultCommandHandlers))]
public abstract partial class AzureKeyVaultCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
