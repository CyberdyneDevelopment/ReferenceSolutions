using System;
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
/// Handler for DeleteSecret commands against SQL Server.
/// Performs soft delete using IsDeleted/IsCurrent flags.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "DeleteSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlDeleteSecretHandler
    : SecretManagerCommandHandlerBase<DeleteSecretManagerCommand, IGenericResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDeleteSecretHandler"/> class.
    /// </summary>
    public MsSqlDeleteSecretHandler()
        : base(id: 3, name: "DeleteSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IGenericResult>> Execute(
        DeleteSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<IGenericResult>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<IGenericResult>.Failure(
                MsSqlSecretManagerLogger.SecretKeyRequired(sqlContext.Logger, "DeleteSecret"));
        }

        var config = sqlContext.MsSqlConfiguration;
        MsSqlSecretManagerLogger.TraceSqlQueryExecution(sqlContext.Logger, "DeleteSecret", config.Schema, config.TableName);

        // Soft delete: mark as IsDeleted=1, IsCurrent=0
        var deleteRow = new SecretRow
        {
            SecretKey = command.SecretKey, SecretValue = string.Empty, Version = 0,
            SecretType = string.Empty, Description = null, ExpiresAt = null,
            CreateDate = DateTimeOffset.UtcNow, ModifyDate = DateTimeOffset.UtcNow,
            IsCurrent = false, IsDeleted = true
        };

        var update = new UpdateCommandBuilder<SecretRow>(config.TableName)
            .DataStore(sqlContext.DataStoreName)
            .Path(config.Schema)
            .Where("SecretKey", command.SecretKey)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(deleteRow);

        var result = await sqlContext.Gateway
            .Execute(update.Command, sqlContext.SecretTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<IGenericResult>();
        }

        MsSqlSecretManagerLogger.SecretDeleted(sqlContext.Logger, command.SecretKey, config.Schema, config.TableName);
        return GenericResult<IGenericResult>.Success(GenericResult.Success());
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
