using FdwReferenceSchedulers.Quartz.Services;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// FDW-1055: Quartz builds <see cref="ScheduledPipelineJob"/> by constructor injection on every fire. FDW never
/// registers <see cref="IDataGateway"/> (only the factory and <see cref="IDataGatewayProvider"/>), so a job that
/// asked for the gateway failed to build and Quartz set the schedule's triggers to ERROR.
/// </summary>
public sealed class ScheduledPipelineJobActivationTests
{
    private static ServiceProvider BuildContainerThatRegistersOnlyTheProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IDataGatewayProvider>().Object);
        services.AddSingleton(new Mock<IGenericScheduledExecutionHandler>().Object);
        services.AddSingleton(new Mock<IAuthenticationContextAccessor>().Object);
        services.AddTransient<ScheduledPipelineJob>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void JobIsBuiltFromAContainerThatRegistersNoDataGateway()
    {
        // Arrange
        using var container = BuildContainerThatRegistersOnlyTheProvider();

        // Act
        var job = container.GetRequiredService<ScheduledPipelineJob>();

        // Assert
        job.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void JobConstructorDoesNotAskForTheDataGatewayItself()
    {
        // Arrange
        var parameterTypes = typeof(ScheduledPipelineJob).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType);

        // Act
        var asksForTheGateway = parameterTypes.Any(type => type == typeof(IDataGateway));

        // Assert
        asksForTheGateway.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JobRefusesANullProvider()
    {
        // Arrange
        var handler = new Mock<IGenericScheduledExecutionHandler>().Object;
        var accessor = new Mock<IAuthenticationContextAccessor>().Object;

        // Act
        var build = () => new ScheduledPipelineJob(handler, null!, accessor);

        // Assert
        Should.Throw<ArgumentNullException>(build);
    }
}
