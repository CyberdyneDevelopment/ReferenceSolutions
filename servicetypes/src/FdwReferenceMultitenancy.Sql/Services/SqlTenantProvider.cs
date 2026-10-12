using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceMultitenancy.Sql.Services;

/// <summary>
/// SQL Server-backed tenant provider using the schema-defined configuration gateway.
/// </summary>
public sealed class SqlTenantProvider : ITenantProvider
{
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlTenantProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTenantProvider"/> class.
    /// </summary>
    /// <param name="configurationGateways">Supplies the configuration gateway for tenant data access.</param>
    /// <param name="configuration">The tenant provider configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SqlTenantProvider(
        IConfigurationGatewayProvider configurationGateways,
        SqlTenantConfiguration configuration,
        ILogger<SqlTenantProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configurationGateways);
        ArgumentNullException.ThrowIfNull(configuration);

        _configurationGateways = configurationGateways;
        _configuration = configuration;
        _logger = logger ?? NullLogger<SqlTenantProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Where("Id", tenantId)
            .Build();

        var result = await Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

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
        return tenant;
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

        var result = await Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

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
        return tenant;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Where("IsActive", true)
            .Build();

        var result = await Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

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
            if (tenant.IsFailure)
                return tenant.ToNewResult<IEnumerable<ITenant>>();
            tenants.Add(tenant.Value!);
        }

        return GenericResult<IEnumerable<ITenant>>.Success(tenants);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetAllTenants(CancellationToken ct = default)
    {
        var command = Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
            .Build();

        var result = await Execute<IEnumerable<SqlTenantEntity>>(command, ct).ConfigureAwait(false);

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
            if (tenant.IsFailure)
                return tenant.ToNewResult<IEnumerable<ITenant>>();
            tenants.Add(tenant.Value!);
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

        // The one answer to "may this user act in this tenant" (FDW-825) -- asked by token issuance
        // eligibility and by the tenant switch alike. The tenant must exist and be active, and the
        // user must be a member of it, or a member of the global tenant, which may operate as any
        // tenant. IsSelectable is not part of the answer: it tells a UI which tenants to offer,
        // and is not an access rule.
        var tenants = await Execute<IEnumerable<SqlTenantEntity>>(
            Query.From<SqlTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantsTableName!)
                .Build(),
            ct).ConfigureAwait(false);
        if (!tenants.IsSuccess)
        {
            return GenericResult<bool>.Failure(
                SqlTenantResultCodes.ByName("TenantAccessValidationFailed"),
                ResultDetails.Create()
                    .With("UserId", userId)
                    .With("Error", tenants.CurrentMessage ?? "the tenants could not be read"));
        }

        if (tenants.Value is null)
        {
            return GenericResult<bool>.Failure(
                SqlTenantResultCodes.ByName("TenantAccessValidationFailed"),
                ResultDetails.Create()
                    .With("UserId", userId)
                    .With("Error", "the tenants read returned no result"));
        }

        var allTenants = tenants.Value.ToList();
        if (allTenants.FirstOrDefault(t => t.Id == tenantId) is not { IsActive: true })
        {
            return GenericResult<bool>.Success(false);
        }

        // Memberships are the implementation rows -- the domain table carries only the member's
        // name. Current rows only: a removed membership is a revoked one.
        var memberships = await Execute<IEnumerable<UserTenantEntity>>(
            Query.From<UserTenantEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.UserTenantsTableName!)
                .Where("UserId", userIdGuid)
                .Where("IsCurrent", true)
                .Where("IsDeleted", false)
                .Build(),
            ct).ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            return GenericResult<bool>.Failure(
                SqlTenantResultCodes.ByName("TenantAccessValidationFailed"),
                ResultDetails.Create()
                    .With("UserId", userId)
                    .With("Error", memberships.CurrentMessage ?? "the memberships could not be read"));
        }

        var memberOf = memberships.Value.Select(m => m.TenantId).ToHashSet();
        return GenericResult<bool>.Success(
            memberOf.Contains(tenantId)
            || allTenants.Any(t => t.IsGlobal && memberOf.Contains(t.Id)));
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
    private async Task<IGenericResult<ITenant>> EnrichTenantEntity(SqlTenantEntity entity, CancellationToken ct)
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
        var features = await LoadTenantFeatures(entity.Id, options, ct).ConfigureAwait(false);
        if (features.IsFailure)
            return features.ToNewResult<ITenant>();

        // Why: per-tenant custom settings are owned by the Settings domain (settings.TenantSetting,
        // read via SettingsConfigurationProvider) — NOT a tenant-schema table. The former
        // LoadTenantSettings read a non-existent 'tenant.TenantSettings' container (ContainerNotFound
        // noise on every request) into a bag no consumer reads. Removed rather than duplicating
        // Settings-domain data across the multitenancy boundary.

        // Load roles
        var roles = await GetTenantRoles(entity.Id, ct).ConfigureAwait(false);

        if (roles.IsFailure)
            return roles.ToNewResult<ITenant>();

        return GenericResult<ITenant>.Success(SqlTenant.FromEntity(entity, theme, options, roles.Value!));
    }

    private async Task<IGenericResult<T>> Execute<T>(DataGatewayCall command, CancellationToken ct)
    {
        var gateway = _configurationGateways.Get(_configuration.DataStoreName!);
        if (gateway.IsFailure)
            return gateway.ToNewResult<T>();

        return await gateway.Value!.Execute<T>(command, ct).ConfigureAwait(false);
    }
    private async Task<IGenericResult> LoadTenantFeatures(Guid tenantId, TenantOptions options, CancellationToken ct)
    {
        var command = Query.From<TenantFeatureEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantFeaturesTableName!)
            .Where("TenantId", tenantId)
            .Build();

        var result = await Execute<IEnumerable<TenantFeatureEntity>>(command, ct).ConfigureAwait(false);

        if (result.IsFailure)
            return result;

        if (result.Value != null)
        {
            foreach (var feature in result.Value)
            {
                options.AddFeature(feature.Feature);
            }
        }
        return GenericResult.Success();
    }

    private async Task<IGenericResult<List<string>>> GetTenantRoles(Guid tenantId, CancellationToken ct)
    {
        var command = Query.From<TenantRoleEntity>(_configuration.DataStoreName!, _configuration.PathName!, _configuration.TenantRolesTableName!)
            .Where("TenantId", tenantId)
            .Build();

        var result = await Execute<IEnumerable<TenantRoleEntity>>(command, ct).ConfigureAwait(false);

        if (result.IsFailure)
            return result.ToNewResult<List<string>>();

        return GenericResult<List<string>>.Success(result.Value?.Select(r => r.Role).ToList() ?? []);
    }
}
