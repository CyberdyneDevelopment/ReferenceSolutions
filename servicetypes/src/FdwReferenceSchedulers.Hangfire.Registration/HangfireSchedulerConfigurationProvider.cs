using Fdw.Services.Configuration;
using FdwReferenceSchedulers.Hangfire.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.Scheduling.Abstractions;
namespace FdwReferenceSchedulers.Hangfire.Registration;

/// <summary>Supplies the HangfireScheduler implementation's own configuration.</summary>
public sealed class HangfireSchedulerConfigurationProvider
    : ImplementationProviderBase<HangfireSchedulerConfiguration, ISchedulerImplementationConfiguration>,
      IHangfireSchedulerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public HangfireSchedulerConfigurationProvider(
        ILogger<HangfireSchedulerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
