using System;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Console;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;

namespace ReferenceNotifications.Console.Tests;

/// <summary>
/// Tests for ConsoleNotificationFactory creation behavior.
/// </summary>
public sealed class ConsoleNotificationFactoryTests
{
    private static ConsoleNotificationFactory BuildFactory() =>
        new(
            NullLogger<ConsoleNotificationFactory>.Instance,
            NullLoggerFactory.Instance);

    private static ConsoleNotificationConfiguration BuildValidConfig() =>
        new() { LogLevel = "Information" };

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
        var result = await factory.CreateNotification((ConsoleNotificationConfiguration)null!);

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
    public async Task CreateNotificationGenericReturnsFailureForWrongType()
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
    public async Task CreateNotificationGenericReturnsSuccessForValidConsoleConfig()
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
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConsoleNotificationFactory(null!, NullLoggerFactory.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenLoggerFactoryIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConsoleNotificationFactory(
                NullLogger<ConsoleNotificationFactory>.Instance,
                null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreatedServiceIsConsoleNotificationService()
    {
        // Arrange
        var factory = BuildFactory();
        var config = BuildValidConfig();

        // Act
        var result = await factory.CreateNotification(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<ConsoleNotificationService>();
    }
}
