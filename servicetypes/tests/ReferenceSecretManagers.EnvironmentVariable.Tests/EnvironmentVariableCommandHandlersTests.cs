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
        // Why not "NotFound": the sentinel is the generator's, and it names it "_Empty". The name it
        // carries is incidental — callers identify it by reference, never by name, because a handler
        // could legitimately be called anything.
        handler.Name.ShouldBe("_Empty");
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
    public void AllReturnsExactlyTheRegisteredHandlers()
    {
        // Act
        var all = EnvironmentVariableCommandHandlers.All();

        // Assert — GetSecret and ListSecrets. It was three while a hand-written NotFound option was
        // registered as a member; the sentinel now sits outside the set, which is the point of it.
        all.Count.ShouldBe(2);
        all.ShouldNotContain(h => ReferenceEquals(h, EnvironmentVariableCommandHandlers.NotFound));
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

    // ── the collection's NotFound sentinel ────────────────────────────────

    /// <summary>
    /// A miss returns the sentinel rather than null, which is the contract every caller relies on:
    /// the managers test the result with ReferenceEquals against it.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsTheSentinelForAnUnknownCommand()
    {
        var handler = EnvironmentVariableCommandHandlers.ByName("NoSuchCommand");

        ReferenceEquals(handler, EnvironmentVariableCommandHandlers.NotFound).ShouldBeTrue();
    }

    /// <summary>
    /// The sentinel is not a member of the collection. It exists to be returned when nothing matched,
    /// so anything enumerating the handlers must not encounter it — a hand-registered stand-in would
    /// appear here, and every caller iterating All() would have to know to skip it.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheSentinelIsNotOneOfTheHandlers()
    {
        var all = EnvironmentVariableCommandHandlers.All();

        all.ShouldNotContain(h => ReferenceEquals(h, EnvironmentVariableCommandHandlers.NotFound));
        all.ShouldNotContain(h => h.Name == "NotFound");
        all.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Every member is a real handler: it names the command it processes and the type of that command.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EveryMemberIsARealHandler()
    {
        foreach (var handler in EnvironmentVariableCommandHandlers.All())
        {
            handler.Name.ShouldNotBeNullOrWhiteSpace();
            handler.CommandTypeClass.ShouldNotBe(typeof(void), $"{handler.Name} declares no command type");
        }
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
