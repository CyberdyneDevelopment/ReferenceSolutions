using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Models;
using ReferenceMultitenancy.Sql.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceMultitenancy.Sql;
using ReferenceMultitenancy.Sql.Logging;
using ReferenceMultitenancy.Sql.Middleware;
using Fdw.Services.Multitenancy;
using Fdw.Services;
using Fdw;

namespace ReferenceMultitenancy.Sql.Services;

/// <summary>
/// SQL Server-backed tenant provider using IDataGateway.
/// </summary>
public sealed class SqlTenantProvider : ITenantProvider
{
    private readonly IDataGateway _dataGateway;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlTenantProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTenantProvider"/> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for database access.</param>
    /// <param name="configuration">The tenant provider configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SqlTenantProvider(
        IDataGateway dataGateway,
        SqlTenantConfiguration configuration,
        ILogger<SqlTenantProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataGateway);
        ArgumentNullException.ThrowIfNull(configuration);

        _dataGateway = dataGateway;
        _configuration = configuration;
        _logger = logger ?? NullLogger<SqlTenantProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Where("Id", tenantId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<ITenant>.Failure(
                SqlTenantResultCodes.ByName("TenantQueryFailed"),
                ResultDetails.Create()
                    .With("TenantId", tenantId.ToString())
                    .With("Error", result.CurrentMessage ?? "Unknown error"));
        }

        var entity = result.Value?.FirstOrDefault();
        if (entity == null)
        {
            return GenericResult<ITenant>.Failure(
                SqlTenantResultCodes.ByName("TenantNotFound"),
                ResultDetails.Create().With("TenantId", tenantId.ToString()));
        }

        var tenant = await EnrichTenantEntity(entity, ct).ConfigureAwait(false);
        return GenericResult<ITenant>.Success(tenant);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ITenant>> GetTenantBySlug(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return GenericResult<ITenant>.Failure(SqlTenantResultCodes.ByName("TenantSlugRequired"));
        }

        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Where("Slug", slug)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<ITenant>.Failure(
                SqlTenantResultCodes.ByName("TenantSlugQueryFailed"),
                ResultDetails.Create()
                    .With("Slug", slug)
                    .With("Error", result.CurrentMessage ?? "Unknown error"));
        }

        var entity = result.Value?.FirstOrDefault();
        if (entity == null)
        {
            return GenericResult<ITenant>.Failure(
                SqlTenantResultCodes.ByName("TenantSlugNotFound"),
                ResultDetails.Create().With("Slug", slug));
        }

        var tenant = await EnrichTenantEntity(entity, ct).ConfigureAwait(false);
        return GenericResult<ITenant>.Success(tenant);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Where("IsActive", true)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IEnumerable<ITenant>>.Failure(
                SqlTenantResultCodes.ByName("ActiveTenantsQueryFailed"),
                ResultDetails.Create().With("Error", result.CurrentMessage ?? "Unknown error"));
        }

        var tenants = new List<ITenant>();
        foreach (var entity in result.Value!)
        {
            var tenant = await EnrichTenantEntity(entity, ct).ConfigureAwait(false);
            tenants.Add(tenant);
        }

        return GenericResult<IEnumerable<ITenant>>.Success(tenants);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetAllTenants(CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IEnumerable<ITenant>>.Failure(
                SqlTenantResultCodes.ByName("AllTenantsQueryFailed"),
                ResultDetails.Create().With("Error", result.CurrentMessage ?? "Unknown error"));
        }

        var tenants = new List<ITenant>();
        foreach (var entity in result.Value!)
        {
            var tenant = await EnrichTenantEntity(entity, ct).ConfigureAwait(false);
            tenants.Add(tenant);
        }

        return GenericResult<IEnumerable<ITenant>>.Success(tenants);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> ValidateTenantAccess(
        Guid tenantId,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return GenericResult<bool>.Failure(SqlTenantResultCodes.ByName("UserIdRequired"));
        }

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            return GenericResult<bool>.Failure(
                SqlTenantResultCodes.ByName("InvalidUserIdFormat"),
                ResultDetails.Create().With("UserId", userId));
        }

        var command = Query.From<UserTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.UserTenantsTableName!)
            .Where("UserId", userIdGuid)
            .Where("TenantId", tenantId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<UserTenantEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<bool>.Failure(
                SqlTenantResultCodes.ByName("TenantAccessValidationFailed"),
                ResultDetails.Create()
                    .With("UserId", userId)
                    .With("Error", result.CurrentMessage ?? "Unknown error"));
        }

        return GenericResult<bool>.Success(result.Value?.Any() == true);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ITenant>> ResolveTenant(
        ITenantResolutionContext context,
        CancellationToken ct = default)
    {
        // Priority: Claims > Header > Route > Host
        if (context.ClaimsTenantId.HasValue)
        {
            return await GetTenant(context.ClaimsTenantId.Value, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(context.TenantHeader))
        {
            if (Guid.TryParse(context.TenantHeader, out var tenantId))
            {
                return await GetTenant(tenantId, ct).ConfigureAwait(false);
            }
            return await GetTenantBySlug(context.TenantHeader, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(context.RouteSlug))
        {
            return await GetTenantBySlug(context.RouteSlug, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(context.Host))
        {
            // Extract subdomain as tenant slug
            var hostParts = context.Host.Split('.');
            if (hostParts.Length >= 2)
            {
                var subdomain = hostParts[0];
                var result = await GetTenantBySlug(subdomain, ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    return result;
                }
            }
        }

        return GenericResult<ITenant>.Failure(SqlTenantResultCodes.ByName("TenantResolutionFailed"));
    }

    /// <summary>
    /// Enriches a tenant entity with features, settings, and roles from related tables.
    /// </summary>
    private async Task<SqlTenant> EnrichTenantEntity(SqlTenantEntity entity, CancellationToken ct)
    {
        // Create theme from entity properties
        var theme = new TenantTheme
        {
            PrimaryColor = entity.PrimaryColor ?? TenantTheme.Default.PrimaryColor,
            SecondaryColor = entity.SecondaryColor ?? TenantTheme.Default.SecondaryColor,
            AccentColor = entity.AccentColor ?? TenantTheme.Default.AccentColor,
            BackgroundColor = entity.BackgroundColor ?? TenantTheme.Default.BackgroundColor,
            SurfaceColor = entity.SurfaceColor ?? TenantTheme.Default.SurfaceColor,
            OverlayColor = entity.OverlayColor ?? TenantTheme.Default.OverlayColor,
            SuccessColor = entity.SuccessColor ?? TenantTheme.Default.SuccessColor,
            WarningColor = entity.WarningColor ?? TenantTheme.Default.WarningColor,
            ErrorColor = entity.ErrorColor ?? TenantTheme.Default.ErrorColor,
            InfoColor = entity.InfoColor ?? TenantTheme.Default.InfoColor,
            TextMainColor = entity.TextMainColor ?? TenantTheme.Default.TextMainColor,
            TextMutedColor = entity.TextMutedColor ?? TenantTheme.Default.TextMutedColor,
            LogoUrl = entity.LogoUrl,
            FaviconUrl = entity.FaviconUrl,
            CustomCssUrl = entity.CustomCssUrl,
            DarkModeDefault = entity.DarkModeDefault
        };

        // Create options from entity properties
        var options = new TenantOptions
        {
            MaxUsers = entity.MaxUsers,
            StorageQuotaBytes = entity.StorageQuotaBytes,
            ApiRateLimitPerMinute = entity.ApiRateLimitPerMinute
        };

        // Load features
        await LoadTenantFeatures(entity.Id, options, ct).ConfigureAwait(false);

        // Why: per-tenant custom settings are owned by the Settings domain (settings.TenantSetting,
        // read via SettingsConfigurationProvider) — NOT a tenant-schema table. The former
        // LoadTenantSettings read a non-existent 'tenant.TenantSettings' container (ContainerNotFound
        // noise on every request) into a bag no consumer reads. Removed rather than duplicating
        // Settings-domain data across the multitenancy boundary.

        // Load roles
        var roles = await GetTenantRoles(entity.Id, ct).ConfigureAwait(false);

        return SqlTenant.FromEntity(entity, theme, options, roles);
    }

    private async Task LoadTenantFeatures(Guid tenantId, TenantOptions options, CancellationToken ct)
    {
        var command = Query.From<TenantFeatureEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantFeaturesTableName!)
            .Where("TenantId", tenantId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<TenantFeatureEntity>>(command, ct).ConfigureAwait(false);

        if (result.IsSuccess && result.Value != null)
        {
            foreach (var feature in result.Value)
            {
                options.AddFeature(feature.Feature);
            }
        }
    }

    private async Task<List<string>> GetTenantRoles(Guid tenantId, CancellationToken ct)
    {
        var command = Query.From<TenantRoleEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantRolesTableName!)
            .Where("TenantId", tenantId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<TenantRoleEntity>>(command, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value == null)
        {
            return [];
        }

        return result.Value.Select(r => r.Role).ToList();
    }
}
