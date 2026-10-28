using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The SavePersonHomePageDraft endpoint (FDW-2471).</summary>
[WebMcpTool("save_my_home_page_draft", "Save the draft of the signed-in person's own home page.")]
[TypeOption(typeof(HomePageEndpoints), "SavePersonHomePageDraft")]
public class SavePersonHomePageDraftEndpointOption : HomePageEndpointBase<SavePersonHomePageDraftEndpoint>
{
}
