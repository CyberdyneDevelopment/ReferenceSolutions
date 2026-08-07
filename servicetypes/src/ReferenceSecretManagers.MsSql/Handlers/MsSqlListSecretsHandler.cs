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
using ReferenceSecretManagers.MsSql.Services;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// Handler for ListSecrets commands against SQL Server via DataCommands.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "ListSecrets")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlListSecretsHandler
    : SecretManagerCommandHandlerBase<ListSecretsManagerCommand, IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlListSecretsHandler"/> class.
    /// </summary>
    public MsSqlListSecretsHandler()
        : base(id: 4, name: "ListSecrets")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IReadOnlyList<ISecretMetadata>>> Execute(
        ListSecretsManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        var config = sqlContext.MsSqlConfiguration;

        MsSqlSecretManagerLogger.TraceSqlQueryExecution(sqlContext.Logger, "ListSecrets", config.Schema, config.TableName);

        var query = DataQuery.From<SecretRow>(sqlContext.DataStoreName, config.Schema, config.TableName)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await sqlContext.Gateway
            .Execute<IEnumerable<SecretRow>>(query.Command, sqlContext.SecretTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<IReadOnlyList<ISecretMetadata>>();
        }

        var secrets = (result.Value ?? Enumerable.Empty<SecretRow>())
            .Select(row => (ISecretMetadata)new MsSqlSecretMetadata(
                key: row.SecretKey,
                schema: config.Schema,
                tableName: config.TableName,
                version: row.Version,
                secretType: row.SecretType,
                description: row.Description,
                createdAt: row.CreateDate,
                modifiedAt: row.ModifyDate,
                expiresAt: row.ExpiresAt.HasValue
                    ? new DateTimeOffset(row.ExpiresAt.Value, TimeSpan.Zero)
                    : null))
            .ToList();

        MsSqlSecretManagerLogger.SecretsListed(sqlContext.Logger, secrets.Count, config.Schema, config.TableName);

        return GenericResult<IReadOnlyList<ISecretMetadata>>.Success((IReadOnlyList<ISecretMetadata>)secrets);
    }
}
