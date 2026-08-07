using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.UserSecrets.Logging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// Handler for GetSecret commands against User Secrets.
/// </summary>
[TypeOption(typeof(UserSecretsCommandHandlers), "GetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class UserSecretsGetSecretHandler
    : SecretManagerCommandHandlerBase<GetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsGetSecretHandler"/> class.
    /// </summary>
    public UserSecretsGetSecretHandler()
        : base(id: 1, name: "GetSecret")
    {
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<SecretValue>> Execute(
        GetSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not UserSecretsExecutionContext usContext)
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(UserSecretsExecutionContext))));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                UserSecretsLogger.SecretKeyRequired(usContext.Logger, "GetSecret")));
        }

        if (usContext.Secrets == null || !usContext.Secrets.TryGetValue(command.SecretKey, out var value))
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                UserSecretsLogger.SecretNotFound(usContext.Logger, command.SecretKey)));
        }

        var secretValue = new SecretValue(
            key: command.SecretKey,
            value: value,
            version: null,
            createdAt: usContext.LastModified,
            modifiedAt: usContext.LastModified,
            expiresAt: null,
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Provider"] = "UserSecrets",
                ["FilePath"] = usContext.SecretsFilePath ?? string.Empty
            });

        return Task.FromResult(GenericResult<SecretValue>.Success(secretValue));
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
