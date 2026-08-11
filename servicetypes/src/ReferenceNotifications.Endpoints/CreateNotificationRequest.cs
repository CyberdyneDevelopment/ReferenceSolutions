using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;

namespace ReferenceNotifications.Endpoints;

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
    }
}
