using System;
using Fdw.Hosting.Abstractions.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Configuration;
using Microsoft.Extensions.Options;
using Fdw;
using Fdw.Services;
using Fdw.Services.Multitenancy;
using ReferenceMultitenancy.Sql;
using ReferenceMultitenancy.Sql.Logging;
using ReferenceMultitenancy.Sql.Middleware;
using ReferenceMultitenancy.Sql.Models;
using ReferenceMultitenancy.Sql.Results;

namespace ReferenceMultitenancy.Sql.Tests.Configuration;

/// <summary>
/// Tests for <see cref="TenantAwareConfigurationConnectionNameProvider"/>. Covers the
/// tenant-override happy path, the fall-through to <see cref="ConfigurationConnectionOptions"/>
/// when no tenant override is present, and the anti-fallback fail-loud throw when neither
/// source supplies a connection name.
/// </summary>
public sealed class TenantAwareConfigurationConnectionNameProviderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConnectionNameTenantHasConnectionNameOverrideReturnsTenantConnectionName()
    {
        // Arrange
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.ConnectionName).Returns("TenantSpecificDb");
        tenantContextMock.Setup(t => t.TenantSlug).Returns("acme");
        var optionsMock = CreateOptionsMonitor(new ConfigurationConnectionOptions { ConnectionName = "ConfigurationDb" });
        var provider = new TenantAwareConfigurationConnectionNameProvider(tenantContextMock.Object, optionsMock.Object, null);

        // Act
        var result = provider.ConnectionName;

        // Assert
        result.ShouldBe("TenantSpecificDb");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(null)]
    [InlineData("")]
    public void ConnectionNameTenantConnectionNameMissingFallsBackToOptions(string? tenantConnectionName)
    {
        // Arrange
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.ConnectionName).Returns(tenantConnectionName);
        tenantContextMock.Setup(t => t.TenantSlug).Returns("acme");
        var optionsMock = CreateOptionsMonitor(new ConfigurationConnectionOptions { ConnectionName = "ConfigurationDb" });
        var provider = new TenantAwareConfigurationConnectionNameProvider(tenantContextMock.Object, optionsMock.Object, null);

        // Act
        var result = provider.ConnectionName;

        // Assert
        result.ShouldBe("ConfigurationDb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConnectionNameNoTenantContextUsesOptionsConnectionName()
    {
        // Arrange
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(false);
        var optionsMock = CreateOptionsMonitor(new ConfigurationConnectionOptions { ConnectionName = "ConfigurationDb" });
        var provider = new TenantAwareConfigurationConnectionNameProvider(tenantContextMock.Object, optionsMock.Object, null);

        // Act
        var result = provider.ConnectionName;

        // Assert
        result.ShouldBe("ConfigurationDb");
        tenantContextMock.Verify(t => t.ConnectionName, Times.Never);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(null)]
    [InlineData("")]
    public void ConnectionNameOptionsConnectionNameMissingThrowsInvalidOperationException(string? optionsConnectionName)
    {
        // Arrange — anti-fallback: when neither the tenant override nor
        // ConfigurationConnectionOptions.ConnectionName supplies a value, the system cannot
        // route config queries and must fail loud rather than default to "ConfigurationDb".
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(false);
        var optionsMock = CreateOptionsMonitor(new ConfigurationConnectionOptions { ConnectionName = optionsConnectionName });
        var provider = new TenantAwareConfigurationConnectionNameProvider(tenantContextMock.Object, optionsMock.Object, null);

        // Act
        var act = () => provider.ConnectionName;

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("ConfigurationConnectionOptions.ConnectionName is not configured.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ConnectionNameTenantHasTenantButNoOverrideAndOptionsMissingThrowsInvalidOperationException()
    {
        // Arrange — a tenant is active but supplies no connection override; the root options
        // path must still fail loud rather than silently default.
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.ConnectionName).Returns((string?)null);
        var optionsMock = CreateOptionsMonitor(new ConfigurationConnectionOptions { ConnectionName = null });
        var provider = new TenantAwareConfigurationConnectionNameProvider(tenantContextMock.Object, optionsMock.Object, null);

        // Act
        var act = () => provider.ConnectionName;

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    private static Mock<IOptionsMonitor<ConfigurationConnectionOptions>> CreateOptionsMonitor(ConfigurationConnectionOptions options)
    {
        var mock = new Mock<IOptionsMonitor<ConfigurationConnectionOptions>>();
        mock.Setup(o => o.CurrentValue).Returns(options);
        return mock;
    }
}
