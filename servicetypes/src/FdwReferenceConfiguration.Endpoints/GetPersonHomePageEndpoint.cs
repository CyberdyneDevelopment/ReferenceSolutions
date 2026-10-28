using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: read the caller's own home page (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPersonHomePageEndpoint : GetPersonHomePageEndpointBase
{
    /// <inheritdoc />
    public GetPersonHomePageEndpoint(
        IHomePages pages,
        IAuthenticationContextAccessor authContext)
        : base(pages, authContext)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Read the caller's own home page";
            s.Description = "Returns the published page and the draft of the signed-in person's own home page, each null when they hold none. Needs no right.";
        });
        Tags("Settings");
    }
}
