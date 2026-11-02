using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The ReopenUiFeedback endpoint.</summary>
[WebMcpTool("reopen_ui_feedback", "Reopen UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "ReopenUiFeedback")]
public class ReopenUiFeedbackOption : UiFeedbackEndpointBase<ReopenUiFeedbackEndpoint>
{
}
