using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
/// Handler for StoreCredential commands against SQL Server.
/// Stores hashed passwords or API key tokens in auth schema tables.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "StoreCredential")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlStoreCredentialHandler
    : SecretManagerCommandHandlerBase<StoreCredentialCommand, CredentialStorageResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlStoreCredentialHandler"/> class.
    /// </summary>
    public MsSqlStoreCredentialHandler()
        : base(id: 6, name: "StoreCredential")
    {
    }

    /// <inheritdoc />
#pragma warning disable MA0051
    protected override async Task<IGenericResult<CredentialStorageResult>> Execute(
        StoreCredentialCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<CredentialStorageResult>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        // Why: Credential type determines which auth table and hash algorithm to use
        if (string.Equals(command.CredentialType, "Password", StringComparison.OrdinalIgnoreCase))
        {
            return await StorePassword(command, sqlContext, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(command.CredentialType, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return await StoreApiKey(command, sqlContext, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<CredentialStorageResult>.Failure(
            CredentialLog.UnsupportedCredentialType(sqlContext.Logger, command.CredentialType));
    }

    private static async Task<IGenericResult<CredentialStorageResult>> StorePassword(
        StoreCredentialCommand command,
        MsSqlExecutionContext sqlContext,
        CancellationToken cancellationToken)
    {
        if (sqlContext.PasswordHasher is null)
        {
            return GenericResult<CredentialStorageResult>.Failure(
                CredentialLog.PasswordHasherNotAvailable(sqlContext.Logger));
        }

        CredentialLog.TraceCredentialQuery(sqlContext.Logger, "StorePassword", "auth");

        var passwordHash = sqlContext.PasswordHasher.HashPassword(command.PlaintextValue);

        // Why: Version-on-write — deactivate current password before inserting new one
        var deactivateRow = new UserPasswordRow { PasswordHash = string.Empty, Username = string.Empty };
        var deactivate = new UpdateCommandBuilder<UserPasswordRow>("Users")
            .DataStore(sqlContext.DataStoreName)
            .Path("usr")
            .Where("Id", command.UserId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(deactivateRow);

        var deactivateResult = await sqlContext.Gateway
            .Execute(deactivate.Command, sqlContext.UsersTarget, cancellationToken)
            .ConfigureAwait(false);

        if (deactivateResult.IsFailure)
        {
            return deactivateResult.ToNewResult<CredentialStorageResult>();
        }

        var insertRow = new UserPasswordRow { PasswordHash = passwordHash, Username = string.Empty };
        // Why: Addressing lives in sqlContext.UsersTarget (DataStoreTarget) passed to the gateway.
        // InsertCommand carries only the entity data — no container/connection properties.
        var insert = new InsertCommand<UserPasswordRow>(insertRow);

        var insertResult = await sqlContext.Gateway
            .Execute(insert, sqlContext.UsersTarget, cancellationToken)
            .ConfigureAwait(false);

        if (insertResult.IsFailure)
        {
            return insertResult.ToNewResult<CredentialStorageResult>();
        }

        CredentialLog.CredentialStored(sqlContext.Logger, command.UserId, "Password");

        return GenericResult<CredentialStorageResult>.Success(
            new CredentialStorageResult
            {
                CredentialId = command.UserId,
                ExpiresAt = command.ExpiresAt
            });
    }

    private static async Task<IGenericResult<CredentialStorageResult>> StoreApiKey(
        StoreCredentialCommand command,
        MsSqlExecutionContext sqlContext,
        CancellationToken cancellationToken)
    {
        if (sqlContext.TokenHasher is null)
        {
            return GenericResult<CredentialStorageResult>.Failure(
                CredentialLog.TokenHasherNotAvailable(sqlContext.Logger));
        }

        if (sqlContext.TokenGenerator is null)
        {
            return GenericResult<CredentialStorageResult>.Failure(
                CredentialLog.TokenGeneratorNotAvailable(sqlContext.Logger));
        }

        if (string.IsNullOrEmpty(sqlContext.HmacKey))
        {
            return GenericResult<CredentialStorageResult>.Failure(
                CredentialLog.HmacKeyNotConfigured(sqlContext.Logger));
        }

        CredentialLog.TraceCredentialQuery(sqlContext.Logger, "StoreApiKey", "auth");

        // Why: Generate a new random token, hash it for storage, return raw value once
        var rawToken = sqlContext.TokenGenerator.Generate("prod");
        var tokenHash = sqlContext.TokenHasher.Hash(rawToken, sqlContext.HmacKey);
        var tokenId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var tokenRow = new PersonalAccessTokenRow
        {
            Id = tokenId,
            UserId = command.UserId,
            TokenHash = tokenHash,
            Label = command.Label,
            ExpiresAt = command.ExpiresAt,
            IsRevoked = false,
            CreateDate = now
        };

        // Why: Addressing lives in sqlContext.PersonalAccessTokenTarget (DataStoreTarget) passed to the
        // gateway. InsertCommand carries only the entity data — no container/connection properties.
        var insert = new InsertCommand<PersonalAccessTokenRow>(tokenRow);

        var insertResult = await sqlContext.Gateway
            .Execute(insert, sqlContext.PersonalAccessTokenTarget, cancellationToken)
            .ConfigureAwait(false);

        if (insertResult.IsFailure)
        {
            return insertResult.ToNewResult<CredentialStorageResult>();
        }

        CredentialLog.CredentialStored(sqlContext.Logger, command.UserId, "ApiKey");

        return GenericResult<CredentialStorageResult>.Success(
            new CredentialStorageResult
            {
                CredentialId = tokenId,
                ExpiresAt = command.ExpiresAt,
                RawValue = rawToken
            });
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(StoreCredentialCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "StoreCredential").With("Field", "UserId"));
        }

        if (string.IsNullOrWhiteSpace(command.CredentialType))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "StoreCredential").With("Field", "CredentialType"));
        }

        if (string.IsNullOrWhiteSpace(command.PlaintextValue))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretValueRequired"),
                ResultDetails.Create().With("Operation", "StoreCredential").With("Field", "PlaintextValue"));
        }

        return GenericResult.Success();
    }
}
