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
using ReferenceSecretManagers.EnvironmentVariable.Logging;
using Fdw.Services.SecretManagers.Handlers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Handlers;

/// <summary>
/// Handler for GetSecret commands against environment variables.
/// </summary>
[TypeOption(typeof(EnvironmentVariableCommandHandlers), "GetSecret")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class EnvironmentVariableGetSecretHandler
    : SecretManagerCommandHandlerBase<GetSecretManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableGetSecretHandler"/> class.
    /// </summary>
    public EnvironmentVariableGetSecretHandler()
        : base(id: 1, name: "GetSecret")
    {
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<SecretValue>> Execute(
        GetSecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not EnvironmentVariableExecutionContext envContext)
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(EnvironmentVariableExecutionContext))));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                EnvironmentVariableLogger.SecretKeyRequired(envContext.Logger, "GetSecret")));
        }

        var config = envContext.EnvironmentVariableConfiguration;
        var variableName = GetEnvironmentVariableName(command.SecretKey, config);
        EnvironmentVariableLogger.TraceSecretKeyLookup(envContext.Logger, command.SecretKey, variableName);
        var value = Environment.GetEnvironmentVariable(variableName, config.TargetEnum);

        if (value == null)
        {
            return Task.FromResult(GenericResult<SecretValue>.Failure(
                EnvironmentVariableLogger.EnvironmentVariableNotFound(envContext.Logger, variableName)));
        }

        EnvironmentVariableLogger.SecretRetrieved(envContext.Logger, variableName);

        var secretValue = new SecretValue(
            key: config.StripPrefix ? command.SecretKey : variableName,
            value: value,
            version: null,
            createdAt: null,
            modifiedAt: null,
            expiresAt: null,
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["EnvironmentVariableName"] = variableName,
                ["Target"] = config.Target
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

    private static string GetEnvironmentVariableName(string secretKey, EnvironmentVariableConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.Prefix) && config.StripPrefix)
        {
            return config.Prefix + secretKey;
        }

        return secretKey;
    }
}
