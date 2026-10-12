using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Hangfire.Configuration;

namespace FdwReferenceSchedulers.Hangfire;

/// <summary>
/// Factory interface for creating Hangfire scheduling service instances.
/// </summary>
public interface IHangfireSchedulingFactory : ISchedulingFactory<IFrameworkSchedulingService, HangfireSchedulerConfiguration>
{
}
