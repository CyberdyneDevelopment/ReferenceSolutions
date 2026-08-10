using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Handlers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// TypeCollection of User Secrets command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(UserSecretsCommandHandlers))]
public abstract partial class UserSecretsCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
