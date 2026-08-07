using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// TypeCollection of SQLite secret manager command handlers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ISecretManagerCommandHandler), typeof(ISecretManagerCommandHandler), typeof(SqliteCommandHandlers))]
public abstract partial class SqliteCommandHandlers : TypeCollectionBase<ISecretManagerCommandHandler>
{
}
