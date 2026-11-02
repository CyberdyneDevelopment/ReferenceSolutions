using Fdw.Results;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.DataVault.Abstractions;

namespace FdwReferenceCredentials.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlCredentialService"/>: the vault-resolution guard chain (missing typed body,
/// blank vault name, vault-resolve failure, wrong vault type) and the four semantic verbs forwarding
/// to the resolved <see cref="ICredentialVault"/>, including lazy-cache-once behavior.
/// </summary>
public class SqlCredentialServiceTests
{
    private static SqlCredentialServiceConfiguration MakeTypedBody(string? vaultName = "default-vault")
        => new() { Id = Guid.NewGuid(), CredentialVaultName = vaultName };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorNullConfigurationThrowsArgumentNullException()
    {
        // Arrange
        var vaultProvider = new Mock<IDataVaultProvider>().Object;

        // Act
        var act = () => new SqlCredentialService(null!, vaultProvider);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorNullVaultProviderThrowsArgumentNullException()
    {
        // Arrange
        var configuration = MakeTypedBody();

        // Act
        var act = () => new SqlCredentialService(configuration, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ValidateBlankVaultNameReturnsFailure()
    {
        // Arrange
        var configuration = MakeTypedBody(vaultName: "   ");
        var vaultProvider = new Mock<IDataVaultProvider>();
        using var service = new SqlCredentialService(configuration, vaultProvider.Object);

        // Act
        var result = await service.Create(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ChangeVaultResolveFailureReturnsFailure()
    {
        // Arrange
        var configuration = MakeTypedBody();
        var vaultProvider = new Mock<IDataVaultProvider>();
        vaultProvider
            .Setup(p => p.Get(It.IsAny<DataVaultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataVault>.Failure(new Fdw.Messages.ErrorMessage("vault down")));
        using var service = new SqlCredentialService(configuration, vaultProvider.Object);

        // Act
        var result = await service.Change(Guid.NewGuid(), new byte[] { 1 }, new byte[] { 2 }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DisableResolvedVaultWrongTypeReturnsFailure()
    {
        // Arrange — Why: the resolved IDataVault is not an ICredentialVault (misconfigured vault type).
        var configuration = MakeTypedBody();
        var notACredentialVault = new Mock<IDataVault>().Object;
        var vaultProvider = new Mock<IDataVaultProvider>();
        vaultProvider
            .Setup(p => p.Get(It.IsAny<DataVaultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataVault>.Success(notACredentialVault));
        using var service = new SqlCredentialService(configuration, vaultProvider.Object);

        // Act
        var result = await service.Disable(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ValidateResolvedVaultForwardsCallAndReturnsOutcome()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var derivedHash = new byte[] { 9, 9 };
        var outcomeMock = new Mock<ICredentialOutcome>();
        var vaultMock = new Mock<ICredentialVault>();
        vaultMock
            .Setup(v => v.Validate(userId, derivedHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(outcomeMock.Object));
        var vaultProvider = new Mock<IDataVaultProvider>();
        vaultProvider
            .Setup(p => p.Get(It.Is<DataVaultRequest>(r => r.Name == "default-vault"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataVault>.Success((IDataVault)vaultMock.Object));
        var configuration = MakeTypedBody();
        using var service = new SqlCredentialService(configuration, vaultProvider.Object);

        // Act
        var result = await service.Validate(userId, derivedHash, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(outcomeMock.Object);
        vaultMock.Verify(v => v.Validate(userId, derivedHash, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task VaultResolvedOnceIsCachedAcrossMultipleCalls()
    {
        // Arrange — Why: ResolveVault caches _vault after first success; a second call must not
        // re-invoke the vault provider.
        var vaultMock = new Mock<ICredentialVault>();
        vaultMock.Setup(v => v.Create(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var vaultProvider = new Mock<IDataVaultProvider>();
        vaultProvider
            .Setup(p => p.Get(It.IsAny<DataVaultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataVault>.Success((IDataVault)vaultMock.Object));
        var configuration = MakeTypedBody();
        using var service = new SqlCredentialService(configuration, vaultProvider.Object);

        // Act
        await service.Create(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);
        await service.Create(Guid.NewGuid(), new byte[] { 2 }, TestContext.Current.CancellationToken);

        // Assert
        vaultProvider.Verify(p => p.Get(It.IsAny<DataVaultRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceIdReflectsConfigurationId()
    {
        // Arrange
        var configuration = MakeTypedBody();
        Fdw.Abstractions.IGenericService service = new SqlCredentialService(configuration, new Mock<IDataVaultProvider>().Object);

        // Assert
        service.Id.ShouldBe(configuration.Id.ToString());
        service.ServiceType.ShouldBe("CredentialService");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IsAvailableTrueWhenConfigurationHasTypedBody()
    {
        // Arrange
        var configuration = MakeTypedBody();
        Fdw.Abstractions.IGenericService service = new SqlCredentialService(configuration, new Mock<IDataVaultProvider>().Object);

        // Assert
        service.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task IGenericServiceGenericExecuteRejectsCommandSurface()
    {
        // Arrange — Why: a credential service exposes ONLY its semantic verbs; a generic command is
        // rejected fail-loud (mirrors the vault's own closed access policy).
        var configuration = MakeTypedBody();
        Fdw.Abstractions.IGenericService service = new SqlCredentialService(configuration, new Mock<IDataVaultProvider>().Object);

        // Act
        var result = await service.Execute<object>(new Mock<Fdw.Abstractions.IGenericCommand>().Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task IGenericServiceNonGenericExecuteRejectsCommandSurface()
    {
        // Arrange
        var configuration = MakeTypedBody();
        Fdw.Abstractions.IGenericService service = new SqlCredentialService(configuration, new Mock<IDataVaultProvider>().Object);

        // Act
        var result = await service.Execute(new Mock<Fdw.Abstractions.IGenericCommand>().Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void DisposeDisposesVaultLockWithoutThrowing()
    {
        // Arrange
        var configuration = MakeTypedBody();
        var service = new SqlCredentialService(configuration, new Mock<IDataVaultProvider>().Object);

        // Act
        var act = () => service.Dispose();

        // Assert
        Should.NotThrow(act);
    }
}
