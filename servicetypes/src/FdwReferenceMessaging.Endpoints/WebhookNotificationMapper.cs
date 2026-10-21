using FdwReferenceNotifications.Webhook;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Moves a Webhook notification service between the request, the stored record and what is read back (FDW-1909).</summary>
internal static class WebhookNotificationMapper
{
    /// <summary>The notification domain the service belongs to.</summary>
    internal const string Domain = "Notification";

    /// <summary>The implementation the service is.</summary>
    internal const string Implementation = "Webhook";

    /// <summary>Copies the stated service onto a record.</summary>
    /// <param name="request">The stated service; the validator has refused one that leaves a field out.</param>
    /// <param name="target">The record to write.</param>
    internal static void Apply(WebhookNotificationRequest request, WebhookNotificationConfiguration target)
    {
        // Why the unwrapping: the validator refuses a request that does not state these, and nothing reaches here without passing it.
        target.IsEnabled = request.IsEnabled!.Value;
        target.Url = request.Url;
        target.Method = request.Method;
        target.ContentType = request.ContentType;
        target.PayloadTemplate = request.PayloadTemplate;
        target.RetryCount = request.RetryCount!.Value;
        target.TimeoutSeconds = request.TimeoutSeconds!.Value;
        target.SecretManagerName = request.SecretManagerName;
        target.SecretKeyName = request.SecretKeyName;
    }

    /// <summary>Reads a record back as the service it is.</summary>
    /// <param name="name">The service's name.</param>
    /// <param name="record">The stored record.</param>
    /// <returns>The service, carrying the names of its secret and never the secret.</returns>
    internal static WebhookNotificationDetail Detail(string name, WebhookNotificationConfiguration record) => new()
    {
        Id = record.Id,
        Name = name,
        IsEnabled = record.IsEnabled,
        Url = record.Url,
        Method = record.Method,
        ContentType = record.ContentType,
        PayloadTemplate = record.PayloadTemplate,
        RetryCount = record.RetryCount,
        TimeoutSeconds = record.TimeoutSeconds,
        SecretManagerName = record.SecretManagerName,
        SecretKeyName = record.SecretKeyName,
    };
}
