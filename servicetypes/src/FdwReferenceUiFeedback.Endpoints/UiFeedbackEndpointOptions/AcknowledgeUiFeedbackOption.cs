using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The AcknowledgeUiFeedback endpoint.</summary>
[WebMcpTool("acknowledge_ui_feedback", "Acknowledge UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "AcknowledgeUiFeedback")]
public class AcknowledgeUiFeedbackOption : UiFeedbackEndpointBase<AcknowledgeUiFeedbackEndpoint>
{
}
