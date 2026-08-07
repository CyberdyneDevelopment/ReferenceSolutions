using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.RestEndpoints.Crud;

namespace Reference.Api.Endpoints.Notifications;

/// <summary>
/// Endpoint to create a new notification configuration (POST /notifications).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNotificationEndpoint
    : CrudCreateEndpoint<CreateNotificationRequest, NotificationDetailDto>
{
    private readonly IServiceConfigurationProvider<NotificationConfiguration> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNotificationEndpoint"/> class.
    /// </summary>
    public CreateNotificationEndpoint(IServiceConfigurationProvider<NotificationConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string GetResourceName(CreateNotificationRequest request) => request.Name;

    /// <inheritdoc />
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
        // Why: Do not set Id — Save mints a v7 GUID for new records (Id == Guid.Empty signals INSERT).
        var config = new NotificationConfiguration
        {
            Name = request.Name,
            ServiceOptionType = request.NotificationType,
            Description = request.Description
        };

        var saveResult = await _provider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<NotificationDetailDto>();
        }

        var saved = saveResult.Value!;
        return GenericResult<NotificationDetailDto>.Success(new NotificationDetailDto
        {
            Id = saved.Id,
            Name = saved.Name,
            ServiceOptionType = saved.ServiceOptionType,
            Description = saved.Description,
            IsEnabled = true
        });
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(NotificationDetailDto detail, CancellationToken ct)
        => Send.ResponseAsync(detail, 201, ct);

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
