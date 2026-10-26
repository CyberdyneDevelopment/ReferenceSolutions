using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Takes back one of the caller's own actions on an alert (DELETE /alerts/{Kind}/{Id}/actions/{Action}).
/// </summary>
[ExcludeFromCodeCoverage]
public class TakeBackAlertActionEndpoint : TakeBackAlertActionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TakeBackAlertActionEndpoint"/> class.
    /// </summary>
    public TakeBackAlertActionEndpoint(IAlertActionService alerts, ILogger<TakeBackAlertActionEndpoint> logger)
        : base(alerts, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
