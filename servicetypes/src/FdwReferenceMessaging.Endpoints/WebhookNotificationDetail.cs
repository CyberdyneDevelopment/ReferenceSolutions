using System;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// A Webhook notification service as it is read back (FDW-1909). It carries the NAMES of the secret manager and key, never the secret.
/// </summary>
public sealed class WebhookNotificationDetail
{
    /// <summary>Gets or sets the service's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the service's name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets whether the service sends.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the address called.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets the HTTP method used.</summary>
    public required string Method { get; set; }

    /// <summary>Gets or sets the content type of the body sent.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the body template, when there is one.</summary>
    public string? PayloadTemplate { get; set; }

    /// <summary>Gets or sets how many times the call is retried.</summary>
    public int RetryCount { get; set; }

    /// <summary>Gets or sets the seconds the call may take.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds the secret, when the service sends one.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key of the secret in that secret manager.</summary>
    public string? SecretKeyName { get; set; }
}
