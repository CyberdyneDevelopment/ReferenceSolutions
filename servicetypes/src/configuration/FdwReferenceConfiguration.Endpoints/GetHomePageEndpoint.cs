using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: read the home page that applies to the caller (FDW-2325).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetHomePageEndpoint : GetHomePageEndpointBase
{
    /// <inheritdoc />
    public GetHomePageEndpoint(IHomePages pages, IAuthenticationContextAccessor authContext)
        : base(pages, authContext)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Read the home page that applies to the caller";
            s.Description = "Returns the caller's own published home page, else the one of the organization they act in, else the one of their tenant, with the scope it comes from. Source kind None and a null page when no scope holds a published page. Needs no right.";
        });
        Tags("Settings");
    }
}
