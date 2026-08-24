using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using ReferenceNotifications.Console;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;
using ReferenceNotifications.Console.Registration;

namespace ReferenceNotifications.Console.Tests;

/// <summary>
/// Tests for ConsoleNotificationType ServiceTypeOption registration.
/// </summary>
public sealed class ConsoleNotificationTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NameIsConsole()
    {
        // Arrange / Act
        var sut = new ConsoleNotificationType();

        // Assert
        sut.Name.ShouldBe("Console");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsServiceType()
    {
        // Arrange / Act
        var sut = new ConsoleNotificationType();

        // Assert
        sut.ShouldBeAssignableTo<IServiceType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterRegistersSingletonFactory()
    {
        // Arrange
        var sut = new ConsoleNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();

        // Act
        var result = sut.Register(builder, null);

        // Assert
        // The phase reports whether it ran, and hands back what it was given.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(builder);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IConsoleNotificationFactory>();
        factory.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigureBindsConsoleNotificationSection()
    {
        // Arrange
        var sut = new ConsoleNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        var configData = new Dictionary<string, string?>
        {
            ["Notifications:Console:0:Name"] = "DevConsole",
            ["Notifications:Console:0:LogLevel"] = "Debug"
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
        var sut = new ConsoleNotificationType();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        // Act & Assert
        Should.NotThrow(() => sut.Configure(builder));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DisplayNameIsConsoleNotifications()
    {
        // Arrange / Act
        var sut = new ConsoleNotificationType();

        // Assert
        sut.DisplayName.ShouldBe("Console Notifications");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ChannelIsConsoleChannel()
    {
        // Arrange / Act
        var sut = new ConsoleNotificationType();

        // Assert
        sut.Channel.ShouldNotBeNull();
        sut.Channel.Name.ShouldBe("Console");
    }
}
