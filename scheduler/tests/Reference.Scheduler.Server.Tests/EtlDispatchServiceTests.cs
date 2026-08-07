using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Resiliency.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Retry;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Services;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class EtlDispatchServiceTests
{
    private readonly Mock<IPipelineJobClient> _client = new();
    private readonly ILogger<EtlDispatchService> _logger = new NullLogger<EtlDispatchService>();
    private readonly Mock<IResiliencyPipelineFactory> _pipelineFactory = new();

    private EtlDispatchService CreateSut(ResiliencePipeline? pipeline = null)
    {
        var options = Options.Create(new EtlDispatchConfiguration());

        var pipelineToUse = pipeline ?? ResiliencePipeline.Empty;
        _pipelineFactory.Setup(x => x.GetOrCreate("HttpClient", "EtlDispatch"))
            .Returns(GenericResult<ResiliencePipeline>.Success(pipelineToUse));

        return new EtlDispatchService(_client.Object, _logger, options, _pipelineFactory.Object);
    }

    private static ResiliencePipeline CreateRetryPipeline(int maxRetries = 2) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant
            })
            .Build();

    private static IGenericResult<TriggerPipelineResponse> SuccessResponse()
    {
        return GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse
        {
            ExecutionId = Guid.NewGuid(),
            Status = "Queued"
        });
    }

    private static IGenericResult<TriggerPipelineResponse> FailureResponse()
    {
        return GenericResult<TriggerPipelineResponse>.Failure(TestErrors.Failed);
    }

    // Success paths

    [Fact]
    public async Task DispatchReturnsSuccessOnFirstAttempt()
    {
        // Arrange
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse());
        var sut = CreateSut();

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task DispatchReturnsSuccessAfterRetry()
    {
        // Arrange - fail first, succeed second
        _client.SetupSequence(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailureResponse())
            .ReturnsAsync(SuccessResponse());
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 2));

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // Failure paths (result-based)

    [Fact]
    public async Task DispatchReturnsFailureAfterAllRetriesExhaustedWithFailureResult()
    {
        // Arrange - always fail
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailureResponse());
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 1));

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchRetriesToMaxAttemptsOnFailureResult()
    {
        // Arrange - always fail, maxRetries = 2 means 3 total attempts (1 initial + 2 retries)
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailureResponse());
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 2));

        // Act
        await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    // Failure paths (exception-based)

    [Fact]
    public async Task DispatchReturnsFailureAfterAllRetriesExhaustedWithException()
    {
        // Arrange - always throw
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 1));

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchRetriesToMaxAttemptsOnException()
    {
        // Arrange - always throw, maxRetries = 2 means 3 total attempts
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 2));

        // Act
        await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    // Exception-based retry success

    [Fact]
    public async Task DispatchReturnsSuccessAfterExceptionThenSuccess()
    {
        // Arrange - throw first, succeed second
        var callCount = 0;
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Returns((TriggerPipelineRequest _, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Transient error");
                return Task.FromResult(SuccessResponse());
            });
        var sut = CreateSut(CreateRetryPipeline(maxRetries: 2));

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        callCount.ShouldBe(2);
    }

    // Cancellation

    [Fact]
    public async Task DispatchThrowsOperationCanceledWhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, cts.Token));
    }

    // Configuration

    [Fact]
    public async Task DispatchReturnsSuccessWhenDisabled()
    {
        // Arrange
        var options = Options.Create(new EtlDispatchConfiguration { Enabled = false });
        _pipelineFactory.Setup(x => x.GetOrCreate("HttpClient", "EtlDispatch"))
            .Returns(GenericResult<ResiliencePipeline>.Success(ResiliencePipeline.Empty));
        var sut = new EtlDispatchService(_client.Object, _logger, options, _pipelineFactory.Object);

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DispatchReturnsFailureWhenPipelineFactoryFails()
    {
        // Arrange
        var options = Options.Create(new EtlDispatchConfiguration());
        _pipelineFactory.Setup(x => x.GetOrCreate("HttpClient", "EtlDispatch"))
            .Returns(GenericResult<ResiliencePipeline>.Failure(TestErrors.Failed));
        var sut = new EtlDispatchService(_client.Object, _logger, options, _pipelineFactory.Object);

        // Act
        var result = await sut.Dispatch("TestSchedule", "TestPipeline", "Scheduled", null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _client.Verify(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DispatchPassesCorrectRequestParameters()
    {
        // Arrange
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse());
        var sut = CreateSut();

        // Act
        await sut.Dispatch("MySchedule", "MyPipeline", "Manual", null, TestContext.Current.CancellationToken);

        // Assert
        _client.Verify(x => x.Trigger(
            It.Is<TriggerPipelineRequest>(r =>
                string.Equals(r.Name, "MyPipeline", StringComparison.Ordinal) &&
                string.Equals(r.TriggerSource, "Manual", StringComparison.Ordinal) &&
                string.Equals(r.ScheduleName, "MySchedule", StringComparison.Ordinal) &&
                r.TenantId == null),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    // Tenant propagation (background-execution isolation seam)

    [Fact]
    [Trait("Category", "Security")]
    public async Task DispatchRelaysTenantIdOntoTriggerPipelineRequest()
    {
        // Arrange
        // Why: the scheduler is a service-account caller with no tenant scope of its own — it must
        // relay the SCHEDULE's own TenantId onto the request so the ETL server's dispatched execution
        // gets the correct RLS SESSION_CONTEXT (see UnifiedTriggerEndpoint.TriggerPipeline).
        var tenantId = Guid.NewGuid();
        _client.Setup(x => x.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse());
        var sut = CreateSut();

        // Act
        await sut.Dispatch("MySchedule", "MyPipeline", "Scheduled", tenantId, TestContext.Current.CancellationToken);

        // Assert
        _client.Verify(x => x.Trigger(
            It.Is<TriggerPipelineRequest>(r => r.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once());
    }
}
