using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: save a tenant's home page draft (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveTenantHomePageDraftEndpoint : SaveTenantHomePageDraftEndpointBase
{
    /// <inheritdoc />
    public SaveTenantHomePageDraftEndpoint(
        IHomePages pages,
        IScopeAuthority authority,
        IAdministrativeAudit audit,
        ILogger<SaveTenantHomePageDraftEndpoint> logger)
        : base(pages, authority, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:write");
        Summary(s =>
        {
            s.Summary = "Save a tenant's home page draft";
            s.Description = "Saves the draft of a tenant's home page, apart from its published page, and records it in the administrative audit. The body is the page. The caller must act in the tenant and hold home-page:write there.";
        });
        Tags("Settings");
    }
}
