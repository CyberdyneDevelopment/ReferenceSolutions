using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Hosting.ApiHost;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.OpenApi;
using NSwag.Generation.Processors;

namespace ReferenceApiHost;

/// <summary>
/// The reference API's host: the values this deployment supplies to the framework's API surface.
/// </summary>
/// <remarks>
/// Everything mechanical — FastEndpoints with discovery off and filtered to the declared set, the
/// context accessor the permission filter needs, the framework's document processors and their
/// post-Build initialization — lives on the base. What is left here is what genuinely belongs to
/// this deployment and could not be guessed: what the document is called, which origins it
/// advertises, and which OAuth client its token form prefills.
/// </remarks>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "ApiHost")]
public class ApiHostServiceType : ApiHostServiceTypeBase
{
    // Why stated rather than derived: behind the Caddy reverse proxy Request.Scheme reports http
    // even with forwarded-headers middleware, so a derived origin gets "try it" blocked as mixed
    // content. These are the origins this deployment is actually reachable on.
    private static readonly string[] Origins =
    {
        "https://api-1-2-0-preview.cyberdynedevelopment.dev",
        "https://localhost:5007",
        "http://localhost:5000",
    };

    private static readonly IDocumentProcessor[] Processors =
    {
        new AuthAndTagDocumentProcessor("reference-client", "fdw.api offline_access"),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiHostServiceType"/> class.
    /// </summary>
    public ApiHostServiceType()
        : base("ApiHost", "ApiHost", "API Host",
               "The reference API's document, origins and OAuth client.")
    {
    }

    /// <inheritdoc />
    protected override string DocumentTitle => "Reference.Api";

    /// <inheritdoc />
    protected override string DocumentDescription =>
        "Fdw Reference API demonstrating DataGateway, SecretManager, and ConnectionProvider.";

    /// <inheritdoc />
    protected override IReadOnlyList<string> ServerUrls => Origins;

    /// <inheritdoc />
    protected override IReadOnlyList<IDocumentProcessor> AdditionalDocumentProcessors => Processors;

}
