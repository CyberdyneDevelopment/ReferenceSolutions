using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Says which actions each kind of alert offers the caller's company (GET /alerts/kinds).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListAlertKindsEndpoint : ListAlertKindsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAlertKindsEndpoint"/> class.
    /// </summary>
    public ListAlertKindsEndpoint(IAlertKindSettingsService kinds, ILogger<ListAlertKindsEndpoint> logger)
        : base(kinds, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
