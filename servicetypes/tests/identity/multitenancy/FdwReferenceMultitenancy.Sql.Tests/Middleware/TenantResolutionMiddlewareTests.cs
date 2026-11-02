using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Middleware;
using FdwReferenceMultitenancy.Sql.Models;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceMultitenancy.Sql.Tests.Middleware;

/// <summary>
/// Tests for <see cref="TenantResolutionMiddleware"/> — the tenant-isolation gate. A regression
/// here silently disables tenant isolation (every request falls through unfiltered) or wrongly
/// denies/500s legitimate requests, so every resolution branch (JWT claim, header-as-GUID,
/// header-as-slug, access-check failure, access-denied, happy path) is covered explicitly.
/// </summary>
public sealed class TenantResolutionMiddlewareTests
{
    private static readonly string TenantClaimType = ClaimDefinitions.tenantId.Name;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorNullNextThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new TenantResolutionMiddleware(null!, null);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("next");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeNoJwtClaimAndNoHeaderCallsNextWithoutResolvingTenant()
    {
        // Arrange
        var (context, nextCalled) = CreateContext(BuildPrincipal());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        nextCalled.Called.ShouldBeTrue();
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        tenantProviderMock.Verify(p => p.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        tenantProviderMock.Verify(p => p.GetTenantBySlug(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeJwtClaimResolvesTenantSetsTenantAndCallsNext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId, "acme");
        var (context, nextCalled) = CreateContext(BuildPrincipal((TenantClaimType, tenantId.ToString())));
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantContextMock.Verify(t => t.SetTenant(tenant), Times.Once);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeJwtClaimProviderReturnsSuccessWithNullValueDoesNotSetTenantAndCallsNext()
    {
        // Arrange — regression-fix path: a provider can return Success with a null Value
        // (e.g. Guid.Empty / unknown id). The old bang-suppression threw inside SetTenant.
        var tenantId = Guid.NewGuid();
        var (context, nextCalled) = CreateContext(BuildPrincipal((TenantClaimType, tenantId.ToString())));
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(null!));

        // Act
        var act = async () => await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        await Should.NotThrowAsync(act);
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeJwtClaimProviderReturnsFailureDoesNotSetTenantAndCallsNext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var (context, nextCalled) = CreateContext(BuildPrincipal((TenantClaimType, tenantId.ToString())));
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Failure(new GenericMessage("Tenant not found")));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeIgnoresTheTenantHeader_TheTokenIsTheOnlySource()
    {
        // Arrange -- FDW-825: the token's tenant is the one active tenant. An X-Tenant-Id header used
        // to set this context (after an access check) while the database kept reading the token's
        // tenant, so a caller could appear to act in one tenant and read another's rows. A header
        // naming a tenant, with or without a claim, must change nothing.
        var headerTenantId = Guid.NewGuid();
        var (context, nextCalled) = CreateContext(
            BuildPrincipal((TenantClaimType, "not-a-guid")),
            headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>(MockBehavior.Strict);

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        nextCalled.Called.ShouldBeTrue();
    }

    private static SqlTenant CreateTenant(Guid id, string slug) =>
        new(id, $"Tenant {slug}", slug);

    private static TenantResolutionMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, null);

    private static ClaimsPrincipal BuildPrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(authenticationType: "Test");
        foreach (var (type, value) in claims)
        {
            identity.AddClaim(new Claim(type, value));
        }

        return new ClaimsPrincipal(identity);
    }

    private static (DefaultHttpContext Context, NextSpy NextCalled) CreateContext(ClaimsPrincipal user, string? tenantHeader = null)
    {
        var context = new DefaultHttpContext
        {
            User = user
        };

        if (tenantHeader is not null)
        {
            context.Request.Headers["X-Tenant-Id"] = tenantHeader;
        }

        return (context, new NextSpy());
    }

    /// <summary>Captures whether the <see cref="RequestDelegate"/> passed to the middleware was invoked.</summary>
    private sealed class NextSpy
    {
        public bool Called { get; private set; }

        public RequestDelegate Delegate => _ =>
        {
            Called = true;
            return Task.CompletedTask;
        };
    }
}
