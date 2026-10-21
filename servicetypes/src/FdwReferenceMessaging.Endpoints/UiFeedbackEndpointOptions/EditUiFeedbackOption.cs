using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The EditUiFeedback endpoint.</summary>
[WebMcpTool("edit_ui_feedback", "Edit UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "EditUiFeedback")]
public class EditUiFeedbackOption : UiFeedbackEndpointBase<EditUiFeedbackEndpoint>
{
}
