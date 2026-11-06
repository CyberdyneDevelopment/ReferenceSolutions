using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Messaging;
using Fdw.Services.Notifications.Abstractions;
using FdwReferenceNotifications.System.Registration;

namespace FdwReferenceNotifications.System.Tests;

/// <summary>
/// The lifetimes SystemNotificationType registers its factory and provider with. Both take the scoped IMessageService, so a host that validates
/// scopes (the Development environment) refuses to build when either is a singleton, and a host that does not keeps one IMessageService for life.
/// </summary>
public sealed class SystemNotificationRegistrationTests
{
    private static IServiceCollection RegisteredServices()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddLogging();
        // Why: NotificationServiceTypes' own collection-level Registration body registers the domain's base INotificationServiceProvider BEFORE the option
        // loop, and each option's Register decorates it; this test runs the option alone, so it seeds that prerequisite. IMessageService is registered
        // scoped, exactly as DefaultMessagingServiceType registers it.
        services.AddScoped<INotificationServiceProvider>(_ => Mock.Of<INotificationServiceProvider>());
        services.AddScoped(_ => Mock.Of<IMessageService>());

        new SystemNotificationType().Register(builder, null).IsSuccess.ShouldBeTrue();

        return services;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisteredFactoryAndProviderBuildWithScopeValidationOn()
    {
        var services = RegisteredServices();

        Should.NotThrow(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }).Dispose());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FactoryAndProviderBelongToTheScopeThatResolvesThem()
    {
        using var provider = RegisteredServices().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var firstProvider = first.ServiceProvider.GetRequiredService<ISystemNotificationProvider>();
        var firstFactory = first.ServiceProvider.GetRequiredService<ISystemNotificationFactory>();

        firstProvider.ShouldBeSameAs(first.ServiceProvider.GetRequiredService<ISystemNotificationProvider>());
        firstFactory.ShouldBeSameAs(first.ServiceProvider.GetRequiredService<ISystemNotificationFactory>());
        firstProvider.ShouldNotBeSameAs(second.ServiceProvider.GetRequiredService<ISystemNotificationProvider>());
        firstFactory.ShouldNotBeSameAs(second.ServiceProvider.GetRequiredService<ISystemNotificationFactory>());
    }
}
