using Fdw.Hosting.Abstractions.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReferenceMultitenancy.Sql;
using ReferenceMultitenancy.Sql.Middleware;
using ReferenceMultitenancy.Sql.Models;
using ReferenceMultitenancy.Sql.Results;
using Fdw.Services.Multitenancy;
using Fdw.Services;
using Fdw;

namespace ReferenceMultitenancy.Sql.Configuration;

/// <summary>
/// Tenant-aware implementation of <see cref="IConfigurationConnectionNameProvider"/>.
/// Checks <see cref="ITenantContext.ConnectionName"/> first, then falls back to
/// <see cref="ConfigurationConnectionOptions.ConnectionName"/>.
/// </summary>
public sealed class TenantAwareConfigurationConnectionNameProvider : IConfigurationConnectionNameProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly IOptionsMonitor<ConfigurationConnectionOptions> _options;
    private readonly ILogger<TenantAwareConfigurationConnectionNameProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareConfigurationConnectionNameProvider"/> class.
    /// </summary>
    /// <param name="tenantContext">The current tenant context.</param>
    /// <param name="options">The configuration connection options.</param>
    /// <param name="logger">Optional logger instance.</param>
    public TenantAwareConfigurationConnectionNameProvider(
        ITenantContext tenantContext,
        IOptionsMonitor<ConfigurationConnectionOptions> options,
        ILogger<TenantAwareConfigurationConnectionNameProvider>? logger = null)
    {
        _tenantContext = tenantContext;
        _options = options;
        _logger = logger ?? NullLogger<TenantAwareConfigurationConnectionNameProvider>.Instance;
    }

    /// <inheritdoc />
    public string ConnectionName
    {
        get
        {
            if (_tenantContext.HasTenant &&
                !string.IsNullOrEmpty(_tenantContext.ConnectionName))
            {
                TenantMiddlewareLog.UsingTenantConnectionKey(
                    _logger, _tenantContext.TenantSlug!, _tenantContext.ConnectionName!);
                return _tenantContext.ConnectionName!;
            }

            // Why: ConfigurationConnectionOptions.ConnectionName is required — no silent "ConfigurationDb" fallback.
            // If the value is missing the system cannot route config queries; fail loud so the operator
            // sees a structured error at request time rather than a confusing connection-not-found failure later.
            if (string.IsNullOrEmpty(_options.CurrentValue.ConnectionName))
            {
                TenantMiddlewareLog.ConfigurationConnectionNameMissing(_logger);
                throw new System.InvalidOperationException(
                    "ConfigurationConnectionOptions.ConnectionName is not configured. " +
                    "Ensure the FdwHost:Configuration:ConnectionName setting is present.");
            }

            TenantMiddlewareLog.UsingDefaultConnectionKey(_logger, _options.CurrentValue.ConnectionName);
            return _options.CurrentValue.ConnectionName;
        }
    }
}
