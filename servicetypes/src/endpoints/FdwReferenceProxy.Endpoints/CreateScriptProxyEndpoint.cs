using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Create a stored script (proxy). Forwards the stored script to the ETL server, which owns scripts.</summary>
[ExcludeFromCodeCoverage]
public sealed class CreateScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public CreateScriptProxyEndpoint(IHttpClientFactory factory, ILogger<CreateScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => "scripts";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts");
        ConfigureScripts("Create a stored script (proxy)", "Forwards the stored script to the ETL server, which owns scripts.");
    }
}
