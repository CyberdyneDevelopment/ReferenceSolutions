namespace FdwReferenceNotifications.Console.Tests;

/// <summary>
/// Tests for ConsoleNotificationConfiguration properties and defaults.
/// </summary>
public sealed class ConsoleNotificationConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultConstructorAssumesNoLogLevel()
    {
        // Arrange / Act
        var config = new ConsoleNotificationConfiguration();

        // Assert
        // A row states its log level; none is assumed (FDW-3580).
        config.LogLevel.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void LogLevelPropertyIsSettable()
    {
        // Arrange
        var config = new ConsoleNotificationConfiguration();

        // Act
        config.LogLevel = "Debug";

        // Assert
        config.LogLevel.ShouldBe("Debug");
    }

    // Why removed: Implementation deliberately defaults to string.Empty on this class ("Set by the
    // provider from the domain row; never persisted" -- see ConsoleNotificationConfiguration.cs).
    // A fresh instance never carries "Console" until a domain provider stamps it there.
}
