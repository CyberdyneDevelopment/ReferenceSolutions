using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Takes an action on an alert as the caller (POST /alerts/{Kind}/{Id}/actions/{Action}).
/// </summary>
[ExcludeFromCodeCoverage]
public class ApplyAlertActionEndpoint : ApplyAlertActionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyAlertActionEndpoint"/> class.
    /// </summary>
    public ApplyAlertActionEndpoint(IAlertActionService alerts, ILogger<ApplyAlertActionEndpoint> logger)
        : base(alerts, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
