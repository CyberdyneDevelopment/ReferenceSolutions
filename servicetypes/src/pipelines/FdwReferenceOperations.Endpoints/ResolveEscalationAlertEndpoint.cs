using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;

namespace FdwReferenceEscalation.Endpoints;

/// <summary>
/// Resolve an escalation alert (FDW-3863).
/// </summary>
public sealed class ResolveEscalationAlertEndpoint : ResolveEscalationAlertEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveEscalationAlertEndpoint"/> class.
    /// </summary>
    /// <param name="alerts">The alerts escalation policies walk.</param>
    public ResolveEscalationAlertEndpoint(IEscalationAlerts alerts)
        : base(alerts) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
