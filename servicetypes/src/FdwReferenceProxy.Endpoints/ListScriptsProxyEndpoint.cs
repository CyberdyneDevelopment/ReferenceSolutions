using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>List stored scripts (proxy). Forwards the paged list (skip, take, total) to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListScriptsProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public ListScriptsProxyEndpoint(IHttpClientFactory factory, ILogger<ListScriptsProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:read";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Get;

    /// <inheritdoc />
    protected override string TargetPath() => "scripts";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/scripts");
        ConfigureScripts("List stored scripts (proxy)", "Forwards the paged list (skip, take, total) to the ETL server.");
    }
}
