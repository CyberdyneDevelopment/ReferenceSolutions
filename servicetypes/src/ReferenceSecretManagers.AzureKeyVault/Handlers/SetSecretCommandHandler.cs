using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using ReferenceSecretManagers.AzureKeyVault.Logging;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// Handler for SetSecret commands against Azure Key Vault.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "SetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class SetSecretCommandHandler
    : SecretManagerCommandHandlerBase<SetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetSecretCommandHandler"/> class.
    /// </summary>
    public SetSecretCommandHandler()
        : base(id: 2, name: "SetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        SetSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not AzureKeyVaultExecutionContext akvContext)
        {
            return GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(AzureKeyVaultExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.SecretKeyRequired(akvContext.Logger, "SetSecret"));
        }

        if (!command.Parameters.TryGetValue("SecretValue", out var secretValueObj) || secretValueObj == null)
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.SecretValueRequired(akvContext.Logger));
        }

        try
        {
            var secretValue = secretValueObj.ToString() ?? string.Empty;
            var secret = new KeyVaultSecret(command.SecretKey, secretValue);
            ApplyOptionalProperties(secret, command);

            var response = await akvContext.SecretClient
                .SetSecretAsync(secret, cancellationToken)
                .ConfigureAwait(false);

            var result = new SecretValue(
                key: response.Value.Name,
                value: response.Value.Value,
                version: response.Value.Properties.Version,
                createdAt: response.Value.Properties.CreatedOn,
                modifiedAt: response.Value.Properties.UpdatedOn,
                expiresAt: response.Value.Properties.ExpiresOn,
                metadata: ConvertTags(response.Value.Properties.Tags));

            return GenericResult<SecretValue>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.OperationFailed(akvContext.Logger, "SetSecret", command.SecretKey, ex.Message));
        }
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

        if (!command.Parameters.ContainsKey("SecretValue"))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("SecretValueRequired"),
                ResultDetails.Create().With("Operation", "SetSecret"));
        }

        return GenericResult.Success();
    }

    private static void ApplyOptionalProperties(KeyVaultSecret secret, SetSecretManagerCommand command)
    {
        if (command.Parameters.TryGetValue("ExpirationDate", out var expiryObj) &&
            expiryObj is DateTimeOffset expiry)
        {
            secret.Properties.ExpiresOn = expiry;
        }

        if (command.Parameters.TryGetValue("Tags", out var tagsObj) &&
            tagsObj is IReadOnlyDictionary<string, string> tags)
        {
            foreach (var tag in tags)
            {
                secret.Properties.Tags[tag.Key] = tag.Value;
            }
        }
    }

    private static Dictionary<string, object> ConvertTags(IDictionary<string, string> tags)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                result[tag.Key] = tag.Value;
            }
        }
        return result;
    }
}
