using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Handlers;

/// <summary>
/// TypeCollection of Environment Variable secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ISecretManagerCommandHandler), typeof(ISecretManagerCommandHandler), typeof(EnvironmentVariableCommandHandlers))]
public abstract partial class EnvironmentVariableCommandHandlers : TypeCollectionBase<ISecretManagerCommandHandler>
{
}
