using System;
using System.Collections;
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
using FdwReferenceSecretManagers.EnvironmentVariable.Logging;
using FdwReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw.Services.SecretManagers.Handlers;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Handlers;

/// <summary>
/// Handler for ListSecrets commands against environment variables.
/// </summary>
[TypeOption(typeof(EnvironmentVariableCommandHandlers), "ListSecrets")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class EnvironmentVariableListSecretsHandler
    : SecretManagerCommandHandlerBase<ListSecretsManagerCommand, IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableListSecretsHandler"/> class.
    /// </summary>
    public EnvironmentVariableListSecretsHandler()
        : base(name: "ListSecrets")
    {
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<IReadOnlyList<ISecretMetadata>>> Execute(
        ListSecretsManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not EnvironmentVariableExecutionContext envContext)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<ISecretMetadata>>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(EnvironmentVariableExecutionContext))));
        }

        var config = envContext.EnvironmentVariableConfiguration;
        var maxResults = 100;
        if (command.Parameters.TryGetValue("MaxResults", out var maxObj) && maxObj is int max)
        {
            maxResults = max;
        }

        var secrets = new List<ISecretMetadata>();
        var targetEnum = config.TargetEnum;
        var environmentVariables = Environment.GetEnvironmentVariables(targetEnum);

        foreach (DictionaryEntry entry in environmentVariables)
        {
            var key = entry.Key?.ToString();
            if (key == null) continue;

            if (!string.IsNullOrEmpty(config.Prefix))
            {
                if (!key.StartsWith(config.Prefix, envContext.StringComparison))
                {
                    continue;
                }
            }

            var displayKey = config.StripPrefix && !string.IsNullOrEmpty(config.Prefix)
                ? key.Substring(config.Prefix.Length)
                : key;

            secrets.Add(new EnvironmentVariableSecretMetadata(
                key: displayKey,
                environmentVariableName: key,
                target: targetEnum));

            if (secrets.Count >= maxResults)
            {
                break;
            }
        }

        EnvironmentVariableLogger.SecretsListed(envContext.Logger, secrets.Count, config.Prefix);

        return Task.FromResult(GenericResult<IReadOnlyList<ISecretMetadata>>.Success(
            (IReadOnlyList<ISecretMetadata>)secrets));
    }
}
