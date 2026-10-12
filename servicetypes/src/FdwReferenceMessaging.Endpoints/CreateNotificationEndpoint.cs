using System.Collections.Generic;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Services.Notifications.Abstractions;
﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Endpoints;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNotificationEndpoint"/> class.
    /// </summary>
    public CreateNotificationEndpoint(ILogger<CreateNotificationEndpoint> logger, INotificationConfigurationProvider provider)
        : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string GetResourceName(CreateNotificationRequest request) => request.Name;

    // The builder takes IsEnabled so a record is never created enabled by its own property initializer:
    // the caller states it, and it is the domain row's list field that Save writes from the record.
    private static readonly Dictionary<string, Func<bool, INotificationImplementationConfiguration>> Builders =
        new Dictionary<string, Func<bool, INotificationImplementationConfiguration>>(StringComparer.OrdinalIgnoreCase)
        {
            // Fully qualified deliberately: FdwReferenceNotifications.System and
            // Fdw.Services.Notifications.System both shadow the System namespace when imported, which
            // breaks generated code in this assembly.
            ["Console"] = isEnabled => new global::FdwReferenceNotifications.Console.ConsoleNotificationConfiguration { IsEnabled = isEnabled },
            ["Email"] = isEnabled => new global::FdwReferenceNotifications.Email.EmailNotificationConfiguration { IsEnabled = isEnabled },
            ["System"] = isEnabled => new global::Fdw.Services.Notifications.System.SystemNotificationConfiguration { IsEnabled = isEnabled },
            ["Teams"] = isEnabled => new global::FdwReferenceNotifications.Teams.TeamsNotificationConfiguration { IsEnabled = isEnabled },
            ["Webhook"] = isEnabled => new global::FdwReferenceNotifications.Webhook.WebhookNotificationConfiguration { IsEnabled = isEnabled },
        };

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
        var implementation = request.NotificationType;
        if (string.IsNullOrEmpty(implementation) || !Builders.TryGetValue(implementation, out var build))
        {
            return GenericResult<NotificationDetailDto>.Failure(
                NotificationEndpointLog.UnknownNotificationImplementation(
                    Logger, request.Name, implementation ?? string.Empty));
        }

        // Why a builder per implementation rather than one flat record: Notification is a domain with
        // several implementations, each with its own row and settings, so "create a notification of
        // type X" has to build X's record. A registry keyed by the discriminator is the sanctioned
        // alternative to a type switch -- the same shape GetConnectionEndpoint uses for its mappers.
        //
        // Name, Domain and Implementation are not set here: Save stamps them from its own arguments,
        // and the implementation row does not persist them -- they belong to the domain row.
        var config = build(request.IsEnabled!.Value);

        var saveResult = await _provider
            .Save(config, "Notification", implementation, request.Name, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<NotificationDetailDto>();
        }

        return GenericResult<NotificationDetailDto>.Success(new NotificationDetailDto
        {
            Id = config.Id,
            Name = request.Name,
            Implementation = implementation,
            // Description is a notify.Notification domain-row column; the implementation row does not
            // carry it, so the response echoes what the caller asked for.
            Description = request.Description,
            IsEnabled = request.IsEnabled!.Value
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
