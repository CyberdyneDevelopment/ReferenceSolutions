using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Check a script draft (proxy). Forwards the draft to the ETL server, which answers every problem saving it would be refused for.</summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public ValidateScriptProxyEndpoint(IHttpClientFactory factory, ILogger<ValidateScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => "scripts/validate";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/scripts/validate");
        ConfigureScripts("Check a script draft (proxy)", "Forwards the draft to the ETL server, which answers every problem saving it would be refused for.");
    }
}
