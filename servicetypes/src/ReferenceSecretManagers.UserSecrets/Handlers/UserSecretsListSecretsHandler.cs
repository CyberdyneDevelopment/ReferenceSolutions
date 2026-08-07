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
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.UserSecrets.Services;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// Handler for ListSecrets commands against User Secrets.
/// </summary>
[TypeOption(typeof(UserSecretsCommandHandlers), "ListSecrets")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class UserSecretsListSecretsHandler
    : SecretManagerCommandHandlerBase<ListSecretsManagerCommand, IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsListSecretsHandler"/> class.
    /// </summary>
    public UserSecretsListSecretsHandler()
        : base(id: 2, name: "ListSecrets")
    {
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<IReadOnlyList<ISecretMetadata>>> Execute(
        ListSecretsManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not UserSecretsExecutionContext usContext)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(UserSecretsExecutionContext))));
        }

        var secrets = new List<ISecretMetadata>();

        if (usContext.Secrets == null || usContext.Secrets.Count == 0)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<ISecretMetadata>>.Success(
                (IReadOnlyList<ISecretMetadata>)secrets));
        }

        var maxResults = 100;
        if (command.Parameters.TryGetValue("MaxResults", out var maxObj) && maxObj is int max)
        {
            maxResults = Math.Min(max, 1000);
        }

        foreach (var kvp in usContext.Secrets.Take(maxResults))
        {
            var metadata = new UserSecretsSecretMetadata(
                key: kvp.Key,
                createdAt: usContext.LastModified,
                modifiedAt: usContext.LastModified,
                secretsFilePath: usContext.SecretsFilePath);
            secrets.Add(metadata);
        }

        return Task.FromResult(GenericResult<IReadOnlyList<ISecretMetadata>>.Success(
            (IReadOnlyList<ISecretMetadata>)secrets));
    }
}
