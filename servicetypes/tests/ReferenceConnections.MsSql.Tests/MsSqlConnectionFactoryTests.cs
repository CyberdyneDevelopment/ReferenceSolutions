using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace ReferenceConnections.MsSql.Tests;

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
    public void CreateReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = factory.Create((MsSqlConnectionConfiguration)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateReturnsSuccessWithValidConfiguration()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeOfType<MsSqlConnection>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateReturnsConnectionInstance()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var connection = result.Value.ShouldBeOfType<MsSqlConnection>();
        connection.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateReturnsFailureWhenAuthenticationTypeIsUnknown()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "UnknownAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateReturnsFailureWhenAuthenticationTypeIsEmpty()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = string.Empty
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConfigurationReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = factory.Create((IGenericConfiguration)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConfigurationReturnsFailureWhenTypeIsWrong()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var wrongConfig = Mock.Of<IGenericConfiguration>();

        var result = factory.Create(wrongConfig);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithConnectionTypeReturnsFailureForUnsupportedType()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, "PostgreSql");

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithConnectionTypeSucceedsForMsSqlType()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, "MsSql");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithConnectionTypeIsCaseInsensitive()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, "mssql");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateAsyncReturnsFailureWhenConfigurationIsNull()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var result = await factory.Create(
            null!,
            null,
            TestContext.Current.CancellationToken);

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
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = await factory.Create(
            config,
            null,
            TestContext.Current.CancellationToken);

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
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "SqlAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = "sa",
                ["SecretKeyName"] = "my-secret"
            }
        };

        var result = await factory.Create(
            config,
            null, // No secret manager
            TestContext.Current.CancellationToken);

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
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "UnknownType"
        };

        var result = await factory.Create(
            config,
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithNamedInstanceUsesBackslashFormat()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "myserver",
            InstanceName = "SQLEXPRESS",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithNonDefaultPortUsesCommaFormat()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "myserver",
            Port = 1434,
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithTrustServerCertificateIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            TrustServerCertificate = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithPoolingDisabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            EnableConnectionPooling = false,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithCustomPoolSizesIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            EnableConnectionPooling = true,
            MinPoolSize = 5,
            MaxPoolSize = 50,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithMarsEnabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            EnableMultipleActiveResultSets = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithApplicationNameIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            ApplicationName = "MyApp",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithCustomTimeoutIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            ConnectionTimeoutSeconds = 30,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithAdditionalPropertiesIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["MultiSubnetFailover"] = "True"
            }
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithSqlAuthRequiresSecretManagerForSecretKey()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object,
            null); // No secret manager provider

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "SqlAuth",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = "sa",
                ["SecretKeyName"] = "sql-password"
            }
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConfigurationDelegatesToTypedCreate()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<MsSqlConnection>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithGenericConnectionTypeReturnsFaillureForUnsupportedType()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        IGenericConfiguration config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config, "PostgreSql");

        result.IsSuccess.ShouldBeFalse();
    }

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
    public void CreateWithAllOptionsIncludesThemInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
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

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithEncryptionEnabledIncludesInConnectionString()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            Encrypt = true,
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithUnknownAuthenticationTypeReturnsFailure()
    {
        var factory = new MsSqlConnectionFactory(
            _factoryLogger.Object,
            _connectionLogger.Object);

        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "NotARealAuthType"
        };

        var result = factory.Create(config);

        result.IsSuccess.ShouldBeFalse();
    }
}
