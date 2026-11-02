using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: publish the caller's own home page (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PublishPersonHomePageEndpoint : PublishPersonHomePageEndpointBase
{
    /// <inheritdoc />
    public PublishPersonHomePageEndpoint(
        IHomePages pages,
        IAuthenticationContextAccessor authContext,
        IAdministrativeAudit audit,
        ILogger<PublishPersonHomePageEndpoint> logger)
        : base(pages, authContext, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Publish the caller's own home page";
            s.Description = "Makes the draft of the signed-in person's own home page its published page. Refused when they hold no draft. Needs no right.";
        });
        Tags("Settings");
    }
}
