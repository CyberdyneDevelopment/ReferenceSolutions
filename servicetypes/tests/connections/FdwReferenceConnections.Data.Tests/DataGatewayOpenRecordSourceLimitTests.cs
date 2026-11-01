using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// <c>IDataGateway.OpenRecordSource</c> is held to the connection's outbound limits exactly as
/// <c>Execute</c> is (FDW-947). Before, a streamed read skipped every one of them: it was not rate
/// limited, not counted against the daily budget, and not bounded by the query timeout.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayOpenRecordSourceLimitTests
{
    private readonly LimitedGatewayHarness _harness = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceRefusesAPageLargerThanTheMaxResultSize()
    {
        // Arrange
        Opens(new ScriptedRecordSource());
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, maxRows: 10));
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression { Skip = 0, Take = 100 },
        };

        // Act
        var opened = await service.OpenRecordSource(command, _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        opened.IsSuccess.ShouldBeFalse();
        NeverOpened();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceRefusesWhenTheRateLimitIsExhausted()
    {
        // Arrange
        Opens(new ScriptedRecordSource());
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, perSecond: 1, burst: 1));

        // Act
        var first = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var second = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceCountsEachOpenAgainstTheDailyQueryBudget()
    {
        // Arrange
        Opens(new ScriptedRecordSource());
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, perDay: 2));

        // Act
        var first = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var second = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var third = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        third.IsSuccess.ShouldBeFalse();
        _harness.Counters.ReadQueryCount(_harness.ConnectionId).ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceDoesNotCountAnOpenThatFailed()
    {
        // Arrange
        _harness.Cursor
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("cannot open")));
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, perDay: 5));

        // Act
        var opened = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        opened.IsSuccess.ShouldBeFalse();
        _harness.Counters.ReadQueryCount(_harness.ConnectionId).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceWithNoLimitsReturnsTheConnectionsCursorUnwrapped()
    {
        // Arrange
        var inner = new ScriptedRecordSource();
        Opens(inner);
        var service = _harness.Gateway();

        // Act
        var opened = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);

        // Assert
        opened.IsSuccess.ShouldBeTrue();
        opened.Value.ShouldBeSameAs(inner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceEndsTheCursorWithATimeoutFailureWhenTheQueryTimeoutFires()
    {
        // Arrange -- the cursor yields one row, then never yields again until it is cancelled.
        Opens(new ScriptedRecordSource { BlockAfterFirstRow = true });
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, timeoutSeconds: 1));

        // Act
        var opened = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        var seen = new List<IGenericResult<DataRecord>>();
        await foreach (var record in opened.Value.ShouldNotBeNull().Read(TestContext.Current.CancellationToken))
        {
            seen.Add(record);
        }

        // Assert
        opened.IsSuccess.ShouldBeTrue();
        seen.Count.ShouldBe(2);
        seen[0].IsSuccess.ShouldBeTrue();
        seen[1].IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSourceUnderATimeoutDisposesTheConnectionsCursorWhenDisposed()
    {
        // Arrange
        var inner = new ScriptedRecordSource();
        Opens(inner);
        var service = _harness.Gateway(new TestConnectionLimit(_harness.ConnectionId, timeoutSeconds: 30));

        // Act
        var opened = await service.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), _harness.Target, TestContext.Current.CancellationToken);
        await opened.Value.ShouldNotBeNull().DisposeAsync();

        // Assert
        inner.Disposed.ShouldBeTrue();
    }

    private void Opens(IRecordSource<DataRecord> source)
        => _harness.Cursor
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(source));

    private void NeverOpened()
        => _harness.Cursor.Verify(
            c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Never);
}
