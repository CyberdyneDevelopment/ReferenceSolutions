using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSchedulers.Quartz.Configuration;

namespace FdwReferenceSchedulers.Quartz.Commands;

/// <summary>ConfigurationCommands TypeOption for the QuartzScheduler configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "QuartzSchedulerConfiguration")]
public sealed class QuartzSchedulerConfigurationCommand : ConfigurationCommandBase<QuartzSchedulerConfiguration>
{
    /// <inheritdoc/>
    public QuartzSchedulerConfigurationCommand() : base("QuartzSchedulerConfiguration") { }
}
