using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;

namespace ReferenceNotifications.Console;

/// <summary>
/// Configuration for console/log notification services.
/// Minimal configuration — suitable for development and test environments.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "Console")]
public sealed partial class ConsoleNotificationConfiguration : NotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleNotificationConfiguration"/> class.
    /// </summary>
    public ConsoleNotificationConfiguration() : base("Notification", "Console", "Notifications:Console")
    {
    }

    // ========================================
    // Runtime fields (not on parent header)
    // Why: Polymorphic configuration pattern — parent is identity-only.
    // These fields are specific to the typed body and read by the factory at service construction time.
    // ========================================

    /// <summary>
    /// Gets or sets the service lifetime for DI registration.
    /// </summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>
    /// Gets or sets whether this notification channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the secret manager to use for retrieving secrets.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name within the secret manager to retrieve.
    /// </summary>
    public string? SecretKeyName { get; set; }

    // ========================================
    // Console-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the log level to use when emitting notification content.
    /// Defaults to Information.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
