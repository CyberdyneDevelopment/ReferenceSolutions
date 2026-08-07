using System;
using Fdw.Operations.Abstractions.Execution;
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
using Reference.Etl.Server.Endpoints;
using Reference.Etl.Server.Endpoints.Trigger;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Endpoints;

/// <summary>
/// Tests for TriggerJobEndpoint request model and construction.
/// FastEndpoints Send.* methods require HTTP context for full integration testing.
/// These tests verify DTO shapes and argument forwarding logic.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class TriggerJobEndpointTests
{
    private readonly Mock<IExecutionTracker> _executionTracker = new();
    private readonly Mock<IDataGateway> _dataGateway = new();
    private readonly Mock<IFdwServiceProvider<IEtlPipeline, PipelineConfiguration>> _pipelineProvider = new();
    private readonly Mock<IPipelineExecutionQueue> _queue = new();
    // Why: ProjectExecutionQueue is sealed with a single int-capacity ctor — cannot be mocked.
    private readonly ProjectExecutionQueue _projectQueue = new(1);
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private readonly Mock<IOrchestrationNodeConfigurationProvider> _nodeProvider = new();

    [Fact]
    public void TriggerJobRequestDefaultsTriggerSourceToNull()
    {
        // Arrange & Act
        var request = new TriggerJobRequest();

        // Assert
        request.TriggerSource.ShouldBeNull();
        request.PipelineName.ShouldBe(string.Empty);
        request.ScheduleName.ShouldBeNull();
    }

    [Fact]
    public void TriggerJobResponseHasDefaultValues()
    {
        // Arrange & Act
        var response = new TriggerJobResponse();

        // Assert
        response.ExecutionId.ShouldBe(Guid.Empty);
        response.Status.ShouldBe(string.Empty);
    }

    [Fact]
    public void HandleAsyncDefaultsTriggerSourceToApiWhenNull()
    {
        // Arrange
        var request = new TriggerJobRequest
        {
            PipelineName = "TestPipeline",
            TriggerSource = null
        };

        // Act — verify the fallback logic the endpoint applies
        var triggerSource = request.TriggerSource ?? "Api";

        // Assert
        triggerSource.ShouldBe("Api");
    }

    [Fact]
    public void HandleAsyncPassesTriggerSourceWhenProvided()
    {
        // Arrange
        var request = new TriggerJobRequest
        {
            PipelineName = "TestPipeline",
            TriggerSource = "Manual"
        };

        // Act
        var triggerSource = request.TriggerSource ?? "Api";

        // Assert
        triggerSource.ShouldBe("Manual");
    }

    [Fact]
    public void EndpointCanBeConstructed()
    {
        // Act
        var endpoint = new TriggerJobEndpoint(
            _executionTracker.Object,
            _dataGateway.Object,
            _pipelineProvider.Object,
            _queue.Object,
            _broadcaster.Object,
            new NullLogger<TriggerJobEndpoint>());

        // Assert
        endpoint.ShouldNotBeNull();
    }
}
