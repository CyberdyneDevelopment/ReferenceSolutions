using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Says what each named alert allows the caller (GET /alerts?alert=kind:id).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListAlertsEndpoint : ListAlertsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAlertsEndpoint"/> class.
    /// </summary>
    public ListAlertsEndpoint(IAlertActionService alerts, ILogger<ListAlertsEndpoint> logger)
        : base(alerts, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
