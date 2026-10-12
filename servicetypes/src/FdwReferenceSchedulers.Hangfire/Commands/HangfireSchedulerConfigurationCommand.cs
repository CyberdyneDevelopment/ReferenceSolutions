using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSchedulers.Hangfire.Configuration;

namespace FdwReferenceSchedulers.Hangfire.Commands;

/// <summary>ConfigurationCommands TypeOption for the HangfireScheduler configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "HangfireSchedulerConfiguration")]
public sealed class HangfireSchedulerConfigurationCommand : ConfigurationCommandBase<HangfireSchedulerConfiguration>
{
    /// <inheritdoc/>
    public HangfireSchedulerConfigurationCommand() : base("HangfireSchedulerConfiguration") { }
}
