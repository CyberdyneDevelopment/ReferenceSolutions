using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.Sqlite.Logging;
using ReferenceSecretManagers.Sqlite.Services;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// Handler for ListSecrets commands against a SQLite file.
/// </summary>
[TypeOption(typeof(SqliteCommandHandlers), "ListSecrets")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class SqliteListSecretsHandler
    : SecretManagerCommandHandlerBase<ListSecretsManagerCommand, IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteListSecretsHandler"/> class.
    /// </summary>
    public SqliteListSecretsHandler()
        : base(id: 4, name: "ListSecrets")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IReadOnlyList<ISecretMetadata>>> Execute(
        ListSecretsManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not SqliteExecutionContext sqliteContext)
        {
            return GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(SqliteExecutionContext)));
        }

        var config = sqliteContext.SqliteConfiguration;
        SqliteSecretManagerLogger.TraceQueryExecution(sqliteContext.Logger, "ListSecrets", config.TableName);

        var conn = sqliteContext.CreateConnection();
        await using (conn.ConfigureAwait(false))
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            var cmd = conn.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandTimeout = config.CommandTimeoutSeconds;
                cmd.CommandText =
                    $"SELECT \"SecretKey\", \"SecretValue\", \"Version\", \"SecretType\", \"Description\", " +
                    $"\"ExpiresAt\", \"CreateDate\", \"ModifyDate\", \"IsCurrent\", \"IsDeleted\" " +
                    $"FROM \"{config.TableName}\" " +
                    $"WHERE \"IsCurrent\" = 1 AND \"IsDeleted\" = 0";

                var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    var secrets = new List<ISecretMetadata>();
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var row = SqliteGetSecretHandler.MapRow(reader);
                        secrets.Add(new SqliteSecretMetadata(
                            key: row.SecretKey,
                            tableName: config.TableName,
                            version: row.Version,
                            secretType: row.SecretType,
                            description: row.Description,
                            createdAt: new DateTimeOffset(row.CreateDate, TimeSpan.Zero),
                            modifiedAt: new DateTimeOffset(row.ModifyDate, TimeSpan.Zero),
                            expiresAt: row.ExpiresAt.HasValue ? new DateTimeOffset(row.ExpiresAt.Value, TimeSpan.Zero) : null));
                    }

                    SqliteSecretManagerLogger.SecretsListed(sqliteContext.Logger, secrets.Count, config.TableName);
                    SqliteSecretManagerLogger.TraceHandlerSuccess(sqliteContext.Logger, "ListSecrets", secrets.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return GenericResult<IReadOnlyList<ISecretMetadata>>.Success(secrets);
                }
            }
        }
    }
}
