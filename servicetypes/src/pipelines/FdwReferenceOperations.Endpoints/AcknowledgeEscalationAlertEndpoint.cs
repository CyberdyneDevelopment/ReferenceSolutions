using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;

namespace FdwReferenceEscalation.Endpoints;

/// <summary>
/// Acknowledge an escalation alert (FDW-3863).
/// </summary>
public sealed class AcknowledgeEscalationAlertEndpoint : AcknowledgeEscalationAlertEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcknowledgeEscalationAlertEndpoint"/> class.
    /// </summary>
    /// <param name="alerts">The alerts escalation policies walk.</param>
    public AcknowledgeEscalationAlertEndpoint(IEscalationAlerts alerts)
        : base(alerts) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
