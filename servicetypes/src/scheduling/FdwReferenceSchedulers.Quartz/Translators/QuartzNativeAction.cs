using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Quartz;

namespace FdwReferenceSchedulers.Quartz.Translators;

/// <summary>
/// The native call a Quartz translator produces. Quartz's own API is heterogeneous across
/// operations (<c>ScheduleJob</c>, <c>PauseTrigger</c>, <c>TriggerJob</c>, ...) — unlike SQL, where
/// every operation ends in one <c>SqlCommand</c> shape — so the translated value is the call itself,
/// closed over whatever native objects (<see cref="IJobDetail"/>, <see cref="ITrigger"/>,
/// <see cref="TriggerKey"/>, <see cref="JobKey"/>) that operation needs, ready for the service to
/// invoke against its live <see cref="IScheduler"/>.
/// </summary>
/// <param name="scheduler">The live Quartz scheduler to act against.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate Task<IGenericResult> QuartzNativeAction(IScheduler scheduler, CancellationToken cancellationToken);
