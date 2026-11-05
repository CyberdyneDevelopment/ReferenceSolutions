using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FastEndpoints;
using Fdw.Services.Notifications;
using FluentValidation;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Request body for POST /notifications/rules.
/// </summary>
public sealed class CreateNotificationRuleRequest
{
    /// <summary>Gets or sets the rule name (must be unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the rule is enabled; left out, the setting DefaultNotificationRuleIsEnabled gives it (FDW-3580).</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets how the rule's conditions combine: "And" or "Or"; left out, the setting DefaultNotificationRuleConditionOperator gives it (FDW-3580).</summary>
    public string? ConditionOperator { get; set; }

    /// <summary>Gets or sets the schedule ID this rule applies to.</summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>Gets or sets the pipeline ID this rule applies to.</summary>
    public Guid? PipelineId { get; set; }

    /// <summary>Gets or sets the workflow ID this rule applies to.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the notification service type (e.g., "Console", "Email").</summary>
    public string NotificationServiceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification service name (configured instance).</summary>
    public string NotificationServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the message template for the notification.</summary>
    public string? Template { get; set; }

    /// <summary>Gets or sets the severity, one of those <c>GET /notifications/rules/severities</c> serves. Required: a rule states its own.</summary>
    public string? Severity { get; set; }

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

        RuleFor(x => x.ConditionOperator)
            .Must(op => string.Equals(op, "And", StringComparison.Ordinal) || string.Equals(op, "Or", StringComparison.Ordinal))
            .When(x => !string.IsNullOrEmpty(x.ConditionOperator))
            .WithMessage(x => $"ConditionOperator '{x.ConditionOperator}' is not offered; the choices are And, Or");

        RuleFor(x => x.NotificationServiceType)
            .NotEmpty().WithMessage("NotificationServiceType is required");

        RuleFor(x => x.NotificationServiceName)
            .NotEmpty().WithMessage("NotificationServiceName is required");

        RuleFor(x => x.Severity)
            .NotEmpty().WithMessage("Severity is required")
            .Must(NotificationRuleSeverities.Offers)
            .When(x => !string.IsNullOrEmpty(x.Severity))
            .WithMessage(x => $"Severity '{x.Severity}' is not offered; the choices are {string.Join(", ", NotificationRuleSeverities.All().Select(pair => pair.Key))}");
    }
}
