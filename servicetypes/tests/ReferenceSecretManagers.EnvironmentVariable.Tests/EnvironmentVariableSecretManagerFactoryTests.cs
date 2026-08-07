using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableSecretManagerFactory"/>: the header/typed-body dispatch in
/// <c>CreateSecretManager(IGenericConfiguration)</c>, the null/invalid guard clauses, the synchronous
/// <see cref="Fdw.Abstractions.IServiceFactory"/> adapter surface, and the logger-failure exception path.
/// </summary>
public class EnvironmentVariableSecretManagerFactoryTests
{
    private static EnvironmentVariableConfiguration MakeTypedBody() => new()
    {
        Id = Guid.NewGuid(),
        SecretManagerId = Guid.NewGuid(),
        Prefix = "FDW_SECRET_",
        Separator = "__",
    };

    private static ILoggerFactory MakeLoggerFactory()
    {
        var mock = new Mock<ILoggerFactory>();
        mock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
        return mock.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerTypedBodyNullConfigurationReturnsFailure()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());

        // Act
        var result = await factory.CreateSecretManager((EnvironmentVariableConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("null");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerTypedBodyValidConfigurationReturnsSuccess()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = await factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(config.Id.ToString());
        result.Value.Dispose();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerGenericHeaderWithTypedBodySucceeds()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var typedBody = MakeTypedBody();
        var header = new SecretManagerConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "env-secrets",
            Configuration = typedBody,
        };

        // Act
        var result = await factory.CreateSecretManager((IGenericConfiguration)header);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerGenericDirectTypedBodySucceeds()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = await factory.CreateSecretManager((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerGenericInvalidConfigurationTypeReturnsFailure()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        // Why: header with no typed body attached — neither the header-with-EnvironmentVariableConfiguration
        // branch nor the direct-EnvironmentVariableConfiguration branch matches.
        var wrongConfig = new SecretManagerConfiguration { Id = Guid.NewGuid(), Name = "no-typed-body" };

        // Act
        var result = await factory.CreateSecretManager((IGenericConfiguration)wrongConfig);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerGenericNullConfigurationReturnsFailure()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());

        // Act
        var result = await factory.CreateSecretManager((IGenericConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerLoggerFactoryThrowsForServiceLoggerReturnsFailure()
    {
        // Arrange — Why: the factory's own logger (ctor) resolves on the first CreateLogger call; the
        // per-instance service logger resolves on the second. Throwing only on the second call exercises
        // CreateSecretManagerInternal's catch(Exception) fail-loud path without breaking construction.
        var mock = new Mock<ILoggerFactory>();
        mock.SetupSequence(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance)
            .Throws(new InvalidOperationException("logger boom"));
        var factory = new EnvironmentVariableSecretManagerFactory(mock.Object);

        // Act
        var result = await factory.CreateSecretManager(MakeTypedBody());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("creation failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateSyncTypedConfigurationDelegatesToAsyncAndSucceeds()
    {
        // Arrange
        var factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ServiceFactoryTypedCreateGenericConfigurationSucceeds()
    {
        // Arrange
        IServiceFactory<ISecretManager> factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = factory.Create((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryNonGenericCreateWrapsIntoIGenericServiceOnSuccess()
    {
        // Arrange
        IServiceFactory factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = factory.Create((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeAssignableTo<IGenericService>();
        ((IDisposable)result.Value!).Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryNonGenericCreatePropagatesFailure()
    {
        // Arrange
        IServiceFactory factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());

        // Act
        var result = factory.Create((IGenericConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryGenericCreateTMatchingTypeSucceeds()
    {
        // Arrange
        IServiceFactory factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = factory.Create<ISecretManager>((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryGenericCreateTMismatchedTypeReturnsFailure()
    {
        // Arrange — Why: the created service is an ISecretManager, never an unrelated IGenericService
        // implementation, so requesting T=UnrelatedFakeService exercises the ServiceTypeMismatch branch.
        IServiceFactory factory = new EnvironmentVariableSecretManagerFactory(MakeLoggerFactory());
        var config = MakeTypedBody();

        // Act
        var result = factory.Create<UnrelatedFakeService>((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("not of expected type");
    }

    // Why: a minimal, unrelated IGenericService implementation used only to prove the factory's
    // "T must match the created service's runtime type" guard fails loud rather than force-casting.
    private sealed class UnrelatedFakeService : IGenericService
    {
        public string Id => "fake";
        public string ServiceType => "Fake";
        public bool IsAvailable => true;

        public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IGenericResult> Execute(IGenericCommand command, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
