using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The ListUiFeedbackComments endpoint.</summary>
[WebMcpTool("list_ui_feedback_comments", "List the comments on UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "ListUiFeedbackComments")]
public class ListUiFeedbackCommentsOption : UiFeedbackEndpointBase<ListUiFeedbackCommentsEndpoint>
{
}
