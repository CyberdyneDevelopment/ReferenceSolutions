using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.DataVault.Abstractions;

namespace FdwReferenceCredentials.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlCredentialServiceFactory"/>: the null/missing-typed-body guard clauses and
/// the <see cref="Fdw.Abstractions.IServiceFactory"/> adapter surface.
/// </summary>
public class SqlCredentialServiceFactoryTests
{
    // The factory takes the implementation configuration the domain provider hands it, so the
    // wrong-type guard is exercised with the DOMAIN record -- the type it must refuse.
    private static DomainConfiguration MakeDomainRecord()
        => new() { Id = Guid.NewGuid(), Name = "sql-credentials", Domain = "CredentialService", Implementation = "Sql" };

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
        var result = factory.Create((ICredentialServiceImplementationConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateWithDomainRecordReturnsFailure()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var domainRecord = MakeDomainRecord();

        // Act
        var result = factory.Create(domainRecord);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateValidImplementationReturnsSuccess()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var implementation = new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" };

        // Act
        var result = factory.Create(implementation);

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
    public void CreateGenericValidImplementationDelegatesToTypedOverload()
    {
        // Arrange
        var factory = new SqlCredentialServiceFactory(new Mock<IDataVaultProvider>().Object);
        var implementation = new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" };

        // Act
        var result = factory.Create((IGenericConfiguration)implementation);

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
        var implementation = new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" };

        // Act
        var result = factory.Create<ICredentialService>(implementation);

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
        var implementation = new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" };

        // Act
        var result = factory.Create<UnrelatedFakeService>(implementation);

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
        var implementation = new SqlCredentialServiceConfiguration { CredentialVaultName = "vault" };

        // Act
        var result = factory.Create(implementation);

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
