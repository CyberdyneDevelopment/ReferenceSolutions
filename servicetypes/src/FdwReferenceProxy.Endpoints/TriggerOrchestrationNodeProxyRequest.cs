namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request for triggering an orchestration node by type.
/// </summary>
public sealed class TriggerOrchestrationNodeProxyRequest
{
    /// <summary>Gets or sets the orchestration node type to trigger.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical identifier of the node to trigger; when set, the name is ignored.</summary>
    public System.Guid? Id { get; set; }

    /// <summary>Gets or sets the name of the node to trigger; required when no id is given.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the parent path that disambiguates a type not unique by name.</summary>
    public string? ParentPath { get; set; }

    /// <summary>
    /// Gets or sets what caused the run: a registered JobTriggerSourceTypes member. Required by the
    /// ETL trigger endpoint, which records it on the execution (FDW-899).
    /// </summary>
    public string? TriggerSource { get; set; }
}
