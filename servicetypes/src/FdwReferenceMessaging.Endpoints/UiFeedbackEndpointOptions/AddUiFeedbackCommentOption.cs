using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The AddUiFeedbackComment endpoint.</summary>
[WebMcpTool("add_ui_feedback_comment", "Comment on UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "AddUiFeedbackComment")]
public class AddUiFeedbackCommentOption : UiFeedbackEndpointBase<AddUiFeedbackCommentEndpoint>
{
}
