using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using ReferenceSecretManagers.AzureKeyVault.Services;
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
/// Handler for ListSecrets commands against Azure Key Vault.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "ListSecrets")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class ListSecretsCommandHandler
    : SecretManagerCommandHandlerBase<ListSecretsManagerCommand, IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretsCommandHandler"/> class.
    /// </summary>
    public ListSecretsCommandHandler()
        : base(id: 4, name: "ListSecrets")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IReadOnlyList<ISecretMetadata>>> Execute(
        ListSecretsManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not AzureKeyVaultExecutionContext akvContext)
        {
            return GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(AzureKeyVaultExecutionContext)));
        }

        try
        {
            var secrets = new List<ISecretMetadata>();

            var maxResults = 25;
            if (command.Parameters.TryGetValue("MaxResults", out var maxObj) && maxObj is int max)
            {
                maxResults = Math.Min(max, 25);
            }

            await foreach (var secretProperties in akvContext.SecretClient
                .GetPropertiesOfSecretsAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var metadata = new AzureKeyVaultSecretMetadata(secretProperties);
                secrets.Add(metadata);

                if (secrets.Count >= maxResults)
                {
                    break;
                }
            }

            return GenericResult<IReadOnlyList<ISecretMetadata>>.Success(secrets);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("ListSecretsFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }
}
