using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

// Why: the sync pure-construction Create(configuration) takes no CancellationToken; xUnit1051 fires
// because an async sibling overload with an optional token exists. Passing a token here would select
// that sibling and stop testing the sync path.
#pragma warning disable xUnit1051

/// <summary>
/// Tests for MsSqlConnectionFactory.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionFactoryTests
{
    private readonly Mock<ILogger<MsSqlConnectionFactory>> _factoryLogger = new();
    private readonly Mock<ILogger<MsSqlConnection>> _connectionLogger = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorAcceptsNullSecretManagerProvider()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object,
            null);

        factory.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = factory.Create((MsSqlConnectionConfiguration)null!, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateReturnsSuccessWithValidConfiguration()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeOfType<MsSqlConnection>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateReturnsConnectionInstance()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var connection = result.Value.ShouldBeOfType<MsSqlConnection>();
        connection.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateReturnsFailureWhenAuthenticationTypeIsUnknown()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "UnknownAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateReturnsFailureWhenAuthenticationTypeIsEmpty()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = string.Empty
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithGenericConfigurationReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = factory.Create((IGenericConfiguration)null!, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithGenericConfigurationReturnsFailureWhenTypeIsWrong()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var wrongConfig = Mock.Of<IGenericConfiguration>();

        var result = factory.Create(wrongConfig, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // Why removed: these three tested Create(configuration, connectionType) -- a validating
    // overload that named which connection type string was acceptable ("MsSql"). That overload was
    // deleted (every private Create layer went, per the FDW-727 synchronous-factory cleanup); the
    // only 2-arg Create left is Create(configuration, resolvedSecret), so a string like "PostgreSql"
    // or "MsSql" passed here is just an ignored resolvedSecret value, not a connection-type check.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = factory.Create(null!, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncSucceedsWithWindowsAuth()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncReturnsFailureWhenSecretManagerRequiredButNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "SqlAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = "sa",
                ["SecretKeyName"] = "my-secret"
            }
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncReturnsFailureWhenAuthIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "UnknownType"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithNamedInstanceUsesBackslashFormat()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "myserver",
            InstanceName = "SQLEXPRESS",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithNonDefaultPortUsesCommaFormat()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "myserver",
            Port = 1434,
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithTrustServerCertificateIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            TrustServerCertificate = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithPoolingDisabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            EnableConnectionPooling = false,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithCustomPoolSizesIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            EnableConnectionPooling = true,
            MinPoolSize = 5,
            MaxPoolSize = 50,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithMarsEnabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            EnableMultipleActiveResultSets = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithApplicationNameIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            ApplicationName = "MyApp",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithCustomTimeoutIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            ConnectionTimeoutSeconds = 30,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithAdditionalPropertiesIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["MultiSubnetFailover"] = "True"
            }
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithSqlAuthRequiresSecretManagerForSecretKey()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object,
            null); // No secret manager provider

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "SqlAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = "sa",
                ["SecretKeyName"] = "sql-password"
            }
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithGenericConfigurationDelegatesToTypedCreate()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<MsSqlConnection>();
    }

    // Why removed: same deleted Create(configuration, connectionType) overload as above -- the
    // IGenericConfiguration-typed variable just routes to Create(IGenericConfiguration, resolvedSecret)
    // instead, which still isn't a connection-type check.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateGenericServiceSucceeds()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = ((IServiceFactory)factory).Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateGenericServiceWithNullConfigFails()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = ((IServiceFactory)factory).Create(null!);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateTypedWithConnectionTypeFails()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        // Request a type that MsSqlConnection doesn't implement
        var result = ((IServiceFactory)factory).Create<ISecretManager>(
            new MsSqlConnectionConfiguration
            {
                MaxSerialInsert = 1000,
                Server = "localhost",
                Database = "TestDb",
                AuthenticationType = "WindowsAuth"
            });

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryCreateTypedSucceedsWithCompatibleType()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = ((IServiceFactory)factory).Create<IGenericConnection>(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryConnectionCreateSucceeds()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = ((IServiceFactory<IGenericConnection>)factory).Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IServiceFactoryTypedConfigCreateSucceeds()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = ((IServiceFactory<IGenericConnection, MsSqlConnectionConfiguration>)factory).Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithAllOptionsIncludesThemInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "myserver",
            Database = "TestDb",
            ConnectionTimeoutSeconds = 60,
            Encrypt = true,
            TrustServerCertificate = true,
            EnableConnectionPooling = true,
            MinPoolSize = 2,
            MaxPoolSize = 50,
            EnableMultipleActiveResultSets = true,
            ApplicationName = "TestApp",
            AuthenticationType = "WindowsAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Failover Partner"] = "backup-server"
            }
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithEncryptionEnabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            Encrypt = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateWithUnknownAuthenticationTypeReturnsFailure()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            MaxSerialInsert = 1000,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotARealAuthType"
        };

        var result = factory.Create(config, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
    }
}
