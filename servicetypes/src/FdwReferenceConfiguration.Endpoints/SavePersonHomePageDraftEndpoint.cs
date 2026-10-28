using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: save the caller's own home page draft (FDW-2471).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SavePersonHomePageDraftEndpoint : SavePersonHomePageDraftEndpointBase
{
    /// <inheritdoc />
    public SavePersonHomePageDraftEndpoint(
        IHomePages pages,
        IAuthenticationContextAccessor authContext,
        IAdministrativeAudit audit,
        ILogger<SavePersonHomePageDraftEndpoint> logger)
        : base(pages, authContext, audit, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Save the caller's own home page draft";
            s.Description = "Saves the draft of the signed-in person's own home page, apart from its published page. The body is the page. Needs no right; a person's own page is not recorded in the administrative audit.";
        });
        Tags("Settings");
    }
}
