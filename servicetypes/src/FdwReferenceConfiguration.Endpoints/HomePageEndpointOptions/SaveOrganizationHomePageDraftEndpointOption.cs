using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The SaveOrganizationHomePageDraft endpoint (FDW-2325).</summary>
[WebMcpTool("save_organization_home_page_draft", "Save the draft of an organization's home page.")]
[TypeOption(typeof(HomePageEndpoints), "SaveOrganizationHomePageDraft")]
public class SaveOrganizationHomePageDraftEndpointOption : HomePageEndpointBase<SaveOrganizationHomePageDraftEndpoint>
{
}
