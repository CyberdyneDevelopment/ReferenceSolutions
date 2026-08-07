using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Webhook;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Webhook;
using Fdw.Services.Notifications.Webhook.Commands;

namespace ReferenceNotifications.Webhook.Tests;

/// <summary>
/// Tests for WebhookNotificationFactory creation behavior.
/// </summary>
public sealed class WebhookNotificationFactoryTests
{
    private static WebhookNotificationFactory BuildFactory()
    {
        var logger = NullLogger<WebhookNotificationFactory>.Instance;
        var loggerFactory = NullLoggerFactory.Instance;
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;
        return new WebhookNotificationFactory(logger, loggerFactory, httpClientFactory);
    }

    private static WebhookNotificationConfiguration BuildValidConfig(string name = "TestWebhook") =>
        new()
        {
            Url = "https://hooks.example.com/notify",
            Method = "POST",
            ContentType = "application/json"
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationReturnsSuccessForValidConfig()
    {
        // Arrange
        var factory = BuildFactory();
        var config = BuildValidConfig();

        // Act
        var result = await factory.CreateNotification(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeAssignableTo<INotificationService>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationReturnsFailureForNullConfig()
    {
        // Arrange
        var factory = BuildFactory();

        // Act
        var result = await factory.CreateNotification((WebhookNotificationConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationReturnsFailureWhenUrlIsMissing()
    {
        // Arrange
        var factory = BuildFactory();
        var config = new WebhookNotificationConfiguration(); // Url is null

        // Act
        var result = await factory.CreateNotification(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationGenericReturnsFailureForNullConfig()
    {
        // Arrange
        var factory = BuildFactory();

        // Act
        var result = await factory.CreateNotification((IGenericConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationGenericReturnsFailureForWrongConfigType()
    {
        // Arrange
        var factory = BuildFactory();
        var wrongConfig = new Mock<IGenericConfiguration>().Object;

        // Act
        var result = await factory.CreateNotification(wrongConfig);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateNotificationGenericReturnsSuccessForValidWebhookConfig()
    {
        // Arrange
        var factory = BuildFactory();
        var config = BuildValidConfig();

        // Act
        var result = await factory.CreateNotification((IGenericConfiguration)config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationFactory(null!, loggerFactory, httpClientFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenLoggerFactoryIsNull()
    {
        // Arrange
        var logger = NullLogger<WebhookNotificationFactory>.Instance;
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationFactory(logger, null!, httpClientFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenHttpClientFactoryIsNull()
    {
        // Arrange
        var logger = NullLogger<WebhookNotificationFactory>.Instance;
        var loggerFactory = NullLoggerFactory.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationFactory(logger, loggerFactory, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreatedServiceIsWebhookNotificationService()
    {
        // Arrange
        var factory = BuildFactory();
        var config = BuildValidConfig();

        // Act
        var result = await factory.CreateNotification(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<WebhookNotificationService>();
    }
}
