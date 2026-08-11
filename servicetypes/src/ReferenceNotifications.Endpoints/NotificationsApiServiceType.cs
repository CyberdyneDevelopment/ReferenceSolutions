using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceNotifications.Endpoints.NotificationEndpointOptions;
using ReferenceNotifications.Endpoints.NotificationListEndpointOptions;
using ReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;
using ReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// The notifications domain's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Notifications")]
public class NotificationsApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsApiServiceType"/> class.
    /// </summary>
    public NotificationsApiServiceType()
        : base("Notifications", "Notifications", "Notifications API", "HTTP endpoints for the notifications domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            new NotificationEndpoints(),
            new NotificationRuleEndpoints(),
            new NotificationListEndpoints(),
            new NotificationPreferenceEndpoints(),
        };
}
