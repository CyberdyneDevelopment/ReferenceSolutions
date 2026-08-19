using System;
using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Configuration;

/// <summary>
/// Downstream service endpoint URLs for API gateway proxying.
/// Loaded from appsettings.json "ServiceEndpoints" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class ServiceEndpointsOptions
{
    /// <summary>Name of the appsettings section these options bind from.</summary>
    public const string SectionName = "ServiceEndpoints";

    /// <summary>Base address of the scheduler service.</summary>
    public string Scheduler { get; set; } = string.Empty;

    /// <summary>Base address of the ETL service.</summary>
    public string Etl { get; set; } = string.Empty;
}
