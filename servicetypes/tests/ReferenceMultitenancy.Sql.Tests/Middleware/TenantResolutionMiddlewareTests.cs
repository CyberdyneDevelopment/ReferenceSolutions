using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Middleware;
using ReferenceMultitenancy.Sql.Models;
using Microsoft.AspNetCore.Http;
using Fdw;
using Fdw.Services;
using Fdw.Services.Multitenancy;
using ReferenceMultitenancy.Sql;
using ReferenceMultitenancy.Sql.Logging;
using ReferenceMultitenancy.Sql.Results;

namespace ReferenceMultitenancy.Sql.Tests.Middleware;

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
    public async Task InvokeMalformedJwtClaimFallsBackToHeaderResolution()
    {
        // Arrange — a non-GUID tenantId claim must not short-circuit to ResolveFromJwtClaim;
        // resolution should fall through to the X-Tenant-Id header.
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        var (context, nextCalled) = CreateContext(
            BuildPrincipal((TenantClaimType, "not-a-guid")),
            headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantContextMock.Verify(t => t.SetTenant(tenant), Times.Once);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderGuidResolvesViaGetTenantByIdSetsTenantAndCallsNext()
    {
        // Arrange
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        var (context, nextCalled) = CreateContext(BuildPrincipal(), headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantProviderMock.Verify(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()), Times.Once);
        tenantProviderMock.Verify(p => p.GetTenantBySlug(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        tenantContextMock.Verify(t => t.SetTenant(tenant), Times.Once);
        nextCalled.Called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderSlugResolvesViaGetTenantBySlugSetsTenantAndCallsNext()
    {
        // Arrange
        const string slug = "acme-corp";
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId, slug);
        var (context, nextCalled) = CreateContext(BuildPrincipal(), slug);
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenantBySlug(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantProviderMock.Verify(p => p.GetTenantBySlug(slug, It.IsAny<CancellationToken>()), Times.Once);
        tenantProviderMock.Verify(p => p.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        tenantContextMock.Verify(t => t.SetTenant(tenant), Times.Once);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderTenantNotFoundCallsNextWithoutSettingTenant()
    {
        // Arrange — header parses as a GUID but the provider can't find it: resolvedTenant
        // stays null, ResolveFromHeader returns false (not denied), and the request proceeds
        // unfiltered (no tenant context set).
        var headerTenantId = Guid.NewGuid();
        var (context, nextCalled) = CreateContext(BuildPrincipal(), headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Failure(new GenericMessage("Tenant not found")));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        tenantProviderMock.Verify(p => p.ValidateTenantAccess(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        nextCalled.Called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeTenantAccessCheckFailsReturns500AndDoesNotCallNext()
    {
        // Arrange — ValidateTenantAccess itself fails (infrastructure error), not a denial.
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        var (context, nextCalled) = CreateContext(BuildPrincipal(), headerTenantId.ToString());
        context.Response.Body = new MemoryStream();
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(new GenericMessage("Access check failed")));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.ShouldBe("application/json");
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        nextCalled.Called.ShouldBeFalse();

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Tenant access check failed");
        body.ShouldContain(headerTenantId.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeTenantAccessDeniedReturns403AndDoesNotCallNext()
    {
        // Arrange
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        var (context, nextCalled) = CreateContext(BuildPrincipal(), headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(false));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        tenantContextMock.Verify(t => t.SetTenant(It.IsAny<ITenant>()), Times.Never);
        nextCalled.Called.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderResolutionPassesSubClaimAsUserIdToAccessCheck()
    {
        // Arrange
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        const string subject = "user-123";
        var (context, nextCalled) = CreateContext(BuildPrincipal(("sub", subject)), headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantProviderMock.Verify(p => p.ValidateTenantAccess(headerTenantId, subject, It.IsAny<CancellationToken>()), Times.Once);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderResolutionFallsBackToNameIdentifierClaimWhenSubMissing()
    {
        // Arrange
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        const string nameId = "user-456";
        var (context, nextCalled) = CreateContext(
            BuildPrincipal((ClaimTypes.NameIdentifier, nameId)),
            headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, nameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantProviderMock.Verify(p => p.ValidateTenantAccess(headerTenantId, nameId, It.IsAny<CancellationToken>()), Times.Once);
        nextCalled.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvokeHeaderResolutionPassesEmptyUserIdWhenNoIdentityClaimsPresent()
    {
        // Arrange — anonymous caller resolving by header: userId falls back to string.Empty.
        var headerTenantId = Guid.NewGuid();
        var tenant = CreateTenant(headerTenantId, "acme");
        var (context, nextCalled) = CreateContext(BuildPrincipal(), headerTenantId.ToString());
        var middleware = CreateMiddleware(nextCalled.Delegate);
        var tenantContextMock = new Mock<IMutableTenantContext>();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock
            .Setup(p => p.GetTenant(headerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant));
        tenantProviderMock
            .Setup(p => p.ValidateTenantAccess(headerTenantId, string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        // Act
        await middleware.Invoke(context, tenantContextMock.Object, tenantProviderMock.Object);

        // Assert
        tenantProviderMock.Verify(p => p.ValidateTenantAccess(headerTenantId, string.Empty, It.IsAny<CancellationToken>()), Times.Once);
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
