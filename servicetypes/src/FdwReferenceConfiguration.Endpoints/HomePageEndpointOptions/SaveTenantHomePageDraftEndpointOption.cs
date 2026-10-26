using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The SaveTenantHomePageDraft endpoint (FDW-2471).</summary>
[WebMcpTool("save_tenant_home_page_draft", "Save the draft of a tenant's home page.")]
[TypeOption(typeof(HomePageEndpoints), "SaveTenantHomePageDraft")]
public class SaveTenantHomePageDraftEndpointOption : HomePageEndpointBase<SaveTenantHomePageDraftEndpoint>
{
}
