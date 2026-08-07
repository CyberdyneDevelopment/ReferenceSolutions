using Fdw.Configuration;
using Fdw.Services.Data;

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
        var config = new DataGatewayConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.Name.ShouldBe(string.Empty);
        config.SectionName.ShouldBe("DataGateway");
        config.ServiceType.ShouldBe("DataGateway");
        config.ServiceOptionType.ShouldBeNull();
        config.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IdCanBeSetAndRetrieved()
    {
        // Arrange
        var id = Guid.NewGuid();
        var config = new DataGatewayConfiguration { Id = id };

        // Assert
        config.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NameCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new DataGatewayConfiguration { Name = "MyGateway" };

        // Assert
        config.Name.ShouldBe("MyGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ServiceOptionTypeCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new DataGatewayConfiguration { ServiceOptionType = "CustomType" };

        // Assert
        config.ServiceOptionType.ShouldBe("CustomType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsEnabledCanBeSetToFalse()
    {
        // Arrange
        var config = new DataGatewayConfiguration { IsEnabled = false };

        // Assert
        config.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIGenericConfiguration()
    {
        // Arrange
        var config = new DataGatewayConfiguration();

        // Assert
        config.ShouldBeAssignableTo<IGenericConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EachInstanceGetsUniqueId()
    {
        // Act
        var config1 = new DataGatewayConfiguration();
        var config2 = new DataGatewayConfiguration();

        // Assert
        config1.Id.ShouldNotBe(config2.Id);
    }
}
