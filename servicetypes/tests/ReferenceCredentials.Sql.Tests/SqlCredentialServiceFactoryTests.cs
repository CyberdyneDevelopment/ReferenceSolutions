using System;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using ReferenceCredentials.Sql;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.DataVault.Abstractions;
using Moq;
using Fdw;
using Fdw.Services;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.Credentials.Sql.Outcomes;

namespace ReferenceCredentials.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlCredentialServiceFactory"/>: the null/missing-typed-body guard clauses and
/// the <see cref="Fdw.Abstractions.IServiceFactory"/> adapter surface.
/// </summary>
public class SqlCredentialServiceFactoryTests
{
    private static CredentialServiceConfiguration MakeHeader(SqlCredentialServiceConfiguration? typedBody)
        => new() { Id = Guid.NewGuid(), Name = "sql-credentials", Configuration = typedBody };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorNullVaultProviderThrowsArgumentNullException()
    {
        // Act
        var act = () => new SqlCredentialServiceFactory(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateNullConfigurationReturnsFailure()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);

        // Act
        var result = factory.Create((CredentialServiceConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateMissingTypedBodyReturnsFailure()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(null);

        // Act
        var result = factory.Create(header);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateValidHeaderReturnsSuccess()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" });

        // Act
        var result = factory.Create(header);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<SqlCredentialService>();
        ((IDisposable)result.Value!).Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateGenericWrongConfigurationTypeReturnsFailure()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var wrongConfig = new Mock<IGenericConfiguration>().Object;

        // Act
        var result = factory.Create(wrongConfig);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateGenericValidHeaderDelegatesToTypedOverload()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" });

        // Act
        var result = factory.Create((IGenericConfiguration)header);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ((IDisposable)result.Value!).Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryGenericCreateTMatchingTypeSucceeds()
    {
        // Arrange
        IServiceFactory factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" });

        // Act
        var result = factory.Create<ICredentialService>(header);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ((IDisposable)result.Value!).Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryGenericCreateTMismatchedTypeReturnsFailure()
    {
        // Arrange
        IServiceFactory factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" });

        // Act
        var result = factory.Create<UnrelatedFakeService>(header);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceFactoryNonGenericCreateWrapsIntoIGenericServiceOnSuccess()
    {
        // Arrange
        IServiceFactory factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var header = MakeHeader(new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" });

        // Act
        var result = factory.Create(header);

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
        IServiceFactory factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);

        // Act
        var result = factory.Create((IGenericConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    private sealed class UnrelatedFakeService : IGenericService
    {
        public string Id => "fake";
        public string ServiceType => "Fake";
        public bool IsAvailable => true;

        public System.Threading.Tasks.Task<Fdw.Results.IGenericResult<T>> Execute<T>(IGenericCommand command, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public System.Threading.Tasks.Task<Fdw.Results.IGenericResult> Execute(IGenericCommand command, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
