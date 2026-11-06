using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;

namespace FdwReferenceEscalation.Endpoints;

/// <summary>
/// List escalation alerts (FDW-3863).
/// </summary>
public sealed class ListEscalationAlertsEndpoint : ListEscalationAlertsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListEscalationAlertsEndpoint"/> class.
    /// </summary>
    /// <param name="alerts">The alerts escalation policies walk.</param>
    public ListEscalationAlertsEndpoint(IEscalationAlerts alerts)
        : base(alerts) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
