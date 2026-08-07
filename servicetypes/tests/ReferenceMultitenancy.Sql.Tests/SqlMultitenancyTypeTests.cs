using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using ReferenceMultitenancy.Sql.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Extensions;
using Fdw.Services.Multitenancy.Sql.Logging;
using Fdw.Services.Multitenancy.Sql.Middleware;
using Fdw.Services.Multitenancy.Sql.Models;
using Fdw.Services.Multitenancy.Sql.Results;

namespace ReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Proves <see cref="SqlMultitenancyType"/>'s <see cref="SqlTenantConfiguration"/> resolution reads
/// <c>settings.SqlTenantProvider</c> through <see cref="IConfigurationGateway"/> at
/// <see cref="SqlMultitenancyType.Initialize"/> (post-Build fail-fast) — never from
/// <c>IConfiguration</c> "TenantProviders" — and fails loud, naming
/// <c>settings.SqlTenantProvider</c>, when the row is absent.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public class SqlMultitenancyTypeTests
{
    [Fact]
    public void InitializeWithOneRowPopulatesSqlTenantConfiguration()
    {
        var row = new SqlTenantConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "DefaultTenants",
            DataStoreName = "ConfigurationDb",
            PathName = "tenant",
            TenantsTableName = "Tenants",
            TenantFeaturesTableName = "TenantFeatures",
            TenantRolesTableName = "TenantRoles",
            UserTenantsTableName = "UserTenants"
        };
        var host = BuildHost(MockGatewayReturning(row));
        var option = new SqlMultitenancyType();

        Should.NotThrow(() => option.Initialize(host, null));

        var resolved = host.Services.GetRequiredService<SqlTenantConfiguration>();
        resolved.Name.ShouldBe("DefaultTenants");
        resolved.DataStoreName.ShouldBe("ConfigurationDb");
        resolved.PathName.ShouldBe("tenant");
        resolved.TenantsTableName.ShouldBe("Tenants");
        resolved.TenantFeaturesTableName.ShouldBe("TenantFeatures");
        resolved.TenantRolesTableName.ShouldBe("TenantRoles");
        resolved.UserTenantsTableName.ShouldBe("UserTenants");
    }

    [Fact]
    public void InitializeWithNoRowsThrowsNamingSettingsSqlTenantProvider()
    {
        var host = BuildHost(MockGatewayReturning());
        var option = new SqlMultitenancyType();

        var ex = Should.Throw<InvalidOperationException>(() => option.Initialize(host, null));

        ex.Message.ShouldContain("settings.SqlTenantProvider");
    }

    // Why: Register never registers IConfiguration at all — if resolving
    // SqlTenantConfiguration still touched the old IConfiguration:"TenantProviders" read, this
    // container would either throw resolving a missing IConfiguration dependency or silently see an
    // unbound section. Resolving succeeds purely off the gateway-backed provider, proving the read
    // path no longer goes through IConfiguration.
    [Fact]
    public void ResolvingSqlTenantConfigurationDoesNotRequireIConfiguration()
    {
        var row = new SqlTenantConfiguration
        {
            Name = "DefaultTenants",
            DataStoreName = "ConfigurationDb",
            PathName = "tenant"
        };
        var host = BuildHost(MockGatewayReturning(row));

        // Why the assertion changed: this used to prove independence from IConfiguration by asserting
        // the container had none. Register now takes IHostApplicationBuilder, which always carries a
        // Configuration, so absence is no longer expressible. The real invariant is that the value
        // comes from the gateway — asserted by resolving it while configuration holds no tenant section.
        Should.NotThrow(() => host.Services.GetRequiredService<SqlTenantConfiguration>());
        host.Services.GetRequiredService<SqlTenantConfiguration>().Name.ShouldBe("DefaultTenants");
        host.Services.GetRequiredService<IConfiguration>().GetSection("tenant").Exists().ShouldBeFalse();
    }

    // Why: regression guard — SqlTenantProvider's constructor must keep taking the concrete
    // SqlTenantConfiguration (not an abstraction/factory); Register resolves it
    // straight off DI (host.Services.GetRequiredService<SqlTenantConfiguration>()) unchanged.
    [Fact]
    public void SqlTenantProviderConstructorStillTakesConcreteSqlTenantConfiguration()
    {
        var dataGateway = new Mock<IDataGateway>().Object;
        var config = new SqlTenantConfiguration
        {
            DataStoreName = "ConfigurationDb",
            PathName = "tenant",
            TenantsTableName = "Tenants"
        };

        var provider = new SqlTenantProvider(dataGateway, config, null);

        provider.ShouldNotBeNull();
    }

    // Why: regression guard — SqlOrganizationProvider's constructor must keep taking the concrete
    // SqlTenantConfiguration unchanged (see SqlMultitenancyType.Register).
    [Fact]
    public void SqlOrganizationProviderConstructorStillTakesConcreteSqlTenantConfiguration()
    {
        var dataGateway = new Mock<IDataGateway>().Object;
        var config = new SqlTenantConfiguration
        {
            DataStoreName = "ConfigurationDb",
            PathName = "tenant"
        };

        var provider = new SqlOrganizationProvider(dataGateway, config, null);

        provider.ShouldNotBeNull();
    }

    // Why the host and not services.BuildServiceProvider(): phase 3 takes the IHost now, and building
    // the container off the same builder keeps the provider the tests resolve from and the one the
    // option initializes against the same object.
    private static IHost BuildHost(IConfigurationGateway gateway)
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddSingleton(new Lazy<IConfigurationGateway>(() => gateway));
        var option = new SqlMultitenancyType();
        option.Register(builder, loggerFactory: null, "ConfigurationDb", "settings", "SqlTenantProvider");
        return builder.Build();
    }

    private static IConfigurationGateway MockGatewayReturning(params SqlTenantConfiguration[] rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<SqlTenantConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantConfiguration>>.Success(rows));
        return gateway.Object;
    }
}
