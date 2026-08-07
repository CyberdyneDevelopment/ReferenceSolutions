using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Logging;
using Fdw.Services.Multitenancy.Sql.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceMultitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Extensions;
using Fdw.Services.Multitenancy.Sql.Middleware;
using Fdw.Services.Multitenancy.Sql.Results;
using Fdw.Services.Multitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Logging;
using Fdw.Services.Multitenancy;
using Fdw.Services;
using Fdw;

namespace ReferenceMultitenancy.Sql.Services;

/// <summary>
/// SQL-backed organization provider. Queries <c>tenant.Organizations</c> via IDataGateway
/// and maps rows to <see cref="OrganizationConfiguration"/>.
/// </summary>
public sealed class SqlOrganizationProvider : IOrganizationProvider
{
    private readonly IDataGateway _dataGateway;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlOrganizationProvider> _logger;

    /// <summary>Initializes a new instance of <see cref="SqlOrganizationProvider"/>.</summary>
    public SqlOrganizationProvider(
        IDataGateway dataGateway,
        SqlTenantConfiguration configuration,
        ILogger<SqlOrganizationProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataGateway);
        ArgumentNullException.ThrowIfNull(configuration);

        _dataGateway = dataGateway;
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
                "Organizations")
            .Where("Id", orgId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlOrganizationEntity>>(command, cancellationToken)
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
                "Organizations")
            .Where("TenantId", tenantId)
            .Where("IsDefault", true)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<SqlOrganizationEntity>>(command, cancellationToken)
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
