using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: publish the draft of an organization's home page (FDW-2325).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PublishOrganizationHomePageEndpoint : PublishOrganizationHomePageEndpointBase
{
    /// <inheritdoc />
    public PublishOrganizationHomePageEndpoint(
        IHomePages pages,
        IOrganizationScope organizations,
        IScopeAuthority authority,
        IAdministrativeAudit audit,
        ILogger<PublishOrganizationHomePageEndpoint> logger)
        : base(pages, organizations, authority, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:write");
        Summary(s =>
        {
            s.Summary = "Publish an organization's home page";
            s.Description = "Makes the draft of an organization's home page its published page. Refused when the organization holds no draft. The caller must act in the organization and hold home-page:write there.";
        });
        Tags("Settings");
    }
}
