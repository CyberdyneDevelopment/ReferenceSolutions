using Fdw.Services.Abstractions;

using Fdw.Services.Scheduling.Abstractions;
namespace FdwReferenceSchedulers.Quartz.Registration;

/// <summary>Supplies the QuartzScheduler implementation's own configuration.</summary>
public interface IQuartzSchedulerConfigurationProvider
    : IImplementationConfigurationProvider<ISchedulerImplementationConfiguration>
{
}
