namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Canonical <c>sched.Schedule.LastRunStatus</c> values recorded by the scheduler for the
/// outcome of a schedule's most recent dispatch attempt.
/// </summary>
// Why: these mirror the canonical Fdw.Results.ExecutionStatus.ExecutionStatuses names ("Succeeded"/
// "Failed") but are held as local constants because those TypeOptions are declared
// RestrictToCurrentCompilation, so their generated static accessors aren't available cross-assembly;
// a runtime ExecutionStatuses.ByName(...) round-trip would still require the same literal key. The
// status reflects the DISPATCH outcome (the scheduler hands off to the ETL server asynchronously),
// not the ultimate pipeline-execution result.
internal static class ScheduleStatuses
{
    /// <summary>Dispatch to the ETL server was accepted.</summary>
    internal const string Succeeded = "Succeeded";

    /// <summary>Dispatch to the ETL server failed; the schedule will retry on the next evaluation.</summary>
    internal const string Failed = "Failed";
}
