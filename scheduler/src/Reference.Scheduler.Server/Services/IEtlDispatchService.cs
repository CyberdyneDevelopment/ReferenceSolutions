using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Service for dispatching jobs to the ETL server.
/// </summary>
public interface IEtlDispatchService
{
    /// <summary>
    /// Dispatches a job to the ETL server for execution.
    /// </summary>
    /// <param name="scheduleName">The name of the schedule triggering the dispatch.</param>
    /// <param name="pipelineName">The name of the pipeline to execute.</param>
    /// <param name="triggerSource">The source that triggered the execution.</param>
    /// <param name="tenantId">
    /// The tenant the triggering schedule belongs to (<c>ScheduleConfiguration.TenantId</c>), if any.
    /// Relayed to the ETL server so the dispatched execution's RLS SESSION_CONTEXT is scoped correctly.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure of the dispatch operation.</returns>
    Task<IGenericResult> Dispatch(
        string scheduleName,
        string pipelineName,
        string triggerSource,
        Guid? tenantId,
        CancellationToken cancellationToken);
}
