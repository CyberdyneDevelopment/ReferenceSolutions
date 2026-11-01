using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Handlers;

/// <summary>
/// TypeCollection of Environment Variable secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(EnvironmentVariableCommandHandlers))]
public abstract partial class EnvironmentVariableCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
