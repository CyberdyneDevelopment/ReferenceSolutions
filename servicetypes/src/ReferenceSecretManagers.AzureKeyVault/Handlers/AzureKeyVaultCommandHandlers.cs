using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// TypeCollection of Azure Key Vault command handlers.
/// Each handler processes a specific command type (GetSecret, SetSecret, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ISecretManagerCommandHandler), typeof(ISecretManagerCommandHandler), typeof(AzureKeyVaultCommandHandlers))]
public abstract partial class AzureKeyVaultCommandHandlers : TypeCollectionBase<ISecretManagerCommandHandler>
{
}
