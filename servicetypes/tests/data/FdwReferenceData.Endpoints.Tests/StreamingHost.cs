using System.Net;
using System.Text;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// A real Kestrel host serving a real <see cref="StreamRecordsEndpointBase{TRequest}"/>, over a real
/// socket.
/// </summary>
/// <remarks>
/// Why a socket and not a TestServer: what this endpoint has to get right is WHEN bytes reach the
/// client and how a response ENDS — progressive chunks, a clean terminating chunk on success, and a
/// reset on an incomplete one. An in-memory test server answers none of those questions, because it
/// never frames an HTTP response. These tests read raw bytes off the wire with timestamps for the
/// same reason.
/// </remarks>
public sealed class StreamingHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public int Port { get; }

    public StreamingHost(int port)
    {
        Port = port;
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
                // Why the assembly list is pinned to this one: FastEndpoints otherwise discovers every
        // endpoint in FdwReferenceData.Endpoints, and the rest of them need the gateway and
        // configuration services a real host wires up. These tests are about the streaming base.
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(PacedStreamEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(PacedStreamEndpoint);
        });
        builder.Services.AddAuthorization();

        // What the ResponseStreaming host option registers in a real host, supplied directly here: these
        // tests are about the streaming base, not about reading an hst row.
        builder.Services.AddSingleton(new ResponseStreamingOptions(
            PacedStreamEndpoint.RowCeiling, PacedStreamEndpoint.ConcurrentStreams));

        _app = builder.Build();
        _app.UseAuthorization();

        _app.UseFastEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    /// <summary>Reads a raw HTTP response, recording when each piece arrived and how it ended.</summary>
    public async Task<RawResponse> Get(string path, string accept, CancellationToken cancellationToken)
    {
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Port, cancellationToken);
        var started = DateTimeOffset.UtcNow;
        await using var stream = client.GetStream();

        var request = Encoding.ASCII.GetBytes(
            $"GET {path} HTTP/1.1\r\nHost: localhost\r\nAccept: {accept}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(request, cancellationToken);

        var chunks = new List<(TimeSpan At, string Text)>();
        var buffer = new byte[8192];
        var reset = false;
        var text = new StringBuilder();

        while (true)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(buffer, cancellationToken);
            }
            catch (IOException)
            {
                // A reset peer is how an incomplete response announces itself on the wire.
                reset = true;
                break;
            }

            if (read == 0) break;

            var piece = Encoding.UTF8.GetString(buffer, 0, read);
            chunks.Add((DateTimeOffset.UtcNow - started, piece));
            text.Append(piece);
        }

        return new RawResponse(text.ToString(), chunks, reset);
    }

    public sealed record RawResponse(string Raw, List<(TimeSpan At, string Text)> Chunks, bool ConnectionReset)
    {
        public int StatusCode =>
            int.TryParse(Raw.Split(' ').Skip(1).FirstOrDefault(), out var code) ? code : 0;

        /// <summary>True when the response ended with a complete final chunk rather than being cut off.</summary>
        public bool EndedCleanly => Raw.EndsWith("0\r\n\r\n", StringComparison.Ordinal);

        public string Header(string name) =>
            Raw.Split("\r\n\r\n")[0]
                .Split("\r\n")
                .FirstOrDefault(l => l.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase))
                ?.Split(':', 2)[1].Trim() ?? string.Empty;

        public string Body => Raw.Split("\r\n\r\n", 2).Skip(1).FirstOrDefault() ?? string.Empty;
    }
}

/// <summary>A cursor that yields rows slowly, so progressive delivery is observable.</summary>
public sealed class PacedRecordSource : IRecordSource<DataRecord>
{
    private readonly int _rows;
    private readonly int _failAtRow;
    private readonly TimeSpan _gap;

    public static int LiveCursors;

    public PacedRecordSource(int rows, int failAtRow, TimeSpan gap)
    {
        _rows = rows;
        _failAtRow = failAtRow;
        _gap = gap;
        Schema = new RecordSchema([NamedField("id"), NamedField("name")]);
        Interlocked.Increment(ref LiveCursors);
    }

    public RecordSchema Schema { get; }

    private static IDataField NamedField(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    public IEnumerable<IGenericResult<DataRecord>> Read() => throw new NotSupportedException();

    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _rows; i++)
        {
            await Task.Delay(_gap, cancellationToken);

            if (i == _failAtRow)
            {
                var message = new Mock<IGenericMessage>();
                message.Setup(m => m.Message).Returns("the source failed at row " + i);
                yield return GenericResult<DataRecord>.Failure(message.Object);
                yield break;
            }

            yield return GenericResult<DataRecord>.Success(new DataRecord(Schema, [i, "name-" + i]));
        }
    }

    public void Dispose() => Interlocked.Decrement(ref LiveCursors);

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}

public sealed class PacedRequest
{
    public int Rows { get; set; } = 3;

    public int FailAt { get; set; } = -1;

    /// <summary>Gets or sets a value indicating whether opening the records is refused, before any row is read.</summary>
    public bool Refuse { get; set; }
}

/// <summary>A minimal real endpoint over the base, standing in for ExportDataSetEndpoint's cursor.</summary>
public class PacedStreamEndpoint(ResponseStreamingOptions streaming)
    : StreamRecordsEndpointBase<PacedRequest>(streaming, NullLogger.Instance)
{
    public const int RowCeiling = 4;

    public const int ConcurrentStreams = 1;

    protected override string ResourceName => "paced";

    protected override string Route => "/paced/export";

    protected override string PolicyName => "paced:export";

    protected override string DefaultFormat => "Json";

    // One character, so every row drains on its own and the timing is the source's, not the buffer's.
    protected override int DrainThresholdChars => 1;

    protected override void ConfigureEndpoint() => AllowAnonymous();

    /// <summary>The code a refused open carries: a validation number (2xxxx), so its category answers 400.</summary>
    public const string RefusalCode = "PACED-20001";

    protected override Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecords(
        PacedRequest request, CancellationToken cancellationToken)
    {
        if (request.Refuse)
        {
            var refusal = new Mock<IGenericMessage>();
            refusal.Setup(m => m.Message).Returns("the export was refused");
            refusal.Setup(m => m.Code).Returns(RefusalCode);
            return Task.FromResult(GenericResult<IRecordSource<DataRecord>>.Failure(refusal.Object));
        }

        return Task.FromResult(GenericResult<IRecordSource<DataRecord>>.Success(
            (IRecordSource<DataRecord>)new PacedRecordSource(
                request.Rows, request.FailAt, TimeSpan.FromMilliseconds(120))));
    }
}
