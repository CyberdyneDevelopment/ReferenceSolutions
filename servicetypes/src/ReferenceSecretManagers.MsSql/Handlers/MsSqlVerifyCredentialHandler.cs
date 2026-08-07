using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// Handler for VerifyCredential commands against SQL Server.
/// Verifies password hashes or API key tokens against auth schema tables.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "VerifyCredential")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlVerifyCredentialHandler
    : SecretManagerCommandHandlerBase<VerifyCredentialCommand, CredentialVerificationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlVerifyCredentialHandler"/> class.
    /// </summary>
    public MsSqlVerifyCredentialHandler()
        : base(id: 5, name: "VerifyCredential")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<CredentialVerificationResult>> Execute(
        VerifyCredentialCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<CredentialVerificationResult>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        CredentialLog.VerificationAttempt(sqlContext.Logger, command.UserId, command.CredentialType);

        // Why: Credential type determines which auth table and hash algorithm to use
        if (string.Equals(command.CredentialType, "Password", StringComparison.OrdinalIgnoreCase))
        {
            return await VerifyPassword(command, sqlContext, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(command.CredentialType, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return await VerifyApiKey(command, sqlContext, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<CredentialVerificationResult>.Failure(
            CredentialLog.UnsupportedCredentialType(sqlContext.Logger, command.CredentialType));
    }

    private static async Task<IGenericResult<CredentialVerificationResult>> VerifyPassword(
        VerifyCredentialCommand command,
        MsSqlExecutionContext sqlContext,
        CancellationToken cancellationToken)
    {
        if (sqlContext.PasswordHasher is null)
        {
            return GenericResult<CredentialVerificationResult>.Failure(
                CredentialLog.PasswordHasherNotAvailable(sqlContext.Logger));
        }

        CredentialLog.TraceCredentialQuery(sqlContext.Logger, "VerifyPassword", "auth");

        var query = DataQuery.From<UserPasswordRow>(
                sqlContext.DataStoreName, "usr", "Users")
            .Where("Id", command.UserId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await sqlContext.Gateway
            .Execute<IEnumerable<UserPasswordRow>>(query.Command, sqlContext.UsersTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<CredentialVerificationResult>();
        }

        var row = result.Value?.FirstOrDefault();
        if (row is null)
        {
            CredentialLog.NoPasswordHashFound(sqlContext.Logger, command.UserId);
            return GenericResult<CredentialVerificationResult>.Success(
                new CredentialVerificationResult { IsValid = false, UserId = command.UserId });
        }

        var isValid = sqlContext.PasswordHasher.VerifyPassword(command.CandidateValue, row.PasswordHash);

        if (isValid)
        {
            CredentialLog.VerificationSuccess(sqlContext.Logger, command.UserId, "Password");
        }
        else
        {
            CredentialLog.VerificationFailed(sqlContext.Logger, command.UserId, "Password");
        }

        return GenericResult<CredentialVerificationResult>.Success(
            new CredentialVerificationResult
            {
                IsValid = isValid,
                UserId = command.UserId,
                Username = row.Username
            });
    }

    private static async Task<IGenericResult<CredentialVerificationResult>> VerifyApiKey(
        VerifyCredentialCommand command,
        MsSqlExecutionContext sqlContext,
        CancellationToken cancellationToken)
    {
        if (sqlContext.TokenHasher is null)
        {
            return GenericResult<CredentialVerificationResult>.Failure(
                CredentialLog.TokenHasherNotAvailable(sqlContext.Logger));
        }

        if (string.IsNullOrEmpty(sqlContext.HmacKey))
        {
            return GenericResult<CredentialVerificationResult>.Failure(
                CredentialLog.HmacKeyNotConfigured(sqlContext.Logger));
        }

        CredentialLog.TraceCredentialQuery(sqlContext.Logger, "VerifyApiKey", "auth");

        // Why: Hash the candidate token first, then look up by hash for constant-time comparison
        var candidateHash = sqlContext.TokenHasher.Hash(command.CandidateValue, sqlContext.HmacKey);

        var query = DataQuery.From<PersonalAccessTokenRow>(
                "AuthDb", "auth", "PersonalAccessToken")
            .Where("TokenHash", candidateHash)
            .Build();

        var result = await sqlContext.Gateway
            .Execute<IEnumerable<PersonalAccessTokenRow>>(query.Command, sqlContext.PersonalAccessTokenTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<CredentialVerificationResult>();
        }

        var row = result.Value?.FirstOrDefault();
        if (row is null || row.IsRevoked)
        {
            CredentialLog.ApiKeyNotFound(sqlContext.Logger);
            return GenericResult<CredentialVerificationResult>.Success(
                new CredentialVerificationResult { IsValid = false, UserId = command.UserId });
        }

        // Why: Check expiration after finding the token to provide specific failure info
        if (row.ExpiresAt.HasValue && row.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            CredentialLog.ApiKeyExpired(sqlContext.Logger, row.UserId);
            return GenericResult<CredentialVerificationResult>.Success(
                new CredentialVerificationResult
                {
                    IsValid = false,
                    UserId = row.UserId,
                    ExpiresAt = row.ExpiresAt
                });
        }

        CredentialLog.VerificationSuccess(sqlContext.Logger, row.UserId, "ApiKey");

        return GenericResult<CredentialVerificationResult>.Success(
            new CredentialVerificationResult
            {
                IsValid = true,
                UserId = row.UserId,
                ExpiresAt = row.ExpiresAt
            });
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(VerifyCredentialCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "VerifyCredential").With("Field", "UserId"));
        }

        if (string.IsNullOrWhiteSpace(command.CredentialType))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "VerifyCredential").With("Field", "CredentialType"));
        }

        return GenericResult.Success();
    }
}
