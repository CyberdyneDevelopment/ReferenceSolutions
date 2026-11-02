using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The GetUiFeedback endpoint.</summary>
[WebMcpTool("get_ui_feedback", "Get UI feedback.", ReadOnly = true)]
[TypeOption(typeof(UiFeedbackEndpoints), "GetUiFeedback")]
public class GetUiFeedbackOption : UiFeedbackEndpointBase<GetUiFeedbackEndpoint>
{
}
