using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: publish a tenant's home page (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PublishTenantHomePageEndpoint : PublishTenantHomePageEndpointBase
{
    /// <inheritdoc />
    public PublishTenantHomePageEndpoint(
        IHomePages pages,
        IScopeAuthority authority,
        IAdministrativeAudit audit,
        ILogger<PublishTenantHomePageEndpoint> logger)
        : base(pages, authority, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("home-page:write");
        Summary(s =>
        {
            s.Summary = "Publish a tenant's home page";
            s.Description = "Makes the draft of a tenant's home page its published page and records it in the administrative audit. Refused when the tenant holds no draft. The caller must act in the tenant and hold home-page:write there.";
        });
        Tags("Settings");
    }
}
