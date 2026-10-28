using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: read an organization's published home page and its draft (FDW-2325).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetOrganizationHomePageEndpoint : GetOrganizationHomePageEndpointBase
{
    /// <inheritdoc />
    public GetOrganizationHomePageEndpoint(IHomePages pages, IOrganizationScope organizations)
        : base(pages, organizations)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:read");
        Summary(s =>
        {
            s.Summary = "Read an organization's home page";
            s.Description = "Returns the published page and the draft of an organization, each null when it holds none. The caller must act in the organization and hold home-page:read there.";
        });
        Tags("Settings");
    }
}
