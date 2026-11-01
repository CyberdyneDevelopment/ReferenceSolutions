using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: read a tenant's home page (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetTenantHomePageEndpoint : GetTenantHomePageEndpointBase
{
    /// <inheritdoc />
    public GetTenantHomePageEndpoint(
        IHomePages pages,
        IScopeAuthority authority)
        : base(pages, authority)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:read");
        Summary(s =>
        {
            s.Summary = "Read a tenant's home page";
            s.Description = "Returns the published page and the draft of a tenant, each null when it holds none. The caller must act in the tenant and hold home-page:read there.";
        });
        Tags("Settings");
    }
}
