using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Http.Tests;

/// <summary>
/// A command the protocol does not map is reported as having no translator: a command that expands into simpler ones runs
/// those, and any other fails with a named result without a request being sent.
/// </summary>
public sealed class HttpConnectionMapsTests
{
    private readonly Mock<IHttpProtocol> _protocol = new();
    private readonly List<string> _sent = [];

    private HttpConnection Connection()
    {
        _protocol.Setup(p => p.Name).Returns("Test");
        _protocol.Setup(p => p.Maps(It.IsAny<string>())).Returns<string>(type => type is "Insert" or "Delete");
        _protocol
            .Setup(p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()))
            .Returns<IDataCommand, IStorageContainer, HttpProtocolContext, CancellationToken>((command, _, _, _) =>
            {
                _sent.Add(command.CommandType);
                return Task.FromResult(GenericResult<HttpRequestMessage>.Success(new HttpRequestMessage(HttpMethod.Post, "http://example.test/x")));
            });
        _protocol
            .Setup(p => p.ProcessResponse(It.IsAny<HttpResponseMessage>(), It.IsAny<IStorageContainer>(), It.IsAny<System.Type>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<object?>.Success(1));
        var client = new HttpClient(new Handler());
        var context = new HttpProtocolContext(new Mock<IGenericConfiguration>().Object, NullLoggerFactory.Instance, null, null, null);
        return new HttpConnection(NullLogger<HttpConnection>.Instance, new HttpConnectionConfiguration { BaseUrl = "http://example.test/" }, client, _protocol.Object, context);
    }

    private static IDataCommand Command(string type)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns(type);
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return command.Object;
    }

    [Fact]
    public async Task ACommandTheProtocolDoesNotMapFailsWithoutSendingARequest()
    {
        var result = await Connection().Execute<int>(Command("Find"), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Find");
        _sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpandableCommandTheProtocolDoesNotMapSendsTheCommandsItExpandsInto()
    {
        var upsert = new Mock<IExpandableDataCommand>();
        upsert.Setup(c => c.CommandType).Returns("Upsert");
        upsert.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        upsert.Setup(c => c.Expand(It.IsAny<IStorageContainer>())).Returns(
            GenericResult<IReadOnlyList<ExpandedCommand>>.Success(
            [
                new ExpandedCommand(Command("Delete"), CountsAsAffected: false),
                new ExpandedCommand(Command("Insert")),
            ]));

        var result = await Connection().Execute<int>(upsert.Object, new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ShouldBe(["Delete", "Insert"]);
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
