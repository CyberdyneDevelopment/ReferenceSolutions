namespace FdwReferenceNotifications.Teams.Tests;

/// <summary>
/// Tests for TeamsNotificationConfiguration class.
/// </summary>
public class TeamsNotificationConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorSetsExpectedDefaults()
    {
        // Arrange & Act
        var config = new TeamsNotificationConfiguration();

        // Assert
        // Why Id is empty and not a fresh Guid: the base deliberately does not default it —
        // the database owns identity assignment, and a random default propagates into typed-body
        // lookups as a WHERE that matches nothing.
        config.Id.ShouldBe(Guid.Empty);
        config.Implementation.ShouldBeEmpty();
        config.IsEnabled.ShouldBeTrue();
        config.DefaultWebhookUrl.ShouldBeNull();
        config.TimeoutSeconds.ShouldBe(30);
        config.UseAdaptiveCards.ShouldBeTrue();
        config.SecretManagerName.ShouldBeNull();
        config.SecretKeyName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void PropertiesCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new TeamsNotificationConfiguration();
        var id = Guid.NewGuid();

        // Act
        config.Id = id;
        config.Name = "CustomTeams";
        // Why explicitly set and not assumed from construction: Implementation defaults to
        // string.Empty (it is stamped by the domain provider from the domain row, never set by the
        // typed body's own constructor) -- this is a plain settable property like its siblings here.
        config.Implementation = "Teams";
        config.IsEnabled = false;
        config.DefaultWebhookUrl = "https://teams.webhook.url";
        config.TimeoutSeconds = 60;
        config.UseAdaptiveCards = false;
        config.SecretManagerName = "MySecretManager";
        config.SecretKeyName = "MySecretKey";

        // Assert
        config.Id.ShouldBe(id);
        config.Name.ShouldBe("CustomTeams");
        config.Implementation.ShouldBe("Teams");
        config.IsEnabled.ShouldBeFalse();
        config.DefaultWebhookUrl.ShouldBe("https://teams.webhook.url");
        config.TimeoutSeconds.ShouldBe(60);
        config.UseAdaptiveCards.ShouldBeFalse();
        config.SecretManagerName.ShouldBe("MySecretManager");
        config.SecretKeyName.ShouldBe("MySecretKey");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ImplementationIsStampedByTheProviderNotTheConstructor()
    {
        // Arrange
        var config = new TeamsNotificationConfiguration();

        // Act & Assert
        // Why empty and not "Teams": Name, Domain and Implementation come from the domain row and are
        // stamped on during a read, never persisted on the implementation row and never defaulted here
        // -- the same reason Id is left at Guid.Empty. [ManagedConfiguration] is what declares the type.
        config.Implementation.ShouldBeEmpty();
    }
}
