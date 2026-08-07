using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for the <see cref="EnvironmentVariableCommandHandlers"/> TypeCollection lookup surface and the
/// three handler implementations dispatched through it (GetSecret, ListSecrets, NotFound).
/// </summary>
public class EnvironmentVariableCommandHandlersTests
{
    private static EnvironmentVariableExecutionContext MakeContext(string prefix = "", bool stripPrefix = true)
    {
        var config = new EnvironmentVariableConfiguration
        {
            Id = Guid.NewGuid(),
            Prefix = prefix,
            StripPrefix = stripPrefix,
            Separator = "__",
        };
        return new EnvironmentVariableExecutionContext(NullLogger.Instance, config, config.Id.ToString());
    }

    // ── TypeCollection lookups ─────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameGetSecretReturnsHandlerWithExpectedIdentity()
    {
        // Act
        var handler = EnvironmentVariableCommandHandlers.ByName("GetSecret");

        // Assert
        handler.ShouldBeOfType<EnvironmentVariableGetSecretHandler>();
        handler.Id.ShouldBe(1);
        handler.Name.ShouldBe("GetSecret");
        handler.CommandTypeClass.ShouldBe(typeof(GetSecretManagerCommand));
        handler.ResultType.ShouldBe(typeof(SecretValue));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameListSecretsReturnsHandlerWithExpectedIdentity()
    {
        // Act
        var handler = EnvironmentVariableCommandHandlers.ByName("ListSecrets");

        // Assert
        handler.ShouldBeOfType<EnvironmentVariableListSecretsHandler>();
        handler.Id.ShouldBe(2);
        handler.Name.ShouldBe("ListSecrets");
        handler.CommandTypeClass.ShouldBe(typeof(ListSecretsManagerCommand));
        handler.ResultType.ShouldBe(typeof(IReadOnlyList<ISecretMetadata>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameUnknownReturnsNotFoundSentinel()
    {
        // Act
        var handler = EnvironmentVariableCommandHandlers.ByName("Bogus");

        // Assert
        handler.ShouldBeSameAs(EnvironmentVariableCommandHandlers.NotFound);
        handler.Id.ShouldBe(0);
        handler.Name.ShouldBe("NotFound");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData(1, "GetSecret")]
    [InlineData(2, "ListSecrets")]
    public void ByIdReturnsMatchingHandler(int id, string expectedName)
    {
        // Act
        var handler = EnvironmentVariableCommandHandlers.ById(id);

        // Assert
        handler.Name.ShouldBe(expectedName);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdUnknownReturnsNotFoundSentinel()
    {
        // Act
        var handler = EnvironmentVariableCommandHandlers.ById(999);

        // Assert
        handler.ShouldBeSameAs(EnvironmentVariableCommandHandlers.NotFound);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsExactlyThreeRegisteredHandlers()
    {
        // Act
        var all = EnvironmentVariableCommandHandlers.All();

        // Assert
        all.Count.ShouldBe(3);
    }

    // ── EnvironmentVariableGetSecretHandler ────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateGetSecretCommandWithSecretKeySucceeds()
    {
        // Arrange
        var handler = new EnvironmentVariableGetSecretHandler();
        var command = new GetSecretManagerCommand(null, "SOME_KEY");

        // Act
        var result = handler.Validate(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateNullCommandReturnsFailure()
    {
        // Arrange
        var handler = new EnvironmentVariableGetSecretHandler();

        // Act
        var result = handler.Validate(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateWrongCommandTypeReturnsFailure()
    {
        // Arrange — Why: GetSecretHandler.Validate rejects any command that is not a GetSecretManagerCommand.
        var handler = new EnvironmentVariableGetSecretHandler();
        var command = ListSecretsManagerCommand.All(null);

        // Act
        var result = handler.Validate(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task InvokeBoxedGetSecretExistingVariableReturnsSuccess()
    {
        // Arrange
        var prefix = $"FDWTEST_{Guid.NewGuid():N}_";
        var variableName = prefix + "PASSWORD";
        Environment.SetEnvironmentVariable(variableName, "p@ss", EnvironmentVariableTarget.Process);
        try
        {
            var handler = new EnvironmentVariableGetSecretHandler();
            var context = MakeContext(prefix: prefix, stripPrefix: true);
            var command = new GetSecretManagerCommand(null, "PASSWORD");

            // Act
            var result = await handler.InvokeBoxed(command, context, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var secretValue = result.Value.ShouldBeOfType<SecretValue>();
            secretValue.GetStringValue().ShouldBe("p@ss");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task InvokeBoxedGetSecretMissingVariableFailsLoud()
    {
        // Arrange
        var prefix = $"FDWTEST_{Guid.NewGuid():N}_";
        var handler = new EnvironmentVariableGetSecretHandler();
        var context = MakeContext(prefix: prefix, stripPrefix: true);
        var command = new GetSecretManagerCommand(null, "NEVER_SET");

        // Act
        var result = await handler.InvokeBoxed(command, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async System.Threading.Tasks.Task InvokeBoxedWrongExecutionContextTypeReturnsFailure()
    {
        // Arrange — Why: the base-class InvokeBoxed dispatches to Execute(TCommand, ISecretManagerExecutionContext,...)
        // regardless of the context's concrete type; the handler's own guard rejects any context that is
        // not an EnvironmentVariableExecutionContext.
        var handler = new EnvironmentVariableGetSecretHandler();
        var command = new GetSecretManagerCommand(null, "KEY");
        var foreignContext = new FakeExecutionContext();

        // Act
        var result = await handler.InvokeBoxed(command, foreignContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── EnvironmentVariableListSecretsHandler ──────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async System.Threading.Tasks.Task InvokeBoxedListSecretsFiltersByPrefixAndStripsKeys()
    {
        // Arrange
        var prefix = $"FDWTEST_{Guid.NewGuid():N}_";
        Environment.SetEnvironmentVariable(prefix + "ALPHA", "a", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(prefix + "BETA", "b", EnvironmentVariableTarget.Process);
        try
        {
            var handler = new EnvironmentVariableListSecretsHandler();
            var context = MakeContext(prefix: prefix, stripPrefix: true);
            var command = ListSecretsManagerCommand.All(null);

            // Act
            var result = await handler.InvokeBoxed(command, context, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var list = (IReadOnlyList<ISecretMetadata>)result.Value!;
            list.Count.ShouldBe(2);
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "ALPHA", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(prefix + "BETA", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async System.Threading.Tasks.Task InvokeBoxedWrongExecutionContextTypeReturnsFailureForListSecrets()
    {
        // Arrange
        var handler = new EnvironmentVariableListSecretsHandler();
        var command = ListSecretsManagerCommand.All(null);
        var foreignContext = new FakeExecutionContext();

        // Act
        var result = await handler.InvokeBoxed(command, foreignContext, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── EnvironmentVariableNotFoundHandler ─────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundHandlerValidateAlwaysFails()
    {
        // Arrange
        var handler = new EnvironmentVariableNotFoundHandler();
        var command = new GetSecretManagerCommand(null, "X");

        // Act
        var result = handler.Validate(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async System.Threading.Tasks.Task NotFoundHandlerInvokeBoxedReturnsFailureNamingTheCommandType()
    {
        // Arrange
        var handler = new EnvironmentVariableNotFoundHandler();
        var command = new GetSecretManagerCommand(null, "X");
        var context = MakeContext();

        // Act
        var result = await handler.InvokeBoxed(command, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("GetSecret");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundHandlerIdentityIsZeroAndNotFound()
    {
        // Arrange
        var handler = new EnvironmentVariableNotFoundHandler();

        // Assert
        handler.Id.ShouldBe(0);
        handler.Name.ShouldBe("NotFound");
        handler.CommandTypeClass.ShouldBe(typeof(void));
        handler.ResultType.ShouldBe(typeof(void));
    }

    // Why: a minimal ISecretManagerExecutionContext that is deliberately NOT an
    // EnvironmentVariableExecutionContext, used to exercise the handlers' "wrong context type" guard.
    private sealed class FakeExecutionContext : ISecretManagerExecutionContext
    {
        public Microsoft.Extensions.Logging.ILogger Logger { get; } = NullLogger.Instance;
        public Fdw.Configuration.IGenericConfiguration Configuration { get; } = new EnvironmentVariableConfiguration();
        public string ServiceId => "foreign";
    }
}
