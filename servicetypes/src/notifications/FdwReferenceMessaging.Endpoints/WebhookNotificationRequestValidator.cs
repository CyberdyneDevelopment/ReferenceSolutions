using System;
using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Refuses a Webhook notification service that is not stated whole, naming the field (FDW-1909).</summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookNotificationRequestValidator : Validator<WebhookNotificationRequest>
{
    private static readonly string[] Methods = ["POST", "PUT", "PATCH"];

    /// <summary>Initializes a new instance of the <see cref="WebhookNotificationRequestValidator"/> class.</summary>
    public WebhookNotificationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.IsEnabled)
            .NotNull().WithMessage("IsEnabled is required: state true to send or false to keep the service switched off");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required: the address the service calls")
            .Must(IsHttpAddress).When(x => !string.IsNullOrWhiteSpace(x.Url), ApplyConditionTo.CurrentValidator).WithMessage("Url must be an absolute http or https address");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method is required: POST, PUT or PATCH")
            .Must(method => Array.Exists(Methods, allowed => string.Equals(allowed, method, StringComparison.OrdinalIgnoreCase)))
            .When(x => !string.IsNullOrWhiteSpace(x.Method), ApplyConditionTo.CurrentValidator).WithMessage("Method must be POST, PUT or PATCH");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required, for example application/json");

        RuleFor(x => x.RetryCount)
            .NotNull().WithMessage("RetryCount is required")
            .GreaterThanOrEqualTo(0).WithMessage("RetryCount must not be negative");

        RuleFor(x => x.TimeoutSeconds)
            .NotNull().WithMessage("TimeoutSeconds is required")
            .GreaterThan(0).WithMessage("TimeoutSeconds must be more than zero");

        RuleFor(x => x.SecretKeyName)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.SecretManagerName))
            .WithMessage("SecretKeyName is required when SecretManagerName is stated: a secret is named by both or by neither");

        RuleFor(x => x.SecretManagerName)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.SecretKeyName))
            .WithMessage("SecretManagerName is required when SecretKeyName is stated: a secret is named by both or by neither");
    }

    private static bool IsHttpAddress(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
