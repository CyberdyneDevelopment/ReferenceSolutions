using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Get a stored script (proxy). Forwards the read to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class GetScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public GetScriptProxyEndpoint(IHttpClientFactory factory, ILogger<GetScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:read";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Get;

    /// <inheritdoc />
    protected override string TargetPath() => $"scripts/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/scripts/{Name}");
        ConfigureScripts("Get a stored script (proxy)", "Forwards the read to the ETL server.");
    }
}
