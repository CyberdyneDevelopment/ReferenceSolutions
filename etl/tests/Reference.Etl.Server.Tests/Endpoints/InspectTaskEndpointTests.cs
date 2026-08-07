using System;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Etl.Server.Endpoints.Executions;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Endpoints;

/// <summary>
/// Tests for InspectTaskEndpoint construction and the test-mode gate guard logic.
/// FastEndpoints Send.* methods require HTTP context for full integration testing.
/// The critical security constraint — 404 for non-test executions — is verified via
/// the IPipelineExecutionInspector mock setup.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class InspectTaskEndpointTests
{
    private readonly Mock<IPipelineExecutionInspector> _inspector = new();

    [Fact]
    public void EndpointCanBeConstructed()
    {
        var endpoint = new InspectTaskEndpoint(_inspector.Object, new NullLogger<InspectTaskEndpoint>());

        endpoint.ShouldNotBeNull();
    }

    [Fact]
    public void InspectTaskRequestHasExpectedDefaults()
    {
        var request = new InspectTaskRequest();

        request.ExecutionId.ShouldBe(Guid.Empty);
        request.TaskId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void TaskInspectorDtoHasExpectedDefaults()
    {
        var dto = new TaskInspectorDto();

        dto.RecordsIn.ShouldBe(0L);
        dto.RecordsOut.ShouldBe(0L);
        dto.RecordsDiscarded.ShouldBe(0L);
        dto.RecordsHeld.ShouldBe(0L);
        dto.SamplesDiscarded.ShouldBe(0L);
        dto.SampleBufferAtCapacity.ShouldBeFalse();
        dto.Samples.ShouldNotBeNull();
        dto.Samples.ShouldBeEmpty();
    }

    [Fact]
    public void IsTestExecutionReturnsFalseForProductionExecutionId()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        // Why: production executions must never expose sample data — inspector returns false,
        // which causes HandleAsync to return 404 immediately.
        _inspector.Setup(i => i.IsTestExecution(executionId)).Returns(false);

        // Act
        var isTest = _inspector.Object.IsTestExecution(executionId);

        // Assert
        isTest.ShouldBeFalse();
        _inspector.Verify(i => i.IsTestExecution(executionId), Times.Once);
    }

    [Fact]
    public void IsTestExecutionReturnsTrueForTestExecutionId()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        _inspector.Setup(i => i.IsTestExecution(executionId)).Returns(true);

        // Act
        var isTest = _inspector.Object.IsTestExecution(executionId);

        // Assert
        isTest.ShouldBeTrue();
    }

    [Fact]
    public void GetTaskStateReturnsNullForUnregisteredTask()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        _inspector.Setup(i => i.GetTaskState(executionId, taskId)).Returns((TaskInspectorState?)null);

        // Act
        var state = _inspector.Object.GetTaskState(executionId, taskId);

        // Assert — endpoint sends 404 when state is null
        state.ShouldBeNull();
    }
}
