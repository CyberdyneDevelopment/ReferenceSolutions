using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling.Abstractions;

namespace Reference.Scheduler.Server.Models;

/// <summary>
/// Lightweight <see cref="IGenericSchedule"/> implementation for passing schedule data
/// to <see cref="IFrameworkSchedulingService"/> CRUD methods.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ScheduleDto : IGenericSchedule
{
    public required string ScheduleId { get; init; }
    public required string ScheduleName { get; init; }
    public required string ProcessId { get; init; }
    public string CronExpression { get; init; } = string.Empty;
    public DateTime? NextExecution { get; init; }
    public bool IsActive { get; init; } = true;
    public string TimeZoneId { get; init; } = "UTC";
    public IReadOnlyDictionary<string, object>? Metadata => null;
}
