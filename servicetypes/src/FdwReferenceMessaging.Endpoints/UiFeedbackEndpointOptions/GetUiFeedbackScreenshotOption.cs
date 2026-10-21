using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The GetUiFeedbackScreenshot endpoint.</summary>
[WebMcpTool("get_ui_feedback_screenshot", "Get UI feedback screenshot.", ReadOnly = true)]
[TypeOption(typeof(UiFeedbackEndpoints), "GetUiFeedbackScreenshot")]
public class GetUiFeedbackScreenshotOption : UiFeedbackEndpointBase<GetUiFeedbackScreenshotEndpoint>
{
}
