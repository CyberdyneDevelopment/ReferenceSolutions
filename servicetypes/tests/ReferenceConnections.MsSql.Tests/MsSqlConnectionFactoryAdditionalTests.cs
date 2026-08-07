using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Covers MsSqlConnectionFactory secret-manager integration: the async provider-supplied path
/// (the runtime path where DefaultConnectionProvider hands the FDW secret-manager provider down),
/// the bootstrap ISecretManager path, and the fail-loud behaviour of the sync pure-construction path.
/// </summary>
/// <remarks>
/// The factory NO LONGER resolves secret managers itself (the old IServiceScopeFactory + sync-over-async
/// ResolvePasswordSync is gone). The manager NAME comes from the connection's own authentication config
/// (the typed body's Properties "SecretManagerName"); there is no "Default" fallback.
/// </remarks>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionFactoryAdditionalTests
{
    private readonly Mock<ILogger<MsSqlConnectionFactory>> _factoryLogger = new();
    private readonly Mock<ILogger<MsSqlConnection>> _connectionLogger = new();

    private MsSqlConnectionFactory NewFactory()
        => new(_factoryLogger.Object, _connectionLogger.Object);

    // Why: the secret-manager provider is now a CONSTRUCTOR dependency of the factory, not a Create
    // argument — the connection type registers it, exactly as HttpConnectionType registers its
    // IHttpClientFactory. These tests build the factory the way DI does.
    private MsSqlConnectionFactory NewFactory(ISecretManagerProvider secretManagerProvider)
        => new(_factoryLogger.Object, _connectionLogger.Object, secretManagerProvider);

    // Why: a secret-manager provider mock keyed by manager name. The factory reads the name from the
    // connection's auth config and calls Get(name).
    private static Mock<ISecretManagerProvider> ProviderReturning(string managerName, ISecretManager manager)
    {
        var mockProvider = new Mock<ISecretManagerProvider>();
        mockProvider.Setup(p => p.Get(managerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(manager));
        return mockProvider;
    }

    // Why: the factory now checks that the manager it was handed IS the one the connection declares,
    // so every mock manager has to answer to its configured name.
    private static Mock<ISecretManager> SecretManagerReturning(SecretValue value, string managerName = "Default")
    {
        var mock = new Mock<ISecretManager>();
        mock.SetupGet(sm => sm.Name).Returns(managerName);
        mock.Setup(sm => sm.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Success(value));
        return mock;
    }

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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithProviderResolvesNamedManagerAndSucceeds()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "resolved-pass-123"));
        var provider = ProviderReturning("Default", manager.Object);

        var result = await NewFactory(provider.Object).Create(
            SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        provider.Verify(p => p.Get("Default", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithProviderResolvesCustomNamedManager()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "custom-pass"), "AzureKeyVault");
        var provider = ProviderReturning("AzureKeyVault", manager.Object);

        var result = await NewFactory(provider.Object).Create(
            SqlAuthConfig("AzureKeyVault"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        provider.Verify(p => p.Get("AzureKeyVault", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithProviderFailsWhenManagerNotFound()
    {
        var provider = new Mock<ISecretManagerProvider>();
        provider.Setup(p => p.Get("Default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Failure(new GenericMessage("Secret manager not found")));

        var result = await NewFactory(provider.Object).Create(
            SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithProviderFailsWhenSecretNotFound()
    {
        var manager = new Mock<ISecretManager>();
        manager.SetupGet(sm => sm.Name).Returns("Default");
        manager.Setup(sm => sm.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Failure(new GenericMessage("Secret not found")));
        var provider = ProviderReturning("Default", manager.Object);

        var result = await NewFactory(provider.Object).Create(
            SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // Why: NO "Default" fallback. A secret is required (SqlAuth declares SecretKeyName) but the auth
    // config names no manager => fail loud, not a silent default.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithProviderFailsWhenSecretNeededButNoManagerNameConfigured()
    {
        var provider = new Mock<ISecretManagerProvider>();

        var result = await NewFactory(provider.Object).Create(
            SqlAuthConfig(secretManagerName: null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Why: a factory built WITHOUT a secret-manager provider cannot serve a secret-bearing auth type.
    // It must say so rather than degrade to a passwordless connection string.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateFailsLoudWhenFactoryHasNoSecretManagerProvider()
    {
        var result = await NewFactory().Create(
            SqlAuthConfig("Default"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // Why: the SYNC pure-construction Create never resolves a secret. A secret-needing connection must
    // fail loud rather than silently build a passwordless connection string.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SyncCreateFailsLoudWhenSecretRequired()
    {
        var result = NewFactory().Create(SqlAuthConfig("Default"));

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncWithSqlAuthResolvedPasswordSucceeds()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "async-pass-456"));

        var result = await NewFactory().Create(
            SqlAuthConfig("Default"),
            manager.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    // Why: the bootstrap caller supplies a manager directly, but it still has to BE the store the
    // connection declares — reading a password out of an undeclared store is a silent credential
    // substitution.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateAsyncFailsWhenSuppliedManagerIsNotTheDeclaredOne()
    {
        var manager = SecretManagerReturning(new SecretValue("sql-password", "async-pass-456"), "SomeOtherStore");

        var result = await NewFactory().Create(
            SqlAuthConfig("Default"),
            manager.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncWithSqlAuthSecretNotFoundFails()
    {
        var manager = new Mock<ISecretManager>();
        manager.SetupGet(sm => sm.Name).Returns("Default");
        manager.Setup(sm => sm.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Failure(new GenericMessage("Secret not found")));

        var result = await NewFactory().Create(
            SqlAuthConfig("Default"),
            manager.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithNoDatabaseSucceedsForWindowsAuth()
    {
        // In the new model, WindowsAuthConfiguration.Validate() always returns Success.
        // Database is not validated inside auth types — it is the connection string builder's concern.
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = null!, // No database
            AuthenticationType = "WindowsAuth"
        };

        var result = NewFactory().Create(config);

        // Connection string is built without Database= part — this is valid for SqlClient
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithUnknownAuthenticationTypeSettingsFails()
    {
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal"
        };

        var result = NewFactory().Create(config);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncWithUnknownAuthenticationTypeFails()
    {
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal"
        };

        var result = await NewFactory().Create(
            config,
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConfigAndMsSqlConnectionTypeDelegatesToCreate()
    {
        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = NewFactory().Create(config, "MsSql");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    // Why: this asserted SUCCESS for a null Server, which was never true — SqlConnectionStringBuilder
    // rejects a null DataSource, so the factory has always returned a structured failure here. A
    // missing server is missing configuration; failing loud is the correct behaviour, and the
    // assertion now matches it.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithNullServerFailsLoud()
    {
        var config = new MsSqlConnectionConfiguration
        {
            Server = null!,
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = NewFactory().Create(config);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateTypedFailsWhenUnderlyingCreateFails()
    {
        // null config causes Create to fail
        var result = ((IServiceFactory)NewFactory()).Create<IGenericConnection>(null!);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateGenericServiceFailsWhenUnderlyingFails()
    {
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal" // Will fail - unknown auth type
        };

        var result = ((IServiceFactory)NewFactory()).Create(config);

        result.IsSuccess.ShouldBeFalse();
    }
}
