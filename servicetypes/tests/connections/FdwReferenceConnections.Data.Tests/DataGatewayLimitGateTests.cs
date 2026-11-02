using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The gateway's limit gate as a whole (FDW-949): a concurrency cap only limits anything if the slot it
/// grants stays taken while the work runs, so a full connection refuses at once and hands the slot back
/// when the call finishes, fails, throws, or the cursor is disposed. And limits that cannot be read are a
/// refusal, not permission to run without any.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayLimitGateTests
{
    private readonly LimitedGatewayHarness _harness = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteRefusesACallWhileTheConcurrencyCapIsFullAndAdmitsOneOnceItIsFree()
    {
        // Arrange -- the first call is held inside the connection until the test lets it go.
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IGenericResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        _harness.Connection
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    entered.SetResult();
                    return release.Task;
                }

                return Task.FromResult<IGenericResult<string>>(GenericResult<string>.Success("later"));
            });
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1));

        // Act
        var first = service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        await entered.Task;
        var whileFull = await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        release.SetResult(GenericResult<string>.Success("first"));
        var firstResult = await first;
        var afterwards = await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        whileFull.IsSuccess.ShouldBeFalse();
        firstResult.IsSuccess.ShouldBeTrue();
        afterwards.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteGivesBackTheConcurrencySlotWhenTheCallFails()
    {
        // Arrange
        _harness.Connection
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Failure(new GenericMessage("boom")));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1));

        // Act
        await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert -- the second call reached the connection, so the first left no slot taken.
        _harness.Connection.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteGivesBackTheConcurrencySlotWhenTheConnectionThrows()
    {
        // Arrange
        _harness.Connection
            .SetupSequence(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("boom"))
            .ReturnsAsync(GenericResult<string>.Success("recovered"));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1));

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken));
        var afterwards = await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        afterwards.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceHoldsAConcurrencySlotUntilTheCursorIsDisposed()
    {
        // Arrange
        _harness.Cursor
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IRecordSource<DataRecord>>.Success(new ScriptedRecordSource()));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1));

        // Act
        var first = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var whileOpen = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        await first.Value.ShouldNotBeNull().DisposeAsync();
        var afterDispose = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        whileOpen.IsSuccess.ShouldBeFalse();
        afterDispose.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceGivesBackTheConcurrencySlotWhenTheConnectionCannotOpen()
    {
        // Arrange
        _harness.Cursor
            .SetupSequence(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("cannot open")))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(new ScriptedRecordSource()));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1));

        // Act
        var failed = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var afterwards = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        failed.IsSuccess.ShouldBeFalse();
        afterwards.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARefusedCallGivesBackTheSlotAnEarlierCheckOfTheSameLimitTook()
    {
        // Arrange -- one limit, two caps: the concurrency slot is taken before the result-size check refuses.
        _harness.Connection
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, concurrent: 1, maxRows: 10));
        var tooLarge = new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = 0, Take = 100 } };
        var small = new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = 0, Take = 5 } };

        // Act
        var refused = await service.Execute<string>(tooLarge, _harness.Target, TestContext.Current.CancellationToken);
        var admitted = await service.Execute<string>(small, _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        refused.IsSuccess.ShouldBeFalse();
        admitted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteFailsWhenTheLimitsCannotBeResolved()
    {
        // Arrange
        var service = _harness.GatewayWhoseLimitsCannotBeResolved();

        // Act
        var result = await service.Execute<string>(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _harness.Connection.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceFailsWhenTheLimitsCannotBeResolved()
    {
        // Arrange
        var service = _harness.GatewayWhoseLimitsCannotBeResolved();

        // Act
        var opened = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        opened.IsSuccess.ShouldBeFalse();
        _harness.Cursor.Verify(
            c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
