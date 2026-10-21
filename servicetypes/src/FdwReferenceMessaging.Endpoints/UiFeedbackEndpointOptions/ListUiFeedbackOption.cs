using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The ListUiFeedback endpoint.</summary>
[WebMcpTool("list_ui_feedback", "List UI feedback.", ReadOnly = true)]
[TypeOption(typeof(UiFeedbackEndpoints), "ListUiFeedback")]
public class ListUiFeedbackOption : UiFeedbackEndpointBase<ListUiFeedbackEndpoint>
{
}
