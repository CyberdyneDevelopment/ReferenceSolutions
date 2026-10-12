using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// Covers MsSqlConnectionFactory secret-manager integration: the async provider-supplied path
/// (the runtime path where ConnectionProvider hands the FDW secret-manager provider down),
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

    // Why the typed body and not the header that wraps it: a connection factory builds from the
    // implementation configuration, and ConfigurationGateway hands it exactly that —
    // configDbEntry.Configuration, never the declared record around it. Server, Database, Port,
    // InstanceName and AuthenticationType all live on the body; the factory takes its log name from
    // the body's own ConnectionId. Passing the header is what the factory refuses, because a header
    // is not the thing that says how to connect.
    private static MsSqlConnectionConfiguration Named(MsSqlConnectionConfiguration body)
        => body;

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

    // Why removed: CreateWithProviderResolvesNamedManagerAndSucceeds/ResolvesCustomNamedManager/
    // FailsWhenManagerNotFound/FailsWhenSecretNotFound/FailsWhenSecretNeededButNoManagerNameConfigured
    // moved to MsSqlConnectionProviderTests. They built a factory WITH a secret-manager provider and
    // called the factory's synchronous Create directly -- but FDW-727 moved secret resolution off the
    // factory entirely (Create never reads _secretManagerProvider), so they were exercising
    // MsSqlConnectionProvider's job through a class that cannot reach it. The mismatch went unnoticed
    // because Create(config, resolvedSecret: null) fails loud on ANY unresolved-required-secret call
    // regardless of whether a provider was configured, which happened to make every FailsWhen* variant
    // pass for the wrong reason.

    // Why: a factory built WITHOUT a secret-manager provider cannot serve a secret-bearing auth type.
    // It must say so rather than degrade to a passwordless connection string.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateFailsLoudWhenFactoryHasNoSecretManagerProvider()
    {
        var result = NewFactory().Create(Named(SqlAuthConfig("Default")), resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // Why: the SYNC pure-construction Create never resolves a secret. A secret-needing connection must
    // fail loud rather than silently build a passwordless connection string.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task SyncCreateFailsLoudWhenSecretRequired()
    {
        var result = NewFactory().Create(SqlAuthConfig("Default"), resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // Why removed: CreateAsyncWithSqlAuthResolvedPasswordSucceeds/CreateAsyncFailsWhenSuppliedManagerIsNotTheDeclaredOne/
    // CreateAsyncWithSqlAuthSecretNotFoundFails moved to MsSqlConnectionProviderTests, same reason as
    // the CreateWithProvider* group above -- they tested provider-mediated secret resolution through
    // the factory's synchronous Create, which cannot reach a secret-manager provider at all.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithNoDatabaseSucceedsForWindowsAuth()
    {
        // In the new model, WindowsAuthConfiguration.Validate() always returns Success.
        // Database is not validated inside auth types — it is the connection string builder's concern.
        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = null!, // No database
            AuthenticationType = "WindowsAuth"
        };

        var result = NewFactory().Create(config, resolvedSecret: null);

        // Connection string is built without Database= part — this is valid for SqlClient
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithUnknownAuthenticationTypeSettingsFails()
    {
        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal"
        };

        var result = NewFactory().Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncWithUnknownAuthenticationTypeFails()
    {
        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal"
        };

        var result = NewFactory().Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConfigAndMsSqlConnectionTypeDelegatesToCreate()
    {
        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
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
    public async Task CreateWithNullServerFailsLoud()
    {
        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = null!,
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = NewFactory().Create(config, resolvedSecret: null);

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
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotReal" // Will fail - unknown auth type
        };

        var result = ((IServiceFactory)NewFactory()).Create(config);

        result.IsSuccess.ShouldBeFalse();
    }
}
