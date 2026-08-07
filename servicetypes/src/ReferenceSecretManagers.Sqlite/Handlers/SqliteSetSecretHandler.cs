using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
using Microsoft.Data.Sqlite;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// Handler for SetSecret commands against a SQLite file.
/// Uses version-on-write: deactivates the current row, then inserts a new one.
/// </summary>
[TypeOption(typeof(SqliteCommandHandlers), "SetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class SqliteSetSecretHandler
    : SecretManagerCommandHandlerBase<SetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSetSecretHandler"/> class.
    /// </summary>
    public SqliteSetSecretHandler()
        : base(id: 2, name: "SetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        SetSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not SqliteExecutionContext sqliteContext)
        {
            return GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(SqliteExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<SecretValue>.Failure(
                SqliteSecretManagerLogger.SecretKeyRequired(sqliteContext.Logger, "SetSecret"));
        }

        // Why: SetSecretManagerCommand.CreateParametersWithValue stores under nameof(SecretValue) = "SecretValue".
        if (!command.Parameters.TryGetValue("SecretValue", out var valueObj) || valueObj is not string secretValue || string.IsNullOrEmpty(secretValue))
        {
            return GenericResult<SecretValue>.Failure(
                SqliteSecretManagerLogger.SecretValueRequired(sqliteContext.Logger));
        }

        // Why: SecretType is a required input — a fabricated "Password" default is a silent fallback
        // (NO-FALLBACKS rule). Fail loud, mirroring the SecretKey/SecretValue requirement above.
        if (!command.Parameters.TryGetValue("SecretType", out var secretTypeObj)
            || secretTypeObj is not string secretType
            || string.IsNullOrWhiteSpace(secretType))
        {
            return GenericResult<SecretValue>.Failure(
                SqliteSecretManagerLogger.SecretTypeRequired(sqliteContext.Logger));
        }

        command.Parameters.TryGetValue("Description", out var descriptionObj);
        var description = descriptionObj as string;

        command.Parameters.TryGetValue("ExpiresAt", out var expiresAtObj);
        DateTime? expiresAt = expiresAtObj is DateTimeOffset dto ? dto.UtcDateTime : null;

        var config = sqliteContext.SqliteConfiguration;
        SqliteSecretManagerLogger.TraceHandlerEntry(sqliteContext.Logger, "SetSecret", command.SecretKey, config.TableName);
        SqliteSecretManagerLogger.TraceQueryExecution(sqliteContext.Logger, "SetSecret", config.TableName);

        var writeResult = await VersionOnWrite(
            sqliteContext, command.SecretKey, secretValue, secretType, description, expiresAt, cancellationToken)
            .ConfigureAwait(false);

        if (writeResult.IsFailure)
        {
            return writeResult.ToNewResult<SecretValue>();
        }

        SqliteSecretManagerLogger.SecretSet(sqliteContext.Logger, command.SecretKey, config.TableName);
        SqliteSecretManagerLogger.TraceHandlerSuccess(sqliteContext.Logger, "SetSecret", command.SecretKey);

        return GenericResult<SecretValue>.Success(new SecretValue(
            key: command.SecretKey,
            value: string.Empty,
            version: null,
            createdAt: null,
            modifiedAt: DateTimeOffset.UtcNow,
            expiresAt: expiresAt.HasValue ? new DateTimeOffset(expiresAt.Value, TimeSpan.Zero) : null,
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["TableName"] = config.TableName,
                ["DataSource"] = config.DataSource
            }));
    }

    private static async Task<IGenericResult> VersionOnWrite(
        SqliteExecutionContext sqliteContext,
        string secretKey, string secretValue, string secretType,
        string? description, DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        var config = sqliteContext.SqliteConfiguration;

        var conn = sqliteContext.CreateConnection();
        await using (conn.ConfigureAwait(false))
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Why: BeginTransactionAsync returns DbTransaction; SqliteCommand.Transaction requires SqliteTransaction.
            // The cast is safe — Microsoft.Data.Sqlite always returns SqliteTransaction at runtime.
            var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    // Step 1: Deactivate the current row (IsCurrent = 0)
                    var deactivateCmd = conn.CreateCommand();
                    await using (deactivateCmd.ConfigureAwait(false))
                    {
                        deactivateCmd.Transaction = transaction;
                        deactivateCmd.CommandTimeout = config.CommandTimeoutSeconds;
                        deactivateCmd.CommandText =
                            $"UPDATE \"{config.TableName}\" " +
                            $"SET \"IsCurrent\" = 0, \"ModifyDate\" = @now " +
                            $"WHERE \"SecretKey\" = @key AND \"IsCurrent\" = 1 AND \"IsDeleted\" = 0";
                        deactivateCmd.Parameters.AddWithValue("@key", secretKey);
                        deactivateCmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                        await deactivateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        SqliteSecretManagerLogger.DebugVersionOnWriteDeactivate(sqliteContext.Logger, secretKey, config.TableName);
                    }

                    // Step 2: Insert the new version with incremented Version counter
                    var insertCmd = conn.CreateCommand();
                    await using (insertCmd.ConfigureAwait(false))
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandTimeout = config.CommandTimeoutSeconds;
                        insertCmd.CommandText =
                            $"INSERT INTO \"{config.TableName}\" " +
                            $"(\"SecretKey\", \"SecretValue\", \"Version\", \"SecretType\", \"Description\", " +
                            $"\"ExpiresAt\", \"CreateDate\", \"ModifyDate\", \"IsCurrent\", \"IsDeleted\") " +
                            $"VALUES (@key, @value, " +
                            $"(SELECT COALESCE(MAX(\"Version\"), 0) + 1 FROM \"{config.TableName}\" WHERE \"SecretKey\" = @key), " +
                            $"@type, @desc, @expires, @now, @now, 1, 0)";
                        insertCmd.Parameters.AddWithValue("@key", secretKey);
                        insertCmd.Parameters.AddWithValue("@value", secretValue);
                        insertCmd.Parameters.AddWithValue("@type", secretType);
                        insertCmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@expires", expiresAt.HasValue
                            ? (object)expiresAt.Value.ToString("O", CultureInfo.InvariantCulture)
                            : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                        await insertCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        SqliteSecretManagerLogger.DebugVersionOnWriteInsert(sqliteContext.Logger, secretKey, config.TableName);
                    }

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return GenericResult.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(SetSecretManagerCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "SetSecret"));
        }

        return GenericResult.Success();
    }
}
