using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Run a script draft (proxy). Forwards the unsaved script and its values to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class RunDraftScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public RunDraftScriptProxyEndpoint(IHttpClientFactory factory, ILogger<RunDraftScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:execute";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => "scripts/run";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/run");
        ConfigureScripts("Run a script draft (proxy)", "Forwards the unsaved script and its values to the ETL server.");
    }
}
