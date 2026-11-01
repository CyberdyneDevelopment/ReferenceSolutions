using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list all notification rules.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationRulesEndpoint : ListNotificationRulesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListNotificationRulesEndpoint"/> class.
    /// </summary>
    public ListNotificationRulesEndpoint(INotificationRuleConfigurationProvider provider, ILogger<ListNotificationRulesEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
