using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
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
/// Handler for GetSecret commands against SQL Server via DataCommands.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "GetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlGetSecretHandler
    : SecretManagerCommandHandlerBase<GetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlGetSecretHandler"/> class.
    /// </summary>
    public MsSqlGetSecretHandler()
        : base(id: 1, name: "GetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        GetSecretManagerCommand command,
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
                MsSqlSecretManagerLogger.SecretKeyRequired(sqlContext.Logger, "GetSecret"));
        }

        var config = sqlContext.MsSqlConfiguration;
        MsSqlSecretManagerLogger.TraceSqlQueryExecution(sqlContext.Logger, "GetSecret", config.Schema, config.TableName);

        var query = DataQuery.From<SecretRow>(sqlContext.DataStoreName, config.Schema, config.TableName)
            .Where("SecretKey", command.SecretKey)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await sqlContext.Gateway
            .Execute<IEnumerable<SecretRow>>(query.Command, sqlContext.SecretTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<SecretValue>();
        }

        var row = result.Value?.FirstOrDefault();
        if (row is null)
        {
            return GenericResult<SecretValue>.Failure(
                MsSqlSecretManagerLogger.SecretNotFound(sqlContext.Logger, command.SecretKey, config.Schema, config.TableName));
        }

        MsSqlSecretManagerLogger.SecretRetrieved(sqlContext.Logger, command.SecretKey, config.Schema, config.TableName);

        var secretValue = new SecretValue(
            key: row.SecretKey,
            value: row.SecretValue,
            version: row.Version.ToString(CultureInfo.InvariantCulture),
            createdAt: row.CreateDate,
            modifiedAt: row.ModifyDate,
            expiresAt: row.ExpiresAt.HasValue ? new DateTimeOffset(row.ExpiresAt.Value, TimeSpan.Zero) : null,
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Schema"] = config.Schema,
                ["TableName"] = config.TableName
            });

        return GenericResult<SecretValue>.Success(secretValue);
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
}
