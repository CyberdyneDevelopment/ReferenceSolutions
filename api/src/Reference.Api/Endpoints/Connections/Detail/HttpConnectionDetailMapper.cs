using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Http;

namespace Reference.Api.Endpoints;

/// <summary>Projects an Http typed body onto the detail DTO (mirrors CreateHttpConnectionEndpoint.MapToDetail).</summary>
[ExcludeFromCodeCoverage]
internal sealed class HttpConnectionDetailMapper : IConnectionDetailMapper
{
    /// <inheritdoc />
    public string ServiceOptionType => "Http";

    /// <inheritdoc />
    public void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto)
    {
        var c = (HttpConnectionConfiguration)body;
        dto.BaseUrl = c.BaseUrl;
        dto.Protocol = c.Protocol;
        dto.TimeoutSeconds = c.TimeoutSeconds;
        dto.AuthenticationType = c.AuthenticationType;
        dto.UseMtls = c.UseMtls;
    }
}
