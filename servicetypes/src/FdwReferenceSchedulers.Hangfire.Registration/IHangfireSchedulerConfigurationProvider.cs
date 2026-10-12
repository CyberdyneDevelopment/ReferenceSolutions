using Fdw.Services.Abstractions;

using Fdw.Services.Scheduling.Abstractions;
namespace FdwReferenceSchedulers.Hangfire.Registration;

/// <summary>Supplies the HangfireScheduler implementation's own configuration.</summary>
public interface IHangfireSchedulerConfigurationProvider
    : IImplementationConfigurationProvider<ISchedulerImplementationConfiguration>
{
}
