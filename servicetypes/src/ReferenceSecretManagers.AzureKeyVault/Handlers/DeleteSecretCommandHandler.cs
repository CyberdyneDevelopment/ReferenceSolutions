using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
/// Handler for DeleteSecret commands against Azure Key Vault.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "DeleteSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class DeleteSecretCommandHandler
    : SecretManagerCommandHandlerBase<DeleteSecretManagerCommand, IGenericResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSecretCommandHandler"/> class.
    /// </summary>
    public DeleteSecretCommandHandler()
        : base(id: 3, name: "DeleteSecret")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IGenericResult>> Execute(
        DeleteSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not AzureKeyVaultExecutionContext akvContext)
        {
            return GenericResult<IGenericResult>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(AzureKeyVaultExecutionContext)));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<IGenericResult>.Failure(
                AzureKeyVaultLogger.SecretKeyRequired(akvContext.Logger, "DeleteSecret"));
        }

        try
        {
            var operation = await akvContext.SecretClient
                .StartDeleteSecretAsync(command.SecretKey, cancellationToken)
                .ConfigureAwait(false);

            await operation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

            // Check if permanent deletion is requested
            var permanentDelete = false;
            if (command.Parameters.TryGetValue("PermanentDelete", out var permObj) &&
                permObj is bool permanent)
            {
                permanentDelete = permanent;
            }

            if (permanentDelete)
            {
                await akvContext.SecretClient
                    .PurgeDeletedSecretAsync(command.SecretKey, cancellationToken)
                    .ConfigureAwait(false);
            }

            return GenericResult<IGenericResult>.Success(GenericResult.Success());
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericResult>.Failure(
                AzureKeyVaultLogger.OperationFailed(akvContext.Logger, "DeleteSecret", command.SecretKey, ex.Message));
        }
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
