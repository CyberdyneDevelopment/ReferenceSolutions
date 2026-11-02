namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request identifying a single schedule by name.
/// </summary>
public sealed class ScheduleNameProxyRequest
{
    /// <summary>Gets or sets the schedule name.</summary>
    public string Name { get; set; } = string.Empty;
}
