using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Chooses the actions a kind of alert offers the caller's company (PUT /alerts/kinds/{Kind}/actions).
/// </summary>
[ExcludeFromCodeCoverage]
public class SaveAlertKindActionsEndpoint : SaveAlertKindActionsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveAlertKindActionsEndpoint"/> class.
    /// </summary>
    public SaveAlertKindActionsEndpoint(IAlertKindSettingsService kinds, ILogger<SaveAlertKindActionsEndpoint> logger)
        : base(kinds, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Alerts");
    }
}
