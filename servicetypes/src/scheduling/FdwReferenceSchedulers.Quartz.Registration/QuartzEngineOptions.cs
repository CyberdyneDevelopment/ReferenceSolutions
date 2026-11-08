using System;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Quartz.Configuration;
using FdwReferenceSchedulers.Quartz.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FdwReferenceSchedulers.Quartz.Registration;

/// <summary>
/// What the Quartz engine runs on, taken from the Quartz scheduler row (FDW-4293): its <c>InstanceName</c> becomes the scheduler's name and its
/// <c>ThreadCount</c> the number of fires it runs at once.
/// </summary>
/// <remarks>
/// <para>
/// Why the row and not code: the row states both (the columns are NOT NULL), so no value is chosen here, and a row that states none is refused.
/// Why read once, when Quartz builds its options: the engine takes its name and pool when it is created and cannot change either while it runs,
/// so a changed row applies at the next host start. The row is read the way <c>PipelineStopGrace.Create</c> reads its setting: in a scope, under the
/// system context, since no request is in flight when the host starts.
/// </para>
/// <para>
/// Why exactly one row: the reconciliation pass reconciles against one Quartz scheduler per host (<c>sched.Schedule</c> has no column that names the
/// scheduler a schedule belongs to), so a host with none, or with several, has no scheduler to take its settings from and does not start.
/// </para>
/// <para>
/// Why a row that clusters is refused: Quartz runs on its in-memory job store, which the reconciliation pass and the missed-fire rules depend on, and
/// a clustered scheduler needs a persistent job store this scheduler does not have. <c>EnableClustering</c> has no meaning here, so it is not applied;
/// <see cref="QuartzSchedulerConfigurationValidator"/> says the same when the row is written.
/// </para>
/// </remarks>
public static class QuartzEngineOptions
{
    /// <summary>The Quartz setting that holds the size of the thread pool.</summary>
    public const string ThreadPoolMaxConcurrencyKey = "quartz.threadPool.maxConcurrency";

    /// <summary>Applies the Quartz scheduler row to the options Quartz is built with.</summary>
    /// <param name="options">The options Quartz is being built with.</param>
    /// <param name="services">The host's services.</param>
    /// <exception cref="InvalidOperationException">There is not exactly one Quartz scheduler row, or it states a value the engine refuses; the host does not start.</exception>
    public static void Apply(QuartzOptions options, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        var scheduler = Read(services);
        options.SchedulerName = scheduler.InstanceName;
        options[ThreadPoolMaxConcurrencyKey] = scheduler.ThreadCount.ToString(CultureInfo.InvariantCulture);
    }

    private static QuartzSchedulerConfiguration Read(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(QuartzEngineOptions));
        using var scope = services.CreateScope();
        using var elevation = new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>());

        // Why GetAwaiter().GetResult(): Quartz builds its options synchronously, and there is no request or loop to await in at host start.
        var rows = scope.ServiceProvider.GetRequiredService<IDomainConfigurationProvider<ISchedulerImplementationConfiguration>>()
            .Get().GetAwaiter().GetResult();
        if (rows.IsFailure || rows.Value is null)
        {
            throw new InvalidOperationException(QuartzSchedulingLog.EngineSchedulerRowsNotOne(logger, 0, rows.CurrentMessage).Message);
        }

        var quartz = rows.Value.OfType<QuartzSchedulerConfiguration>().ToList();
        if (quartz.Count != 1)
        {
            throw new InvalidOperationException(
                QuartzSchedulingLog.EngineSchedulerRowsNotOne(logger, quartz.Count, "the Quartz engine takes its settings from exactly one row").Message);
        }

        var scheduler = quartz[0];
        var validation = new QuartzSchedulerConfigurationValidator().Validate(scheduler);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                QuartzSchedulingLog.EngineSchedulerRefused(logger, scheduler.Name, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))).Message);
        }

        QuartzSchedulingLog.EngineOptionsRead(logger, scheduler.Name, scheduler.InstanceName, scheduler.ThreadCount);
        return scheduler;
    }
}
