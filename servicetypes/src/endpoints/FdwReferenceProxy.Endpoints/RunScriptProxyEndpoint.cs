using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Run a stored script (proxy). Forwards the run to the ETL server, which holds the script extension and the DataEngineer role check.</summary>
[ExcludeFromCodeCoverage]
public sealed class RunScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public RunScriptProxyEndpoint(IHttpClientFactory factory, ILogger<RunScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:execute";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => $"scripts/{Route<string>("Name")}/run";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/{Name}/run");
        ConfigureScripts("Run a stored script (proxy)", "Forwards the run to the ETL server, which holds the script extension and the DataEngineer role check.");
    }
}
