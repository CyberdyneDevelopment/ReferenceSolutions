using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
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
/// Handler for RevokeCredential commands against SQL Server.
/// Sets IsRevoked = true on API key tokens in the auth.PersonalAccessToken table.
/// </summary>
[TypeOption(typeof(MsSqlCommandHandlers), "RevokeCredential")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class MsSqlRevokeCredentialHandler
    : SecretManagerCommandHandlerBase<RevokeCredentialCommand, bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlRevokeCredentialHandler"/> class.
    /// </summary>
    public MsSqlRevokeCredentialHandler()
        : base(id: 7, name: "RevokeCredential")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> Execute(
        RevokeCredentialCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not MsSqlExecutionContext sqlContext)
        {
            return GenericResult<bool>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(MsSqlExecutionContext)));
        }

        // Why: Only ApiKey credentials support revocation; passwords are replaced, not revoked
        if (!string.Equals(command.CredentialType, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<bool>.Failure(
                CredentialLog.UnsupportedCredentialType(sqlContext.Logger, command.CredentialType));
        }

        CredentialLog.TraceCredentialQuery(sqlContext.Logger, "RevokeCredential", "auth");

        var revokeRow = new PersonalAccessTokenRow
        {
            Id = command.CredentialId,
            UserId = Guid.Empty,
            TokenHash = string.Empty,
            Label = null,
            ExpiresAt = null,
            IsRevoked = true,
            CreateDate = DateTimeOffset.UtcNow
        };

        var update = new UpdateCommandBuilder<PersonalAccessTokenRow>("PersonalAccessToken")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("Id", command.CredentialId)
            .Where("IsRevoked", false)
            .Value(revokeRow);

        var result = await sqlContext.Gateway
            .Execute(update.Command, sqlContext.PersonalAccessTokenTarget, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<bool>();
        }

        CredentialLog.CredentialRevoked(sqlContext.Logger, command.CredentialId, command.CredentialType);
        return GenericResult<bool>.Success(true);
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(RevokeCredentialCommand command)
    {
        if (command.CredentialId == Guid.Empty)
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "RevokeCredential").With("Field", "CredentialId"));
        }

        if (string.IsNullOrWhiteSpace(command.CredentialType))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretKeyRequired"),
                ResultDetails.Create().With("Operation", "RevokeCredential").With("Field", "CredentialType"));
        }

        return GenericResult.Success();
    }
}
