using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests.TestDoubles;

// Why: a bare ISecretManagerCommand implementation that is NOT one of the concrete command types
// (GetSecretManagerCommand / ListSecretsManagerCommand) the EnvironmentVariable handlers' generic base
// class casts to. Declaring CommandType = "GetSecret" routes it to EnvironmentVariableGetSecretHandler,
// whose InvokeBoxed does an unconditional (GetSecretManagerCommand)command cast — since this object is
// not that type, the cast throws InvalidCastException. Used to exercise the mechanism-level
// exception-to-Failure conversion in EnvironmentVariableSecretManager.ExecuteBatch.
internal sealed class FakeSecretManagerCommand : ISecretManagerCommand
{
    public FakeSecretManagerCommand(string commandType, string? secretKey = null)
    {
        CommandType = commandType;
        SecretKey = secretKey;
    }

    private readonly Guid _id = Guid.NewGuid();

    Guid IGenericCommand.CommandId => _id;

    string ISecretManagerCommand.CommandId => _id.ToString("D");

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public string CommandType { get; }

    public string Category => "SecretManagement";

    public string? Container => null;

    public string? SecretKey { get; }

    public Type ExpectedResultType => typeof(object);

    public TimeSpan? Timeout => null;

    public IReadOnlyDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();

    public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

    public bool IsSecretModifying => false;

    public ISecretManagerCommand WithParameters(IReadOnlyDictionary<string, object?> newParameters) => this;

    public ISecretManagerCommand WithMetadata(IReadOnlyDictionary<string, object> newMetadata) => this;
}
