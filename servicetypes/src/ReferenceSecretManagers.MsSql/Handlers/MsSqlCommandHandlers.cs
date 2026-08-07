using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// TypeCollection of MsSql secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ISecretManagerCommandHandler), typeof(ISecretManagerCommandHandler), typeof(MsSqlCommandHandlers))]
public abstract partial class MsSqlCommandHandlers : TypeCollectionBase<ISecretManagerCommandHandler>
{
}
