using System.Net.Http;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;

namespace ReferenceConnections.Http.Tests.Protocols;

/// <summary>
/// Testable implementation of RestProtocolBase that exposes protected methods for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TestableRestProtocol : RestProtocolBase
{
    public TestableRestProtocol()
        : base(999, "TestRest", "Test REST protocol for unit tests")
    {
    }

    // Expose protected methods for testing
    public new string BuildFilterFromExpression(IFilterNode node)
        => base.BuildFilterFromExpression(node);

    public new string BuildPaginationQueryString(IQueryCommand command, HttpProtocolContext context)
        => base.BuildPaginationQueryString(command, context);

    public new string BuildOrderingQueryString(IQueryCommand command, HttpProtocolContext context)
        => base.BuildOrderingQueryString(command, context);

    public new string BuildFullQueryString(IQueryCommand command, HttpProtocolContext context)
        => base.BuildFullQueryString(command, context);

    public new string ParseErrorResponse(HttpResponseMessage response, string content, HttpProtocolContext context)
        => base.ParseErrorResponse(response, content, context);

    public new string ExtractDataFromWrapper(string content)
        => base.ExtractDataFromWrapper(content);

    public new RestPaginationInfo? ExtractPaginationInfo(
        HttpResponseMessage response,
        string content,
        HttpProtocolContext context)
        => base.ExtractPaginationInfo(response, content, context);

    public new void ConfigureAuthenticationHeaders(HttpRequestMessage request, HttpProtocolContext context)
        => base.ConfigureAuthenticationHeaders(request, context);
}
