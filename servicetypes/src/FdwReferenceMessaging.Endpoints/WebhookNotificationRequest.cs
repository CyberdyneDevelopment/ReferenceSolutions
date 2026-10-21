namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// States a Webhook notification service whole, to make it or to change it (FDW-1909). Nothing is defaulted: a field the service needs is
/// stated, and a request that leaves one out is refused naming it.
/// </summary>
public sealed class WebhookNotificationRequest
{
    /// <summary>Gets or sets the service's name; on a change it is taken from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the service sends: stated, so a service is never on or off by an initializer.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the address called, an http or https address.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets the HTTP method used: POST, PUT or PATCH.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the content type of the body sent.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the body template, with <c>{subject}</c>, <c>{body}</c>, <c>{type}</c> and <c>{timestamp}</c>; when none, a JSON message is sent.</summary>
    public string? PayloadTemplate { get; set; }

    /// <summary>Gets or sets how many times the call is retried; stored, and not yet used by the sender.</summary>
    public int? RetryCount { get; set; }

    /// <summary>Gets or sets the seconds the call may take.</summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds the secret sent in <c>X-Webhook-Secret</c>; stated with <see cref="SecretKeyName"/> or not at all.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key of that secret in the secret manager; stated with <see cref="SecretManagerName"/> or not at all.</summary>
    public string? SecretKeyName { get; set; }
}
