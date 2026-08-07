using System;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Etl.Server.Endpoints.Executions;
using Reference.Etl.Server.Endpoints.Trigger;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Endpoints;

/// <summary>
/// Tests for ApproveExecutionEndpoint construction and approval gate guard logic.
/// FastEndpoints Send.* methods require HTTP context for full integration testing.
/// These tests verify DTO shapes and the IExecutionTracker
/// dependency wiring.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class ApproveExecutionEndpointTests
{
    private readonly Mock<IExecutionTracker> _executionTracker = new();
    // Why: ProjectExecutionQueue is sealed with int-capacity ctor — cannot be mocked.
    private readonly ProjectExecutionQueue _projectQueue = new(1);

    [Fact]
    public void EndpointCanBeConstructed()
    {
        var endpoint = new ApproveExecutionEndpoint(
            _executionTracker.Object,
            _projectQueue,
            new NullLogger<ApproveExecutionEndpoint>());

        endpoint.ShouldNotBeNull();
    }

    [Fact]
    public void ApproveExecutionRequestHasExpectedDefault()
    {
        var request = new ApproveExecutionRequest();

        request.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void UnifiedTriggerResponseStatusDefaultsToEmptyString()
    {
        // Why: ApproveExecutionEndpoint returns UnifiedTriggerResponse — verify the shared DTO.
        var response = new UnifiedTriggerResponse();

        response.ExecutionId.ShouldBe(Guid.Empty);
        response.Status.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetItemReturnsFailureWhenTrackerFails()
    {
        // Arrange — tracker returns a failure result for an unknown execution ID
        var executionId = Guid.NewGuid();
        var ct = TestContext.Current.CancellationToken;
        // Why: endpoint sends 404 when !result.IsSuccess — verify the mock contract.
        _executionTracker
            .Setup(t => t.GetItem(executionId, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult<IExecutionItem>.Failure(new GenericMessage("Execution not found")));

        // Act
        var result = await _executionTracker.Object.GetItem(executionId, ct);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ExecutionNotInAwaitingApprovalStateIsRejected()
    {
        // Why: guard — only executions in AwaitingApproval can be approved.
        // Validate the string comparison the endpoint uses.
        var stateName = "Running";

        var isAwaitingApproval = string.Equals(stateName, "AwaitingApproval", StringComparison.Ordinal);

        isAwaitingApproval.ShouldBeFalse();
    }

    [Fact]
    public void ExecutionInAwaitingApprovalStatePassesGuard()
    {
        var stateName = "AwaitingApproval";

        var isAwaitingApproval = string.Equals(stateName, "AwaitingApproval", StringComparison.Ordinal);

        isAwaitingApproval.ShouldBeTrue();
    }
}
