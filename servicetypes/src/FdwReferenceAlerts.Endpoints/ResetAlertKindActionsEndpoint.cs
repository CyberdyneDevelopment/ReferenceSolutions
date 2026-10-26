using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Goes back to the actions a kind of alert offers by default (DELETE /alerts/kinds/{Kind}/actions).
/// </summary>
[ExcludeFromCodeCoverage]
public class ResetAlertKindActionsEndpoint : ResetAlertKindActionsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetAlertKindActionsEndpoint"/> class.
    /// </summary>
    public ResetAlertKindActionsEndpoint(IAlertKindSettingsService kinds, ILogger<ResetAlertKindActionsEndpoint> logger)
        : base(kinds, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
