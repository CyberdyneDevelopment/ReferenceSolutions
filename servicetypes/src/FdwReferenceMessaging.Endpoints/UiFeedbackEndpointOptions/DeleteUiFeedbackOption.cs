using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The DeleteUiFeedback endpoint.</summary>
[WebMcpTool("delete_ui_feedback", "Delete UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "DeleteUiFeedback")]
public class DeleteUiFeedbackOption : UiFeedbackEndpointBase<DeleteUiFeedbackEndpoint>
{
}
