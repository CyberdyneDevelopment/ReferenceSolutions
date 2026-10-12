using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// Covers <see cref="MsSqlConnectionProvider"/> -- the async layer that resolves the secret a
/// connection's authentication type declares before handing the resolved value to the (synchronous)
/// <see cref="MsSqlConnectionFactory"/>.
/// </summary>
/// <remarks>
/// Why this file and not <c>MsSqlConnectionFactoryAdditionalTests</c>: FDW-727 moved secret
/// resolution off the factory and onto this provider so every connection factory's Create could stay
/// synchronous (see <see cref="IMsSqlConnectionFactory.Create(MsSqlConnectionConfiguration, string?)"/>'s
/// own remarks). The factory tests that used to exercise "supply a secret-manager provider and get a
/// working connection back" were never updated for that move -- they kept constructing the factory
/// with a provider and calling its synchronous Create directly, which cannot reach the provider at
/// all. This file exercises the real class that does that work.
/// </remarks>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionProviderTests
{
    private readonly Mock<IMsSqlConnectionFactory> _factoryMock = new();
    private readonly Mock<ILogger<MsSqlConnectionProvider>> _providerLogger = new();

    private static MsSqlConnectionConfiguration SqlAuthConfig(string? secretManagerName = null)
        => new()
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "SqlAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = "sa",
                ["SecretKeyName"] = "sql-password",
                ["SecretManagerName"] = secretManagerName,
            },
        };

    private static Mock<ISecretManagerProvider> ProviderReturning(string managerName, ISecretManager manager)
    {
        var mockProvider = new Mock<ISecretManagerProvider>();
        mockProvider.Setup(p => p.Get(managerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(manager));
        return mockProvider;
    }

    private static Mock<ISecretManager> SecretManagerReturning(SecretValue value, string managerName = "Default")
    {
        var mock = new Mock<ISecretManager>();
        mock.SetupGet(sm => sm.Name).Returns(managerName);
        mock.Setup(sm => sm.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Success(value));
        return mock;
    }

    private MsSqlConnectionProvider NewProvider(ISecretManagerProvider secretManagerProvider)
        => new(_factoryMock.Object, secretManagerProvider, _providerLogger.Object);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateResolvesNamedManagerAndSucceeds()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "resolved-pass-123"));
        var provider = ProviderReturning("Default", manager.Object);
        _factoryMock
            .Setup(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), "resolved-pass-123"))
            .Returns(GenericResult<IGenericConnection>.Success(Mock.Of<IGenericConnection>()));

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        provider.Verify(p => p.Get("Default", It.IsAny<CancellationToken>()), Times.Once);
        _factoryMock.Verify(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), "resolved-pass-123"), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateResolvesCustomNamedManager()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "custom-pass"), "AzureKeyVault");
        var provider = ProviderReturning("AzureKeyVault", manager.Object);
        _factoryMock
            .Setup(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), "custom-pass"))
            .Returns(GenericResult<IGenericConnection>.Success(Mock.Of<IGenericConnection>()));

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig("AzureKeyVault"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Get("AzureKeyVault", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateFailsWhenManagerNotFound()
    {
        var provider = new Mock<ISecretManagerProvider>();
        provider.Setup(p => p.Get("Default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Failure(new GenericMessage("Secret manager not found")));

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _factoryMock.Verify(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateFailsWhenSecretNotFound()
    {
        var manager = new Mock<ISecretManager>();
        manager.SetupGet(sm => sm.Name).Returns("Default");
        manager.Setup(sm => sm.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Failure(new GenericMessage("Secret not found")));
        var provider = ProviderReturning("Default", manager.Object);

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _factoryMock.Verify(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), It.IsAny<string>()), Times.Never);
    }

    // Why: NO "Default" fallback. A secret is required (SqlAuth declares SecretKeyName) but the auth
    // config names no manager => fail loud, not a silent default.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateFailsWhenSecretNeededButNoManagerNameConfigured()
    {
        var provider = new Mock<ISecretManagerProvider>();

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig(secretManagerName: null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Why: the provider is asked for a manager by name; if what comes back carries a different name,
    // reading its secret would be a silent credential substitution from a store this connection never
    // declared.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateFailsWhenResolvedManagerIsNotTheDeclaredOne()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "async-pass-456"), "SomeOtherStore");
        var provider = ProviderReturning("Default", manager.Object);

        var result = await NewProvider(provider.Object).Create(SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _factoryMock.Verify(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreatePassesThroughForAuthenticationTypeThatNeedsNoSecret()
    {
        var provider = new Mock<ISecretManagerProvider>();
        _factoryMock
            .Setup(f => f.Create(It.IsAny<MsSqlConnectionConfiguration>(), (string?)null))
            .Returns(GenericResult<IGenericConnection>.Success(Mock.Of<IGenericConnection>()));

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth",
        };

        var result = await NewProvider(provider.Object).Create(config, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
