using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Credentials.Abstractions;
using ReferenceCredentials.Sql;
using Fdw.Services.Connections.Abstractions;
using Moq;

using ReferenceConnections.MsSql.DataVault;

namespace ReferenceConnections.MsSql.DataVault.Tests;

/// <summary>
/// Tests for <see cref="CredentialVault"/>: the guard clauses that run before any ADO call (empty
/// derived hash / raw token / raw key) and the connection-type-mismatch fail-loud path that
/// <see cref="Fdw.Services.DataVault.MsSql.MsSqlDataVaultBase"/> enforces for every verb when the
/// resolved connection is not a SQL Server connection. A real MsSqlConnection cannot be exercised
/// without a live SQL Server, so the ADO success paths are out of unit-test reach here — see the
/// final report for this documented gap.
/// </summary>
public class CredentialVaultTests
{
    private static CredentialVault MakeVault(IDataConnection? connection = null)
        => new("test-vault", connection ?? new Mock<IDataConnection>().Object, new byte[32]);

    // ── Name / identity ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void NameReturnsConstructedVaultName()
    {
        // Act
        var vault = MakeVault();

        // Assert
        vault.Name.ShouldBe("test-vault");
    }

    // ── Password verbs — guard clauses ─────────────────────────────────────

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [MemberData(nameof(EmptyByteArrays))]
    public async Task ValidateEmptyDerivedHashReturnsFailureWithoutTouchingConnection(byte[]? derivedHash)
    {
        // Arrange
        var connection = new Mock<IDataConnection>();
        var vault = MakeVault(connection.Object);

        // Act
        var result = await vault.Validate(Guid.NewGuid(), derivedHash!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [MemberData(nameof(EmptyByteArrays))]
    public async Task CreateEmptyDerivedHashReturnsFailure(byte[]? derivedHash)
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Create(Guid.NewGuid(), derivedHash!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ChangeEmptyOldDerivedHashReturnsFailure()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Change(Guid.NewGuid(), Array.Empty<byte>(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ChangeEmptyNewDerivedHashReturnsFailure()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Change(Guid.NewGuid(), new byte[] { 1 }, Array.Empty<byte>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── Connection-type mismatch (every verb funnels through Query/NonQuery) ─

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ValidateWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange — Why: MsSqlDataVaultBase.Query rejects a resolved connection that is not an
        // MsSqlConnection before ever opening ADO — the connection type is invisible above the
        // vault, but internally still enforced.
        var vault = MakeVault();

        // Act
        var result = await vault.Validate(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Create(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ChangeWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Change(Guid.NewGuid(), new byte[] { 1 }, new byte[] { 2 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task DisableWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var result = await vault.Disable(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── PAT verbs ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PatCreateEmptyRawTokenReturnsFailureWithoutTouchingConnection()
    {
        // Arrange
        var connection = new Mock<IDataConnection>();
        IPatVault vault = MakeVault(connection.Object);

        // Act
        var result = await vault.Create(Guid.NewGuid(), "   ", "label", null, 5, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        connection.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PatCreateWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.Create(Guid.NewGuid(), "raw-token", "label", null, 5, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PatValidateEmptyRawTokenReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.Validate(string.Empty, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PatValidateWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.Validate("raw-token", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PatListWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.List(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PatRevokeWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.Revoke(Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task PatRevokeAllWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IPatVault vault = MakeVault();

        // Act
        var result = await vault.RevokeAll(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── Agent-key verbs ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AgentKeyCreateEmptyRawKeyReturnsFailureWithoutTouchingConnection()
    {
        // Arrange
        var connection = new Mock<IDataConnection>();
        IAgentKeyVault vault = MakeVault(connection.Object);

        // Act
        var result = await vault.Create(Guid.NewGuid(), "user", string.Empty, "label", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        connection.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AgentKeyCreateWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IAgentKeyVault vault = MakeVault();

        // Act
        var result = await vault.Create(Guid.NewGuid(), "user", "raw-key", "label", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AgentKeyListWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IAgentKeyVault vault = MakeVault();

        // Act
        var result = await vault.List(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AgentKeyDeleteWithNonMsSqlConnectionReturnsFailure()
    {
        // Arrange
        IAgentKeyVault vault = MakeVault();

        // Act
        var result = await vault.Delete(Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ── Disposal ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void DisposeClearsPepperAndDoesNotThrow()
    {
        // Arrange
        var vault = MakeVault();

        // Act
        var act = () => vault.Dispose();

        // Assert
        Should.NotThrow(act);
    }

    public static TheoryData<byte[]?> EmptyByteArrays() => new()
    {
        { (byte[]?)null },
        { Array.Empty<byte>() },
    };
}
