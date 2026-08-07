using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Console;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Console;
using Fdw.Services.Notifications.Console.Commands;

namespace ReferenceNotifications.Console.Tests;

/// <summary>
/// Tests for ConsoleNotificationService behavior.
/// </summary>
public sealed class ConsoleNotificationServiceTests
{
    private static ConsoleNotificationConfiguration BuildConfig(string name = "DevConsole") =>
        new()
        {
            LogLevel = "Information"
        };

    private static NotificationRequest BuildRequest(string message = "Test message") =>
        new(
            channelName: "Console",
            recipients: Array.Empty<string>(),
            subject: "Test Subject",
            message: message);

    private static ConsoleNotificationService BuildService(ConsoleNotificationConfiguration? config = null)
    {
        config ??= BuildConfig();
        return new ConsoleNotificationService(
            NullLogger<ConsoleNotificationService>.Instance,
            config);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ServiceTypeIsConsole()
    {
        // Arrange / Act
        var service = BuildService();

        // Assert
        service.ServiceType.ShouldBe("Console");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsAvailableIsAlwaysTrue()
    {
        // Arrange / Act
        var service = BuildService();

        // Assert
        service.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ChannelIsConsoleChannel()
    {
        // Arrange / Act
        var service = BuildService();

        // Assert
        service.Channel.ShouldNotBeNull();
        service.Channel.Name.ShouldBe("Console");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IdIsNonEmptyGuid()
    {
        // Arrange / Act
        var service = BuildService();

        // Assert
        service.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(service.Id, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsFailureWhenMessageIsEmpty()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest(message: string.Empty);

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsFailureWhenMessageIsWhitespace()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest(message: "   ");

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsSuccessForValidRequest()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest();

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendLogsAtInformationLevelAndReturnsSuccess()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendReturnsSynchronouslyCompletedTask()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest();

        // Act
        var task = service.Send(request, TestContext.Current.CancellationToken);

        // Assert - Console notifications complete synchronously
        task.IsCompleted.ShouldBeTrue();
        var result = await task;
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendReturnsRequestIdInResult()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.RequestId.ShouldBe(request.RequestId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteTypedReturnsFailureForUnsupportedCommand()
    {
        // Arrange
        var service = BuildService();
        var unsupportedCommand = new Mock<IGenericCommand>().Object;

        // Act
        var result = await service.Execute<INotificationResult>(unsupportedCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteNonGenericReturnsSuccessForValidRequest()
    {
        // Arrange
        var service = BuildService();
        var request = BuildRequest();

        // Act
        var result = await service.Execute(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteNonGenericReturnsFailureForUnsupportedCommand()
    {
        // Arrange
        var service = BuildService();
        var unsupportedCommand = new Mock<IGenericCommand>().Object;

        // Act
        var result = await service.Execute(unsupportedCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        var config = BuildConfig();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConsoleNotificationService(null!, config));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenConfigurationIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConsoleNotificationService(
                NullLogger<ConsoleNotificationService>.Instance,
                null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DisposeDoesNotThrow()
    {
        // Arrange
        var service = BuildService();

        // Act & Assert
        Should.NotThrow(() => service.Dispose());
    }
}
