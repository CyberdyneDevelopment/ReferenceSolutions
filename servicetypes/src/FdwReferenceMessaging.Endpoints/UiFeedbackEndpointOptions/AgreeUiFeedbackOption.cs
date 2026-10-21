using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The AgreeUiFeedback endpoint.</summary>
[WebMcpTool("agree_ui_feedback", "Agree with UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "AgreeUiFeedback")]
public class AgreeUiFeedbackOption : UiFeedbackEndpointBase<AgreeUiFeedbackEndpoint>
{
}
