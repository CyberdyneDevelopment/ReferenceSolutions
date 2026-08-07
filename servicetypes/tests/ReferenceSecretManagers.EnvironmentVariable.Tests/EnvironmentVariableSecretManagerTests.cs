using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using ReferenceSecretManagers.EnvironmentVariable.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableSecretManager"/>: command dispatch, FDW_SECRET_* style
/// prefix mapping, missing-key fail-loud behavior, batch execution, and the IGenericService adapter
/// surface.
/// </summary>
/// <remarks>
/// Command-declaration typing matters here: <see cref="GetSecretManagerCommand"/> implements both
/// <c>ISecretManagerCommand</c> and <c>ISecretManagerCommand&lt;SecretValue&gt;</c>. Local variables
/// are explicitly typed as one or the other so each test exercises the intended overload
/// (<c>Execute(ISecretManagerCommand,...)</c> vs the generic <c>Execute&lt;TResult&gt;(...)</c>)
/// deterministically rather than relying on overload-resolution betterness rules.
/// </remarks>
public class EnvironmentVariableSecretManagerTests
{
    private static EnvironmentVariableConfiguration MakeConfig(
        string prefix = "",
        bool stripPrefix = true,
        bool caseSensitive = false) => new()
    {
        Id = Guid.NewGuid(),
        Prefix = prefix,
        StripPrefix = stripPrefix,
        CaseSensitive = caseSensitive,
        Separator = "__",
        Target = nameof(EnvironmentVariableTarget.Process),
    };

    private static string UniquePrefix() => $"FDWTEST_{Guid.NewGuid():N}_";

    // ── Construction ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorNullLoggerThrowsArgumentNullException()
    {
        // Arrange
        var config = MakeConfig();

        // Act
        var act = () => new EnvironmentVariableSecretManager(null!, config);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorNullConfigurationThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<EnvironmentVariableSecretManager>.Instance;

        // Act
        var act = () => new EnvironmentVariableSecretManager(logger, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorValidArgsSetsIdAndServiceType()
    {
        // Arrange
        var config = MakeConfig();

        // Act
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);

        // Assert
        manager.Id.ShouldBe(config.Id.ToString());
        manager.ServiceType.ShouldBe("EnvironmentVariable");
        manager.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DisposeSetsIsAvailableFalse()
    {
        // Arrange
        var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());

        // Act
        manager.Dispose();

        // Assert
        manager.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void DisposeCalledTwiceDoesNotThrow()
    {
        // Arrange
        var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());

        // Act
        var act = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };

        // Assert
        Should.NotThrow(act);
    }

    // ── Execute(ISecretManagerCommand) — non-generic overload ──────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteNullCommandReturnsFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        ISecretManagerCommand? command = null;

