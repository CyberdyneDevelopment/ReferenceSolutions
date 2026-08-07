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
/// Handler for GetSecret commands against Azure Key Vault.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "GetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class GetSecretCommandHandler
    : SecretManagerCommandHandlerBase<GetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretCommandHandler"/> class.
    /// </summary>
    public GetSecretCommandHandler()
        : base(id: 1, name: "GetSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        GetSecretManagerCommand command,
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
                AzureKeyVaultLogger.SecretKeyRequired(akvContext.Logger, "GetSecret"));
        }

        try
        {
            var response = await akvContext.SecretClient
                .GetSecretAsync(command.SecretKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var azureSecret = response.Value;

            var secretValue = new SecretValue(
                key: azureSecret.Name,
                value: azureSecret.Value,
                version: azureSecret.Properties.Version,
                createdAt: azureSecret.Properties.CreatedOn,
                modifiedAt: azureSecret.Properties.UpdatedOn,
                expiresAt: azureSecret.Properties.ExpiresOn,
                metadata: ConvertTags(azureSecret.Properties.Tags));

            return GenericResult<SecretValue>.Success(secretValue);
        }
        catch (Exception ex)
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.OperationFailed(akvContext.Logger, "GetSecret", command.SecretKey, ex.Message));
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
