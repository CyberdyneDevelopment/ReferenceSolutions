using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to create a new notification configuration (POST /notifications).
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateNotificationEndpoint
    : CrudCreateEndpointBase<CreateNotificationRequest, NotificationDetailDto>
{
    private readonly INotificationConfigurationProvider _provider;
    private readonly Lazy<IEffectiveSettingsProvider> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNotificationEndpoint"/> class.
    /// </summary>
    public CreateNotificationEndpoint(
        ILogger<CreateNotificationEndpoint> logger, INotificationConfigurationProvider provider, Lazy<IEffectiveSettingsProvider> settings)
        : base(logger)
    {
        _provider = provider;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string GetResourceName(CreateNotificationRequest request) => request.Name;

    protected override async Task<IGenericResult<bool>> CheckExists(
        CreateNotificationRequest request,
        CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<NotificationDetailDto>> Create(
        CreateNotificationRequest request,
        CancellationToken ct)
    {
        // Why the type comes from NotificationServiceTypes: it is the collection the application registered its notification types in,
        // and each type declares the settings it needs and makes its own configuration, so no list of types is written here.
        var type = NotificationServiceTypes.All().Values.FirstOrDefault(
            candidate => string.Equals(candidate.Name, request.NotificationType, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return GenericResult<NotificationDetailDto>.Failure(
                NotificationEndpointLog.UnknownNotificationImplementation(
                    Logger, request.Name, request.NotificationType,
                    string.Join(", ", NotificationServiceTypes.All().Values.Select(known => known.Name).OrderBy(name => name, StringComparer.Ordinal))));
        }

        // Name, Domain and Implementation are not set here: Save stamps them from its own arguments,
        // and the implementation row does not persist them -- they belong to the domain row.
        // What the request leaves out takes the caller's tenant's setting (FDW-3580): "make it configurable ... in configuration and not hardcoded".
        var tenantId = new ClaimsPrincipalAuthenticationContext(User).ActiveTenantId;
        var enabled = InstanceDefault.Resolve(
            request.IsEnabled, _settings, DefaultValueSettings.DefaultNotificationChannelIsEnabled, tenantId, "notification", request.Name, "IsEnabled", Logger);
        if (enabled.IsFailure)
        {
            return enabled.ToNewResult<NotificationDetailDto>();
        }

        var config = type.NewConfiguration();
        config.IsEnabled = enabled.Value;
        config.Description = request.Description;
        config.SecretManagerName = request.SecretManagerName;
        config.SecretKeyName = request.SecretKeyName;

        var applied = NotificationSettings.ApplyNew(type, config, request.Settings, _settings, tenantId, request.Name, Logger);
        if (applied.IsFailure)
        {
            return applied.ToNewResult<NotificationDetailDto>();
        }

        var saveResult = await _provider
            .Save(config, "Notification", type.Name, request.Name, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<NotificationDetailDto>();
        }

        return GenericResult<NotificationDetailDto>.Success(new NotificationDetailDto
        {
            Id = config.Id,
            Name = request.Name,
            Implementation = type.Name,
            Description = config.Description,
            IsEnabled = config.IsEnabled,
            SecretManagerName = config.SecretManagerName,
            SecretKeyName = config.SecretKeyName
        });
    }

    protected override Task SendCreatedResponse(NotificationDetailDto detail, CancellationToken ct)
        => Send.ResponseAsync(detail, 201, ct);

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
