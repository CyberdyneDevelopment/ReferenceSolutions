using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Logging;
using FdwReferenceMultitenancy.Sql.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceMultitenancy.Sql.Services;

/// <summary>
/// SQL-backed organization provider. Queries <c>tenant.Organization</c> via the configuration gateway
/// and maps rows to <see cref="OrganizationConfiguration"/>.
/// </summary>
public sealed class SqlOrganizationProvider : IOrganizationProvider
{
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlOrganizationProvider> _logger;

    /// <summary>Initializes a new instance of <see cref="SqlOrganizationProvider"/>.</summary>
    public SqlOrganizationProvider(
        IConfigurationGatewayProvider configurationGateways,
        SqlTenantConfiguration configuration,
        ILogger<SqlOrganizationProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configurationGateways);
        ArgumentNullException.ThrowIfNull(configuration);

        _configurationGateways = configurationGateways;
        _configuration = configuration;
        _logger = logger ?? NullLogger<SqlOrganizationProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationConfiguration>> Get(
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        var command = Query.From<SqlOrganizationEntity>(
                _configuration.DataStoreName!,
                _configuration.PathName!,
                "Organization")
            .Where("Id", orgId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var gateway = _configurationGateways.Get(_configuration.DataStoreName!);
        if (gateway.IsFailure)
            return gateway.ToNewResult<OrganizationConfiguration>();

        var result = await gateway.Value!.Execute<IEnumerable<SqlOrganizationEntity>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<OrganizationConfiguration>();
        }

        var entity = result.Value?.FirstOrDefault();
        if (entity is null)
        {
            return GenericResult<OrganizationConfiguration>.Failure(
                TenantMiddlewareLog.OrgFromJwtClaimNotFound(_logger, orgId));
        }

        return GenericResult<OrganizationConfiguration>.Success(entity.ToConfiguration());
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationConfiguration>> GetDefault(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var command = Query.From<SqlOrganizationEntity>(
                _configuration.DataStoreName!,
                _configuration.PathName!,
                "Organization")
            .Where("TenantId", tenantId)
            .Where("IsDefault", true)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var gateway = _configurationGateways.Get(_configuration.DataStoreName!);
        if (gateway.IsFailure)
            return gateway.ToNewResult<OrganizationConfiguration>();

        var result = await gateway.Value!.Execute<IEnumerable<SqlOrganizationEntity>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<OrganizationConfiguration>();
        }

        var entity = result.Value?.FirstOrDefault();
        if (entity is null)
        {
            return GenericResult<OrganizationConfiguration>.Failure(
                TenantMiddlewareLog.NoDefaultOrgForTenant(_logger, tenantId));
        }

        return GenericResult<OrganizationConfiguration>.Success(entity.ToConfiguration());
    }
}
