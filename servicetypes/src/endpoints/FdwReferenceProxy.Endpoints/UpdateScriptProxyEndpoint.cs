using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Replace a stored script (proxy). Forwards the replacement to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public UpdateScriptProxyEndpoint(IHttpClientFactory factory, ILogger<UpdateScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Put;

    /// <inheritdoc />
    protected override string TargetPath() => $"scripts/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/scripts/{Name}");
        ConfigureScripts("Replace a stored script (proxy)", "Forwards the replacement to the ETL server.");
    }
}
