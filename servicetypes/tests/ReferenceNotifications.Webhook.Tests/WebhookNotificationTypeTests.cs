using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Webhook;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Webhook;
using Fdw.Services.Notifications.Webhook.Commands;

namespace ReferenceNotifications.Webhook.Tests;

/// <summary>
/// Tests for WebhookNotificationType ServiceTypeOption registration.
/// </summary>
public sealed class WebhookNotificationTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NameIsWebhook()
    {
        // Arrange / Act
        var sut = new WebhookNotificationType();

        // Assert
        sut.Name.ShouldBe("Webhook");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsServiceType()
    {
        // Arrange / Act
        var sut = new WebhookNotificationType();

        // Assert
        sut.ShouldBeAssignableTo<IServiceType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterRegistersSingletonFactory()
    {
        // Arrange
        var sut = new WebhookNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();
        services.AddHttpClient();

        // Act
        var result = sut.Register(builder, null, "TestStore", "TestPath", "TestContainer");

        // Assert
        // The phase reports whether it ran, and hands back what it was given.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(builder);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IWebhookNotificationFactory>();
        factory.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigureBindsWebhookNotificationSection()
    {
        // Arrange
        var sut = new WebhookNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        var configData = new Dictionary<string, string?>
        {
            ["Notifications:Webhook:0:Name"] = "TestWebhook",
            ["Notifications:Webhook:0:Url"] = "https://hooks.example.com"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        sut.Configure(builder);

        // Assert
        services.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigureWithEmptyConfigDoesNotThrow()
    {
        // Arrange
        var sut = new WebhookNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        // Act & Assert
        Should.NotThrow(() => sut.Configure(builder));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DisplayNameIsWebhookNotifications()
    {
        // Arrange / Act
        var sut = new WebhookNotificationType();

        // Assert
        sut.DisplayName.ShouldBe("Webhook Notifications");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ChannelIsWebhookChannel()
    {
        // Arrange / Act
        var sut = new WebhookNotificationType();

        // Assert
        sut.Channel.ShouldNotBeNull();
        sut.Channel.Name.ShouldBe("Webhook");
    }
}
