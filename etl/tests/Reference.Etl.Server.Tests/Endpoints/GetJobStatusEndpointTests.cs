using System;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Etl.Server.Endpoints;
using Reference.Etl.Server.Models;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Endpoints;

/// <summary>
/// Tests for GetJobStatusEndpoint construction and request model.
/// FastEndpoints Send.* methods require HTTP context for full integration testing.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class GetJobStatusEndpointTests
{
    private readonly Mock<IExecutionTracker> _executionTracker = new();
    private readonly Mock<IDataGateway> _dataGateway = new();

    [Fact]
    public void EndpointCanBeConstructed()
    {
        // Arrange & Act
        var endpoint = new GetJobStatusEndpoint(
            _executionTracker.Object,
            _dataGateway.Object,
            new NullLogger<GetJobStatusEndpoint>());

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    public void JobStatusRequestHasDefaultValues()
    {
        // Arrange & Act
        var request = new JobStatusRequest();

        // Assert
        request.ExecutionId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void JobStatusResponseHoldsAllFields()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var startedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var completedAt = new DateTime(2026, 1, 15, 10, 5, 0, DateTimeKind.Utc);

        // Act
        var response = new JobStatusResponse(
            executionId,
            "TestPipeline",
            "Succeeded",
            startedAt,
            completedAt,
            300_000L,
            100L, 95L, 5L, null);

        // Assert
        response.ExecutionId.ShouldBe(executionId);
        response.PipelineName.ShouldBe("TestPipeline");
        response.Status.ShouldBe("Succeeded");
        response.StartedAt.ShouldBe(startedAt);
        response.CompletedAt.ShouldBe(completedAt);
        response.DurationMs.ShouldBe(300_000L);
        response.RecordsExtracted.ShouldBe(100L);
        response.RecordsLoaded.ShouldBe(95L);
        response.RecordsFailed.ShouldBe(5L);
        response.ErrorMessage.ShouldBeNull();
    }
}
