using System.Diagnostics.CodeAnalysis;

namespace Reference.Scheduler.Server.Configuration;

/// <summary>
/// Configuration for ETL dispatch service.
/// </summary>
[ExcludeFromCodeCoverage]
public class EtlDispatchConfiguration
{
    /// <summary>
    /// Gets or sets whether ETL dispatch is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the base URL of the ETL server.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5002";

    /// <summary>
    /// Gets or sets the timeout in seconds for dispatch requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
