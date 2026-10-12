using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Quartz.Configuration;

namespace FdwReferenceSchedulers.Quartz;

/// <summary>
/// Factory interface for creating Quartz scheduling service instances.
/// </summary>
public interface IQuartzSchedulingFactory : ISchedulingFactory<IFrameworkSchedulingService, QuartzSchedulerConfiguration>
{
}
