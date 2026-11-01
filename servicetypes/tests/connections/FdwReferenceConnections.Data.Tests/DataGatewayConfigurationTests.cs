using Fdw.Configuration;

namespace Fdw.Services.Data.Tests;

[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultConstructorSetsDefaults()
    {
        // Act
        var config = new MainDataGatewayConfiguration();

        // Assert
        // Why Id is Guid.Empty and IsEnabled is false: FDW-711 dropped both hardcoded defaults --
        // the store assigns Id, and the mapper overwrites every field from the row immediately after
        // construction, so a value set here would only mask whatever the actual row holds.
        config.Id.ShouldBe(Guid.Empty);
        config.Name.ShouldBe(string.Empty);
        config.Implementation.ShouldBe(string.Empty);
        config.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IdCanBeSetAndRetrieved()
    {
        // Arrange
        var id = Guid.NewGuid();
        var config = new MainDataGatewayConfiguration { Id = id };

        // Assert
        config.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NameCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new MainDataGatewayConfiguration { Name = "MyGateway" };

        // Assert
        config.Name.ShouldBe("MyGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementationCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new MainDataGatewayConfiguration { Implementation = "CustomType" };

        // Assert
        config.Implementation.ShouldBe("CustomType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsEnabledCanBeSetToFalse()
    {
        // Arrange
        var config = new MainDataGatewayConfiguration { IsEnabled = false };

        // Assert
        config.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIGenericConfiguration()
    {
        // Arrange
        var config = new MainDataGatewayConfiguration();

        // Assert
        config.ShouldBeAssignableTo<IGenericConfiguration>();
    }
}
