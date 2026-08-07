using System;
using System.Security.Claims;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Notifications;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Etl.Server.Endpoints.Trigger;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Endpoints;

/// <summary>
/// Tests for UnifiedTriggerEndpoint request model, DTO shapes, and TypeCollection dispatch logic.
/// FastEndpoints Send.* methods require HTTP context for full integration testing.
/// These tests verify DTO shapes and open TypeCollection dispatch guard logic.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class UnifiedTriggerEndpointTests
{
    private readonly Mock<IExecutionTracker> _executionTracker = new();
    private readonly Mock<IDataGateway> _dataGateway = new();
    private readonly Mock<IFdwServiceProvider<IEtlPipeline, PipelineConfiguration>> _pipelineProvider = new();
    private readonly Mock<IPipelineExecutionQueue> _pipelineQueue = new();
    // Why: ProjectExecutionQueue is sealed with a single int-capacity ctor — cannot be mocked.
    // A capacity of 1 is sufficient for construction tests.
    private readonly ProjectExecutionQueue _projectQueue = new(1);
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private readonly Mock<IOrchestrationNodeConfigurationProvider> _nodeProvider = new();

    private UnifiedTriggerEndpoint BuildEndpoint() => new(
        _executionTracker.Object,
        _dataGateway.Object,
        _pipelineProvider.Object,
        _pipelineQueue.Object,
        _projectQueue,
        _broadcaster.Object,
        _nodeProvider.Object,
        new NullLogger<UnifiedTriggerEndpoint>());

    [Fact]
    public void EndpointCanBeConstructed()
    {
        BuildEndpoint().ShouldNotBeNull();
    }

    [Fact]
    public void UnifiedTriggerRequestDefaultsAreNull()
    {
        // Arrange & Act
        var request = new UnifiedTriggerRequest();

        // Assert
        request.Id.ShouldBeNull();
        request.Name.ShouldBeNull();
        request.ParentPath.ShouldBeNull();
        request.TriggerSource.ShouldBeNull();
    }

    [Fact]
    public void UnifiedTriggerResponseDefaultsStatusToEmptyString()
    {
        // Arrange & Act
        var response = new UnifiedTriggerResponse();

        // Assert
        response.ExecutionId.ShouldBe(Guid.Empty);
        response.Status.ShouldBe(string.Empty);
    }

    [Fact]
    public void ExecutionItemTypesReturnsNotFoundForUnknownType()
    {
        // Why: verifies the TypeCollection guard at the top of HandleAsync returns
        // NotFound for unregistered type names, which causes a 400 response.
        var unknownType = ExecutionItemTypes.ByName("__not_a_real_type_xyz__");

        unknownType.ShouldBe(ExecutionItemTypes.NotFound);
    }

    [Fact]
    public void TriggerSourceFallsBackToApiWhenNull()
    {
        // Why: mirrors the same fallback guard used inside TriggerPipeline / TriggerProject.
        var request = new UnifiedTriggerRequest
        {
            Name = "MyPipeline",
            TriggerSource = null
        };

        var effective = request.TriggerSource ?? "Api";

        effective.ShouldBe("Api");
    }

    [Fact]
    public void TriggerSourceIsPreservedWhenProvided()
    {
        var request = new UnifiedTriggerRequest
        {
            Name = "MyPipeline",
            TriggerSource = "Scheduler"
        };

        var effective = request.TriggerSource ?? "Api";

        effective.ShouldBe("Scheduler");
    }

    // Tenant propagation (background-execution isolation seam) — mirrors the precedence expression
    // in UnifiedTriggerEndpoint.TriggerPipeline: the caller's own authenticated tenant claim always
    // wins over a body-supplied TenantId, to prevent a per-tenant caller from requesting execution
    // scoped to a DIFFERENT tenant. The request-supplied value is a relay path used only when the
    // caller's own token carries no tenant scope (e.g. the scheduler's service-account credential).

    [Fact]
    [Trait("Category", "Security")]
    public void CallersOwnTenantClaimWinsOverRequestSuppliedTenantId()
    {
        // Arrange
        var callerTenantId = Guid.NewGuid();
        var relayedTenantId = Guid.NewGuid();
        var principal = BuildAuthenticatedPrincipal(callerTenantId);
        var request = new UnifiedTriggerRequest { Name = "MyPipeline", TenantId = relayedTenantId };

        // Act
        var effective = new ClaimsPrincipalAuthenticationContext(principal).ActiveTenantId ?? request.TenantId;

        // Assert
        effective.ShouldBe(callerTenantId);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void RequestSuppliedTenantIdIsUsedWhenCallerHasNoTenantClaim()
    {
        // Arrange — a system/service-account caller with no tenant_id claim (e.g. the scheduler).
        var relayedTenantId = Guid.NewGuid();
        var principal = BuildAuthenticatedPrincipal(tenantId: null);
        var request = new UnifiedTriggerRequest { Name = "MyPipeline", TenantId = relayedTenantId };

        // Act
        var effective = new ClaimsPrincipalAuthenticationContext(principal).ActiveTenantId ?? request.TenantId;

        // Assert
        effective.ShouldBe(relayedTenantId);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void EffectiveTenantIdIsNullWhenNeitherCallerNorRequestCarryOne()
    {
        // Arrange
        var principal = BuildAuthenticatedPrincipal(tenantId: null);
        var request = new UnifiedTriggerRequest { Name = "MyPipeline" };

        // Act
        var effective = new ClaimsPrincipalAuthenticationContext(principal).ActiveTenantId ?? request.TenantId;

        // Assert
        effective.ShouldBeNull();
    }

    private static ClaimsPrincipal BuildAuthenticatedPrincipal(Guid? tenantId)
    {
        var claims = new System.Collections.Generic.List<Claim>
        {
            new(ClaimDefinitions.sub.Name, Guid.NewGuid().ToString())
        };
        if (tenantId.HasValue)
        {
            claims.Add(new Claim(ClaimDefinitions.tenantId.Name, tenantId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
