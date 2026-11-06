using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Http.Tests;

/// <summary>
/// FDW-2570: an insert of more rows than the protocol sends in one request runs as one request for each batch, one after the other, and answers
/// the rows written in all; the first batch that fails ends it.
/// </summary>
public sealed class HttpConnectionInsertBatchesTests
{
    private readonly Mock<IHttpProtocol> _protocol = new();
    private readonly List<string> _sent = [];
    private int _failOnRequest;

    private HttpConnection Connection(int rowsPerRequest)
    {
        _protocol.Setup(p => p.Name).Returns("Test");
        _protocol.Setup(p => p.InsertRowsPerRequest).Returns(rowsPerRequest);
        _protocol.Setup(p => p.Maps(It.IsAny<string>())).Returns<string>(type => type is "Insert");
        _protocol
            .Setup(p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()))
            .Returns<IDataCommand, IStorageContainer, HttpProtocolContext, CancellationToken>((command, _, _, _) =>
            {
                _sent.Add(command.Metadata["name"].ToString()!);
                return Task.FromResult(GenericResult<HttpRequestMessage>.Success(new HttpRequestMessage(HttpMethod.Post, "http://example.test/x")));
            });
        _protocol
            .Setup(p => p.ProcessResponse(It.IsAny<HttpResponseMessage>(), It.IsAny<IStorageContainer>(), It.IsAny<System.Type>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_failOnRequest == _sent.Count ? GenericResult<object?>.Failure(HttpResultCodes.ByName("RequestFailed"), ResultDetails.Create().With("ErrorMessage", "the request was refused")) : GenericResult<object?>.Success(2)));
        var client = new HttpClient(new Handler());
        var context = new HttpProtocolContext(new Mock<IGenericConfiguration>().Object, NullLoggerFactory.Instance, null, null, null);
        return new HttpConnection(NullLogger<HttpConnection>.Instance, new HttpConnectionConfiguration { Protocol = "Rest", AuthenticationType = "None", Lifetime = "Scoped", BaseUrl = "http://example.test/" }, client, _protocol.Object, context);
    }

    private static IDataCommand Named(string name)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["name"] = name });
        return command.Object;
    }

    private static IBatchableDataCommand Batchable(params string[] batches)
    {
        var command = new Mock<IBatchableDataCommand>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["name"] = "whole" });
        command.Setup(c => c.InBatchesOf(It.IsAny<int>())).Returns(batches.Select(Named).ToList());
        return command.Object;
    }

    [Fact]
    public async Task AnInsertOfManyBatchesSendsOneRequestForEachInOrderAndAnswersTheRowsWrittenInAll()
    {
        var result = await Connection(2).Execute<int>(Batchable("a", "b", "c"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ToArray().ShouldBe(new[] { "a", "b", "c" });
        result.Value.ShouldBe(6);
    }

    [Fact]
    public async Task AnInsertThatFitsInOneRequestIsSentAsItIs()
    {
        var result = await Connection(5).Execute<int>(Batchable("only"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ToArray().ShouldBe(new[] { "whole" });
    }

    [Fact]
    public async Task TheFirstBatchThatFailsEndsTheInsertAndLaterBatchesAreNotSent()
    {
        _failOnRequest = 2;

        var result = await Connection(2).Execute<int>(Batchable("a", "b", "c"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _sent.ToArray().ShouldBe(new[] { "a", "b" });
    }

    [Fact]
    public async Task AProtocolThatSendsNoRowsInARequestFailsWithoutSendingOne()
    {
        var result = await Connection(0).Execute<int>(Batchable("a"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task ACommandThatIsNotBatchableIsSentAsItIs()
    {
        var result = await Connection(2).Execute<int>(Named("plain"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ToArray().ShouldBe(new[] { "plain" });
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
