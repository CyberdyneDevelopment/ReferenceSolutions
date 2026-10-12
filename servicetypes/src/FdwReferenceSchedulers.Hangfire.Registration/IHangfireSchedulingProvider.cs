using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Registration;

/// <summary>The HangfireScheduling implementation of its domain.</summary>
public interface IHangfireSchedulingProvider : IImplementationServiceProvider<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>
{
}