        // Act
        var result = await manager.Execute(command!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteUnknownCommandTypeReturnsFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        ISecretManagerCommand command = new FakeSecretManagerCommand("Bogus");

        // Act
        var result = await manager.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Unknown command type");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteGetSecretWithPrefixStrippedMapsToFdwSecretStyleEnvironmentVariable()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        var variableName = prefix + "DATABASE_PASSWORD";
        Environment.SetEnvironmentVariable(variableName, "s3cr3t", EnvironmentVariableTarget.Process);
        try
        {
            ISecretManagerCommand command = new GetSecretManagerCommand(null, "DATABASE_PASSWORD");

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var secretValue = result.Value.ShouldBeOfType<SecretValue>();
            secretValue.Key.ShouldBe("DATABASE_PASSWORD");
            secretValue.GetStringValue().ShouldBe("s3cr3t");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteGetSecretWithoutStripPrefixUsesRawVariableName()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: false);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        var rawKey = prefix + "API_KEY";
        Environment.SetEnvironmentVariable(rawKey, "raw-value", EnvironmentVariableTarget.Process);
        try
        {
            // Why: StripPrefix=false means GetEnvironmentVariableName returns secretKey unchanged —
            // the caller must already supply the full (prefixed) variable name as the SecretKey.
            ISecretManagerCommand command = new GetSecretManagerCommand(null, rawKey);

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var secretValue = result.Value.ShouldBeOfType<SecretValue>();
            secretValue.Key.ShouldBe(rawKey);
            secretValue.GetStringValue().ShouldBe("raw-value");
        }
        finally
        {
            Environment.SetEnvironmentVariable(rawKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteGetSecretMissingEnvironmentVariableFailsLoud()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        ISecretManagerCommand command = new GetSecretManagerCommand(null, "DOES_NOT_EXIST");

        // Act
        var result = await manager.Execute(command, TestContext.Current.CancellationToken);

        // Assert — fail loud, never a null/default fallback value.
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteListSecretsReturnsOnlyMatchingPrefixWithStrippedKeys()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "ONE", "1", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(prefix + "TWO", "2", EnvironmentVariableTarget.Process);
        try
        {
            ISecretManagerCommand command = ListSecretsManagerCommand.All(null);

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var list = (IReadOnlyList<ISecretMetadata>)result.Value!;
            list.Count.ShouldBe(2);
            var keys = new List<string> { list[0].Key, list[1].Key };
            keys.ShouldContain("ONE");
            keys.ShouldContain("TWO");
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "ONE", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(prefix + "TWO", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task ExecuteListSecretsRespectsMaxResultsParameter()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "ONE", "1", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(prefix + "TWO", "2", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(prefix + "THREE", "3", EnvironmentVariableTarget.Process);
        try
        {
            ISecretManagerCommand command = ListSecretsManagerCommand.WithPagination(null, maxResults: 1);

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var list = (IReadOnlyList<ISecretMetadata>)result.Value!;
            list.Count.ShouldBe(1);
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "ONE", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(prefix + "TWO", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(prefix + "THREE", null, EnvironmentVariableTarget.Process);
        }
    }

    // ── Execute<TResult>(ISecretManagerCommand<TResult>) — public generic ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteGenericNullCommandReturnsFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        ISecretManagerCommand<SecretValue>? command = null;

        // Act
        var result = await manager.Execute(command!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteGenericValidCommandReturnsTypedSuccess()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "TOKEN", "abc123", EnvironmentVariableTarget.Process);
        try
        {
            ISecretManagerCommand<SecretValue> command = new GetSecretManagerCommand(null, "TOKEN");

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value!.Key.ShouldBe("TOKEN");
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "TOKEN", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteGenericMissingEnvironmentVariablePropagatesFailure()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        ISecretManagerCommand<SecretValue> command = new GetSecretManagerCommand(null, "MISSING");

        // Act
        var result = await manager.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── ExecuteBatch ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteBatchNullCommandsThrowsArgumentNullException()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());

        // Act
        var act = () => manager.ExecuteBatch(null!, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteBatchEmptyCommandsThrowsArgumentException()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());

        // Act
        var act = () => manager.ExecuteBatch(Array.Empty<ISecretManagerCommand>(), TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteBatchAllSucceedReturnsSuccess()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "A", "1", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(prefix + "B", "2", EnvironmentVariableTarget.Process);
        try
        {
            var commands = new List<ISecretManagerCommand>
            {
                new GetSecretManagerCommand(null, "A"),
                new GetSecretManagerCommand(null, "B"),
            };

            // Act
            var result = await manager.ExecuteBatch(commands, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "A", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(prefix + "B", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecuteBatchPartialFailureReturnsAggregateFailure()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "PRESENT", "1", EnvironmentVariableTarget.Process);
        try
        {
            var commands = new List<ISecretManagerCommand>
            {
                new GetSecretManagerCommand(null, "PRESENT"),
                new GetSecretManagerCommand(null, "MISSING"),
            };

            // Act
            var result = await manager.ExecuteBatch(commands, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.CurrentMessage!.ShouldContain("1 of 2");
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "PRESENT", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteBatchCommandCastMismatchIsCaughtAndConvertedToFailure()
    {
        // Arrange — Why: FakeSecretManagerCommand declares CommandType="GetSecret" but is not a real
        // GetSecretManagerCommand instance, so the handler's internal (TCommand)command cast throws
        // InvalidCastException. ExecuteBatch awaits ExecuteCommandInternal inside its own try/catch,
        // so the exception must be caught here and converted into a structured Failure — never an
        // unhandled exception escaping a batch call.
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var commands = new List<ISecretManagerCommand> { new FakeSecretManagerCommand("GetSecret", "X") };

        // Act
        var result = await manager.ExecuteBatch(commands, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── ValidateCommand ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ValidateCommandNullCommandThrowsArgumentNullException()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());

        // Act
        var act = () => manager.ValidateCommand(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ValidateCommandEmptyCommandTypeReturnsFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeSecretManagerCommand(string.Empty);

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Command type is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ValidateCommandSetSecretReturnsNotSupportedFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeSecretManagerCommand("SetSecret");

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("not supported");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ValidateCommandDeleteSecretReturnsNotSupportedFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeSecretManagerCommand("DeleteSecret");

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("not supported");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ValidateCommandBogusCommandTypeReturnsUnknownCommandTypeFailure()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeSecretManagerCommand("Bogus");

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Unknown command type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ValidateCommandGetSecretWithSecretKeyDelegatesToHandlerAndSucceeds()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new GetSecretManagerCommand(null, "SOME_KEY");

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ValidateCommandListSecretsSucceeds()
    {
        // Arrange
        using var manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = ListSecretsManagerCommand.All(null);

        // Act
        var result = manager.ValidateCommand(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ── IGenericService explicit adapter surface ──────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task IGenericServiceExecuteGenericNonSecretCommandReturnsFailure()
    {
        // Arrange
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeGenericCommand();

        // Act
        var result = await manager.Execute<SecretValue>(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task IGenericServiceExecuteGenericExactTypeMatchDelegatesToTypedExecute()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "MATCH", "v", EnvironmentVariableTarget.Process);
        try
        {
            var command = new GetSecretManagerCommand(null, "MATCH");

            // Act
            var result = await manager.Execute<SecretValue>(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value!.Key.ShouldBe("MATCH");
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "MATCH", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task IGenericServiceExecuteGenericTypeMismatchReturnsFailure()
    {
        // Arrange — Why: exercises ExecuteAndCast<TOut>'s "Result is not of type" branch: the real
        // command succeeds and produces a SecretValue, but the caller requested TOut=int, so the
        // (secretCommand is ISecretManagerCommand<TOut>) check fails and dispatch falls through to
        // the boxed-execute-and-cast path.
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "MISMATCH", "v", EnvironmentVariableTarget.Process);
        try
        {
            var command = new GetSecretManagerCommand(null, "MISMATCH");

            // Act
            var result = await manager.Execute<int>(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.CurrentMessage!.ShouldContain("not of type");
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "MISMATCH", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task IGenericServiceExecuteNonGenericNonSecretCommandReturnsFailure()
    {
        // Arrange
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, MakeConfig());
        var command = new FakeGenericCommand();

        // Act
        var result = await manager.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task IGenericServiceExecuteNonGenericSuccessfulCommandReturnsSuccess()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        Environment.SetEnvironmentVariable(prefix + "OK", "v", EnvironmentVariableTarget.Process);
        try
        {
            var command = new GetSecretManagerCommand(null, "OK");

            // Act
            var result = await manager.Execute(command, TestContext.Current.CancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "OK", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task IGenericServiceExecuteNonGenericFailingCommandPropagatesFailure()
    {
        // Arrange
        var prefix = UniquePrefix();
        var config = MakeConfig(prefix: prefix, stripPrefix: true);
        IGenericService manager = new EnvironmentVariableSecretManager(NullLogger<EnvironmentVariableSecretManager>.Instance, config);
        var command = new GetSecretManagerCommand(null, "NOPE");

        // Act
        var result = await manager.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
