using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Abstractions;

namespace FdwReferenceSchedulers.Quartz.Registration;

/// <summary>The QuartzScheduling implementation of its domain.</summary>
public interface IQuartzSchedulingProvider : IImplementationServiceProvider<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>
{
}
