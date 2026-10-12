using FdwReferenceSchedulers.Hangfire.Services;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// FDW-1055: the same fault as the Quartz job. Hangfire builds <see cref="HangfireJobDispatcher"/> through the
/// container, which holds no <see cref="IDataGateway"/> registration.
/// </summary>
public sealed class HangfireJobDispatcherActivationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void DispatcherIsBuiltFromAContainerThatRegistersNoDataGateway()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IDataGatewayProvider>().Object);
        services.AddSingleton(new Mock<IGenericScheduledExecutionHandler>().Object);
        services.AddSingleton(new Mock<IAuthenticationContextAccessor>().Object);
        services.AddTransient<HangfireJobDispatcher>();
        using var container = services.BuildServiceProvider(validateScopes: true);

        // Act
        var dispatcher = container.GetRequiredService<HangfireJobDispatcher>();

        // Assert
        dispatcher.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void DispatcherConstructorDoesNotAskForTheDataGatewayItself()
    {
        // Arrange
        var parameterTypes = typeof(HangfireJobDispatcher).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType);

        // Act
        var asksForTheGateway = parameterTypes.Any(type => type == typeof(IDataGateway));

        // Assert
        asksForTheGateway.ShouldBeFalse();
    }
}
