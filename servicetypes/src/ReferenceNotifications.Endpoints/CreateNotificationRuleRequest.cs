using System;
using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Request body for POST /notifications/rules.
/// </summary>
public sealed class CreateNotificationRuleRequest
{
    /// <summary>Gets or sets the rule name (must be unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the schedule ID this rule applies to.</summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>Gets or sets the pipeline ID this rule applies to.</summary>
    public Guid? PipelineId { get; set; }

    /// <summary>Gets or sets the workflow ID this rule applies to.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets how multiple conditions are combined ("And" or "Or").</summary>
    public string ConditionOperator { get; set; } = "Or";

    /// <summary>Gets or sets the notification service type (e.g., "Console", "Email").</summary>
    public string NotificationServiceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification service name (configured instance).</summary>
    public string NotificationServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the message template for the notification.</summary>
    public string? Template { get; set; }

    /// <summary>Gets or sets the severity level ("Info", "Warning", "Error", "Critical").</summary>
    public string Severity { get; set; } = "Info";

    /// <summary>Gets or sets the minimum interval between notifications in minutes.</summary>
    public int? CooldownMinutes { get; set; }
}

/// <summary>
/// Validator for <see cref="CreateNotificationRuleRequest"/>.
/// Why: FastEndpoints scans the entry assembly; adding the validator here turns missing
/// required fields into structured 400 responses before the handler runs.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNotificationRuleRequestValidator : Validator<CreateNotificationRuleRequest>
{
    /// <inheritdoc />
    public CreateNotificationRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(1).WithMessage("Name must be at least 1 character")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.NotificationServiceType)
            .NotEmpty().WithMessage("NotificationServiceType is required");

        RuleFor(x => x.NotificationServiceName)
            .NotEmpty().WithMessage("NotificationServiceName is required");
    }
}
