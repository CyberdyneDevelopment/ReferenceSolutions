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
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// Handler for GetSecret commands against a SQLite file via direct SQL.
/// </summary>
[TypeOption(typeof(SqliteCommandHandlers), "GetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class SqliteGetSecretHandler
    : SecretManagerCommandHandlerBase<GetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteGetSecretHandler"/> class.
    /// </summary>
    public SqliteGetSecretHandler()
        : base(id: 1, name: "GetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        GetSecretManagerCommand command,
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
                SqliteSecretManagerLogger.SecretKeyRequired(sqliteContext.Logger, "GetSecret"));
        }

        var config = sqliteContext.SqliteConfiguration;
        SqliteSecretManagerLogger.TraceHandlerEntry(sqliteContext.Logger, "GetSecret", command.SecretKey, config.TableName);
        SqliteSecretManagerLogger.TraceQueryExecution(sqliteContext.Logger, "GetSecret", config.TableName);

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
                    $"WHERE \"SecretKey\" = @key AND \"IsCurrent\" = 1 AND \"IsDeleted\" = 0 " +
                    $"LIMIT 1";
                cmd.Parameters.AddWithValue("@key", command.SecretKey);

                var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return GenericResult<SecretValue>.Failure(
                            SqliteSecretManagerLogger.SecretNotFound(sqliteContext.Logger, command.SecretKey, config.TableName));
                    }

                    var row = MapRow(reader);

                    SqliteSecretManagerLogger.SecretRetrieved(sqliteContext.Logger, command.SecretKey, config.TableName);
                    SqliteSecretManagerLogger.TraceHandlerSuccess(sqliteContext.Logger, "GetSecret", command.SecretKey);

                    return GenericResult<SecretValue>.Success(new SecretValue(
                        key: row.SecretKey,
                        value: row.SecretValue,
                        version: row.Version.ToString(CultureInfo.InvariantCulture),
                        createdAt: new DateTimeOffset(row.CreateDate, TimeSpan.Zero),
                        modifiedAt: new DateTimeOffset(row.ModifyDate, TimeSpan.Zero),
                        expiresAt: row.ExpiresAt.HasValue ? new DateTimeOffset(row.ExpiresAt.Value, TimeSpan.Zero) : null,
                        metadata: new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["TableName"] = config.TableName,
                            ["DataSource"] = config.DataSource
                        }));
                }
            }
        }
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(GetSecretManagerCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "GetSecret"));
        }

        return GenericResult.Success();
    }

    internal static SecretRow MapRow(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var expiresAtText = reader.IsDBNull(5) ? null : reader.GetString(5);
        DateTime? expiresAt = expiresAtText is not null
            ? DateTime.Parse(expiresAtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            : null;

        return new SecretRow(
            SecretKey: reader.GetString(0),
            SecretValue: reader.GetString(1),
            Version: reader.GetInt32(2),
            SecretType: reader.GetString(3),
            Description: reader.IsDBNull(4) ? null : reader.GetString(4),
            ExpiresAt: expiresAt,
            CreateDate: DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            ModifyDate: DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            IsCurrent: reader.GetInt32(8) == 1,
            IsDeleted: reader.GetInt32(9) == 1);
    }
}
