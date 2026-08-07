using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.MsSql.Logging;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// Handler for SetSecret commands against SQL Server via DataCommands.
/// Uses version-on-write pattern with IsCurrent/IsDeleted flags.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "SetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlSetSecretHandler
    : SecretManagerCommandHandlerBase<SetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSetSecretHandler"/> class.
    /// </summary>
    public MsSqlSetSecretHandler()
        : base(id: 2, name: "SetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        SetSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<SecretValue>.Failure(
                MsSqlSecretManagerLogger.SecretKeyRequired(sqlContext.Logger, "SetSecret"));
        }

        if (!command.Parameters.TryGetValue("Value", out var valueObj) || valueObj is not string secretValue || string.IsNullOrEmpty(secretValue))
        {
            return GenericResult<SecretValue>.Failure(
                MsSqlSecretManagerLogger.SecretValueRequired(sqlContext.Logger));
        }

        // Why: SecretType is a required input — a fabricated "Password" default is a silent fallback
        // (NO-FALLBACKS rule). Fail loud, mirroring the SecretKey/SecretValue requirement above.
        if (!command.Parameters.TryGetValue("SecretType", out var secretTypeObj)
            || secretTypeObj is not string secretType
            || string.IsNullOrWhiteSpace(secretType))
        {
            return GenericResult<SecretValue>.Failure(
                MsSqlSecretManagerLogger.SecretTypeRequired(sqlContext.Logger));
        }

        command.Parameters.TryGetValue("Description", out var descriptionObj);
        var description = descriptionObj as string;

        command.Parameters.TryGetValue("ExpiresAt", out var expiresAtObj);
        DateTime? expiresAt = expiresAtObj is DateTimeOffset dto ? dto.UtcDateTime : null;

        var config = sqlContext.MsSqlConfiguration;
        MsSqlSecretManagerLogger.TraceSqlQueryExecution(sqlContext.Logger, "SetSecret", config.Schema, config.TableName);

        var writeResult = await VersionOnWrite(
            sqlContext, command.SecretKey, secretValue, secretType, description, expiresAt, cancellationToken)
            .ConfigureAwait(false);

        if (writeResult.IsFailure)
        {
            return writeResult.ToNewResult<SecretValue>();
        }

        MsSqlSecretManagerLogger.SecretSet(sqlContext.Logger, command.SecretKey, config.Schema, config.TableName);

        return GenericResult<SecretValue>.Success(new SecretValue(
            key: command.SecretKey,
            value: string.Empty,
            version: null,
            createdAt: null,
            modifiedAt: DateTimeOffset.UtcNow,
            expiresAt: expiresAt.HasValue ? new DateTimeOffset(expiresAt.Value, TimeSpan.Zero) : null,
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Schema"] = config.Schema,
                ["TableName"] = config.TableName
            }));
    }

    private static async Task<IGenericResult> VersionOnWrite(
        MsSqlExecutionContext sqlContext,
        string secretKey, string secretValue, string secretType,
        string? description, DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        var config = sqlContext.MsSqlConfiguration;

        // Deactivate current row
        var deactivateRow = new SecretRow
        {
            SecretKey = secretKey, SecretValue = string.Empty, Version = 0,
            SecretType = string.Empty, Description = null, ExpiresAt = null,
            CreateDate = DateTimeOffset.UtcNow, ModifyDate = DateTimeOffset.UtcNow,
            IsCurrent = false, IsDeleted = false
        };

        var deactivate = new UpdateCommandBuilder<SecretRow>(config.TableName)
            .DataStore(sqlContext.DataStoreName)
            .Path(config.Schema)
            .Where("SecretKey", secretKey)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(deactivateRow);

        var deactivateResult = await sqlContext.Gateway
            .Execute(deactivate.Command, sqlContext.SecretTarget, cancellationToken)
            .ConfigureAwait(false);

        if (deactivateResult.IsFailure)
        {
            return deactivateResult;
        }

        // Insert new version
        var newRow = new SecretRow
        {
            SecretKey = secretKey, SecretValue = secretValue, Version = 0,
            SecretType = secretType, Description = description, ExpiresAt = expiresAt,
            CreateDate = DateTimeOffset.UtcNow, ModifyDate = DateTimeOffset.UtcNow,
            IsCurrent = true, IsDeleted = false
        };

        // Why: Addressing lives in sqlContext.SecretTarget (DataStoreTarget) passed to the gateway.
        // InsertCommand carries only the entity data — no container/connection properties.
        var insert = new InsertCommand<SecretRow>(newRow);

        return await sqlContext.Gateway
            .Execute(insert, sqlContext.SecretTarget, cancellationToken)
            .ConfigureAwait(false);
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
