using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;

namespace FdwReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// TypeCollection of SQLite secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerCommandHandlerBase), typeof(ISecretManagerCommandHandler), typeof(SqliteCommandHandlers))]
public abstract partial class SqliteCommandHandlers : TypeCollectionBase<SecretManagerCommandHandlerBase, ISecretManagerCommandHandler>
{
}
