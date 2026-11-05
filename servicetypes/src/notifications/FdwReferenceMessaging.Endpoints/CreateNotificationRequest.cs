using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FastEndpoints;
using FluentValidation;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Request body for POST /notifications.
/// </summary>
public sealed class CreateNotificationRequest
{
    /// <summary>Gets or sets the notification name (must be unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the service option type (e.g., "Console", "Email", "Webhook").</summary>
    public string? NotificationType { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the notification is enabled; left out, the caller's tenant's setting
    /// DefaultNotificationChannelIsEnabled gives it (FDW-3580).
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the secret manager the notification's secrets are named in; given with <see cref="SecretKeyName"/> or not at all.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key of the notification's secret in the secret manager; given with <see cref="SecretManagerName"/> or not at all.</summary>
    public string? SecretKeyName { get; set; }

    /// <summary>Gets or sets the type's settings by name, as <c>GET /notifications/types</c> serves them. A secret setting is not given here.</summary>
    public Dictionary<string, JsonElement> Settings { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Validator for <see cref="CreateNotificationRequest"/>.
/// Why: FastEndpoints scans the entry assembly; adding the validator here turns missing
/// required fields into structured 400 responses before the handler runs.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNotificationRequestValidator : Validator<CreateNotificationRequest>
{
    /// <inheritdoc />
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(1).WithMessage("Name must be at least 1 character")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.SecretKeyName)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.SecretManagerName))
            .WithMessage("SecretKeyName is required when SecretManagerName is given");

        RuleFor(x => x.SecretManagerName)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.SecretKeyName))
            .WithMessage("SecretManagerName is required when SecretKeyName is given");
    }
}
