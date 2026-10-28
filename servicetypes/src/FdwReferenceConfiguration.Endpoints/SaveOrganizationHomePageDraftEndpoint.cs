using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: save the draft of an organization's home page (FDW-2325).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveOrganizationHomePageDraftEndpoint : SaveOrganizationHomePageDraftEndpointBase
{
    /// <inheritdoc />
    public SaveOrganizationHomePageDraftEndpoint(
        IHomePages pages,
        IOrganizationScope organizations,
        IAdministrativeAudit audit,
        ILogger<SaveOrganizationHomePageDraftEndpoint> logger)
        : base(pages, organizations, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:write");
        Summary(s =>
        {
            s.Summary = "Save an organization's home page draft";
            s.Description = "Saves the draft of an organization's home page, apart from its published page. The body is the page: a JSON object with version 1 and a cards array. The caller must act in the organization and hold home-page:write there.";
        });
        Tags("Settings");
    }
}
