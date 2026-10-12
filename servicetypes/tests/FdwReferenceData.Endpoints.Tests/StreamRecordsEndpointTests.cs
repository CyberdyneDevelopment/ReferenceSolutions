namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// The behaviour a streaming endpoint has to get right, asserted over a real socket: rows arrive as
/// they are produced, one cursor serves every registered format, and an incomplete response is never
/// mistakable for a complete one.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class StreamRecordsEndpointTests : IAsyncLifetime
{
    private StreamingHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        _host = new StreamingHost(5487);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task RowsArriveAsTheyAreProducedRatherThanAllAtTheEnd()
    {
        var response = await _host.Get("/paced/export?rows=4", "text/csv", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(200);

        // The source yields a row every 120ms. Held until the end, every byte would land in one
        // piece at the end; streamed, the pieces are spread across the response's lifetime.
        response.Chunks.Count.ShouldBeGreaterThan(2);
        var span = response.Chunks[^1].At - response.Chunks[0].At;
        span.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task TheFirstBytesArriveLongBeforeTheLastRowIsRead()
    {
        var response = await _host.Get("/paced/export?rows=4", "text/csv", TestContext.Current.CancellationToken);

        // Time-to-first-byte is what makes this useful on a large set: it must not scale with the
        // row count.
        response.Chunks[0].At.ShouldBeLessThan(response.Chunks[^1].At - TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public async Task TheResponseIsChunkedAndCarriesNoContentLength()
    {
        var response = await _host.Get("/paced/export?rows=2", "text/csv", TestContext.Current.CancellationToken);

        response.Header("Transfer-Encoding").ShouldBe("chunked");
        response.Header("Content-Length").ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAcceptHeaderPicksTheFormatFromTheSameCursor()
    {
        var csv = await _host.Get("/paced/export?rows=2", "text/csv", TestContext.Current.CancellationToken);
        var json = await _host.Get("/paced/export?rows=2", "application/json", TestContext.Current.CancellationToken);

        csv.Header("Content-Type").ShouldBe("text/csv");
        csv.Body.ShouldContain("id,name");
        csv.Body.ShouldContain("0,name-0");

        json.Header("Content-Type").ShouldBe("application/json");
        json.Body.ShouldContain("\"name\":\"name-0\"");
    }

    [Fact]
    public async Task AWildcardAcceptGetsTheFormatTheEndpointDeclares()
    {
        var response = await _host.Get("/paced/export?rows=2", "*/*", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(200);
        response.Header("Content-Type").ShouldBe("application/json");
    }

    [Fact]
    public async Task AnUnservableAcceptIs406BeforeAnyBodyIsWritten()
    {
        var response = await _host.Get("/paced/export?rows=2", "application/pdf", TestContext.Current.CancellationToken);

        // Negotiation happens before the first byte, so it can still carry a real status code.
        response.StatusCode.ShouldBe(406);
        response.Body.ShouldContain("application/json");
    }

    [Fact]
    public async Task ARefusedOpenAnswersItsOwnStatusBeforeAnyBodyIsWritten()
    {
        var response = await _host.Get("/paced/export?rows=2&refuse=true", "application/json", TestContext.Current.CancellationToken);

        // FDW-1162: the open happens before the first byte, so its failure still carries a real status. That status is
        // the failure's own -- a refusal is a 400 naming its code -- not a 500 for whatever went wrong.
        response.StatusCode.ShouldBe(400);
        response.Body.ShouldContain(PacedStreamEndpoint.RefusalCode);
    }

    [Fact]
    public async Task ACompleteResponseEndsWithATerminatingChunk()
    {
        var response = await _host.Get("/paced/export?rows=3", "text/csv", TestContext.Current.CancellationToken);

        response.EndedCleanly.ShouldBeTrue();
        response.ConnectionReset.ShouldBeFalse();
    }

    [Fact]
    public async Task AFailureMidStreamLeavesTheBodyTruncatedRatherThanLookingComplete()
    {
        var response = await _host.Get(
            "/paced/export?rows=4&failAt=2", "text/csv", TestContext.Current.CancellationToken);

        // The status line already said 200 and cannot be taken back, so the only honest signal left
        // is refusing to terminate the body.
        response.Body.ShouldContain("name-0");
        response.EndedCleanly.ShouldBeFalse();
    }

    [Fact]
    public async Task HittingTheRowCeilingAlsoRefusesToLookComplete()
    {
        var response = await _host.Get("/paced/export?rows=50", "text/csv", TestContext.Current.CancellationToken);

        // The regression this guards: response trailers exist only on HTTP/2 and later, so over
        // HTTP/1.1 a capped response that terminated cleanly was indistinguishable from a whole one.
        // A short body that looks complete is the silent truncation this endpoint must never produce.
        response.EndedCleanly.ShouldBeFalse();
        response.Body.ShouldContain($"{PacedStreamEndpoint.RowCeiling - 1},name-{PacedStreamEndpoint.RowCeiling - 1}");
        response.Body.ShouldNotContain($"{PacedStreamEndpoint.RowCeiling},name-{PacedStreamEndpoint.RowCeiling}");
    }

    [Fact]
    public async Task ASecondStreamIsRefusedWhileTheCapIsFull()
    {
        // Why the first stream is held open on a raw socket rather than started and slept on: a sleep is a
        // guess about when the first request has taken its slot, and under a loaded test run the guess was
        // wrong - the second request could win the slot and pass. The slot is taken before the cursor opens,
        // which is before the first byte is written, so a first byte arriving is PROOF the slot is held.
        using var holder = new System.Net.Sockets.TcpClient();
        await holder.ConnectAsync(System.Net.IPAddress.Loopback, _host.Port, TestContext.Current.CancellationToken);
        var stream = holder.GetStream();
        await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(
            "GET /paced/export?rows=50 HTTP/1.1\r\nHost: localhost\r\nAccept: text/csv\r\n\r\n"),
            TestContext.Current.CancellationToken);
        var firstBytes = new byte[64];
        (await stream.ReadAsync(firstBytes, TestContext.Current.CancellationToken)).ShouldBeGreaterThan(0);

        var second = await _host.Get("/paced/export?rows=1", "text/csv", TestContext.Current.CancellationToken);

        // A stream holds a cursor, and therefore a pooled connection, for as long as the client reads.
        second.StatusCode.ShouldBe(429);

        // Release the slot before returning. Left to disposal, it frees asynchronously and the next test
        // can start while it is still held - moving this test's race one test along instead of removing it.
        holder.Close();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (PacedRecordSource.LiveCursors != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        PacedRecordSource.LiveCursors.ShouldBe(0);
    }

    [Fact]
    public async Task TheCursorAndItsSlotAreReleasedOnceAStreamEnds()
    {
        await _host.Get("/paced/export?rows=2", "text/csv", TestContext.Current.CancellationToken);
        await _host.Get("/paced/export?rows=2", "text/csv", TestContext.Current.CancellationToken);

        PacedRecordSource.LiveCursors.ShouldBe(0);

        // A leaked slot would make this third request 429 rather than 200.
        (await _host.Get("/paced/export?rows=2", "text/csv", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task TheCursorIsReleasedWhenTheClientGoesAwayMidStream()
    {
        using (var client = new System.Net.Sockets.TcpClient())
        {
            await client.ConnectAsync(System.Net.IPAddress.Loopback, _host.Port, TestContext.Current.CancellationToken);
            var request = System.Text.Encoding.ASCII.GetBytes(
                $"GET /paced/export?rows=200 HTTP/1.1\r\nHost: localhost\r\nAccept: text/csv\r\n\r\n");
            await client.GetStream().WriteAsync(request, TestContext.Current.CancellationToken);
            await Task.Delay(300, TestContext.Current.CancellationToken);
        }

        // Disposal is what returns the pooled database connection; without it a disconnecting client
        // would be a resource leak rather than merely a wasted query.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (PacedRecordSource.LiveCursors != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        PacedRecordSource.LiveCursors.ShouldBe(0);
    }

    [Fact]
    public async Task AnExportIsSentAsAnAttachmentNamedForTheResourceAndFormat()
    {
        var csv = await _host.Get("/paced/export?rows=1", "text/csv", TestContext.Current.CancellationToken);
        var json = await _host.Get("/paced/export?rows=1", "application/json", TestContext.Current.CancellationToken);

        // Without "attachment" a browser renders a CSV inline instead of downloading it. The extension is
        // the format's own, so the same cursor downloads as paced.csv or paced.json.
        csv.Header("Content-Disposition").ShouldStartWith("attachment");
        csv.Header("Content-Disposition").ShouldContain("paced.csv");
        json.Header("Content-Disposition").ShouldContain("paced.json");
    }

    [Fact]
    public async Task ARefusedRequestCarriesNoAttachmentHeader()
    {
        var refused = await _host.Get("/paced/export?rows=1", "application/pdf", TestContext.Current.CancellationToken);

        // A 406 body is an error, not a file. Naming it as a download would save the error message to disk.
        refused.StatusCode.ShouldBe(406);
        refused.Header("Content-Disposition").ShouldBeEmpty();
    }
}
