using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// TypeCollection for Azure Key Vault credential types.
/// Supports: ManagedIdentity, ServicePrincipal, Certificate, DeviceCode.
/// </summary>
[TypeCollection(typeof(AzureCredentialTypeBase), typeof(IAzureCredentialType), typeof(AzureCredentialTypes))]
public abstract partial class AzureCredentialTypes : TypeCollectionBase<AzureCredentialTypeBase, IAzureCredentialType>
{
}
