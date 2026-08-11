using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list all notification rules.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationRulesEndpoint : ListNotificationRulesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListNotificationRulesEndpoint"/> class.
    /// </summary>
    public ListNotificationRulesEndpoint(IServiceConfigurationProvider<NotificationRuleConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
