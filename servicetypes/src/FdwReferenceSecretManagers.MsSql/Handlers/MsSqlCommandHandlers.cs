using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;

namespace FdwReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// TypeCollection of MsSql secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(MsSqlCommandHandlers))]
public abstract partial class MsSqlCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
