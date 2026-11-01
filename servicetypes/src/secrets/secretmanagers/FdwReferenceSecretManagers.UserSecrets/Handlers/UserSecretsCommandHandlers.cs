using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;

namespace FdwReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// TypeCollection of User Secrets command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(UserSecretsCommandHandlers))]
public abstract partial class UserSecretsCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
