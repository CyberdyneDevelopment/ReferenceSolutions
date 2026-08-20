using ReferenceNotifications.Teams;

namespace ReferenceNotifications.Teams.Tests;

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
        config.SectionName.ShouldBe("Notifications:Teams");
        config.ServiceType.ShouldBe("Notification");
        config.ServiceOptionType.ShouldBe("Teams");
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
        config.IsEnabled = false;
        config.DefaultWebhookUrl = "https://teams.webhook.url";
        config.TimeoutSeconds = 60;
        config.UseAdaptiveCards = false;
        config.SecretManagerName = "MySecretManager";
        config.SecretKeyName = "MySecretKey";

        // Assert
        config.Id.ShouldBe(id);
        config.Name.ShouldBe("CustomTeams");
        // Why these stay "Teams" through a mutation pass: the option type is fixed by the
        // constructor, not set per instance — a Teams configuration that could call itself
        // something else would be resolvable under a name nothing registers.
        config.ServiceOptionType.ShouldBe("Teams");
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
    public void ServiceOptionTypeIsFixedByTheConstructor()
    {
        // Arrange
        var config = new TeamsNotificationConfiguration();

        // Act & Assert
        config.ServiceOptionType.ShouldBe("Teams");
        config.ServiceType.ShouldBe("Notification");
    }
}
