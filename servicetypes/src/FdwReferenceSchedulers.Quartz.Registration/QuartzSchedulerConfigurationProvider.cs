using Fdw.Services.Configuration;
using FdwReferenceSchedulers.Quartz.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.Scheduling.Abstractions;
namespace FdwReferenceSchedulers.Quartz.Registration;

/// <summary>Supplies the QuartzScheduler implementation's own configuration.</summary>
public sealed class QuartzSchedulerConfigurationProvider
    : ImplementationProviderBase<QuartzSchedulerConfiguration, ISchedulerImplementationConfiguration>,
      IQuartzSchedulerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public QuartzSchedulerConfigurationProvider(
        ILogger<QuartzSchedulerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
