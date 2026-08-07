using System;
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
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// Handler for DeleteSecret commands against a SQLite file.
/// Performs a soft delete: sets IsCurrent = 0 and IsDeleted = 1.
/// </summary>
[TypeOption(typeof(SqliteCommandHandlers), "DeleteSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class SqliteDeleteSecretHandler
    : SecretManagerCommandHandlerBase<DeleteSecretManagerCommand, IGenericResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDeleteSecretHandler"/> class.
    /// </summary>
    public SqliteDeleteSecretHandler()
        : base(id: 3, name: "DeleteSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IGenericResult>> Execute(
        DeleteSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not SqliteExecutionContext sqliteContext)
        {
            return GenericResult<IGenericResult>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(SqliteExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<IGenericResult>.Failure(
                SqliteSecretManagerLogger.SecretKeyRequired(sqliteContext.Logger, "DeleteSecret"));
        }

        var config = sqliteContext.SqliteConfiguration;
        SqliteSecretManagerLogger.TraceHandlerEntry(sqliteContext.Logger, "DeleteSecret", command.SecretKey, config.TableName);
        SqliteSecretManagerLogger.TraceQueryExecution(sqliteContext.Logger, "DeleteSecret", config.TableName);

        var conn = sqliteContext.CreateConnection();
        await using (conn.ConfigureAwait(false))
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            var cmd = conn.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandTimeout = config.CommandTimeoutSeconds;
                cmd.CommandText =
                    $"UPDATE \"{config.TableName}\" " +
                    $"SET \"IsCurrent\" = 0, \"IsDeleted\" = 1, \"ModifyDate\" = @now " +
                    $"WHERE \"SecretKey\" = @key AND \"IsCurrent\" = 1 AND \"IsDeleted\" = 0";
                cmd.Parameters.AddWithValue("@key", command.SecretKey);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                SqliteSecretManagerLogger.SecretDeleted(sqliteContext.Logger, command.SecretKey, config.TableName);
                SqliteSecretManagerLogger.TraceHandlerSuccess(sqliteContext.Logger, "DeleteSecret", command.SecretKey);
                return GenericResult<IGenericResult>.Success(GenericResult.Success());
            }
        }
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(DeleteSecretManagerCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "DeleteSecret"));
        }

        return GenericResult.Success();
    }
}
