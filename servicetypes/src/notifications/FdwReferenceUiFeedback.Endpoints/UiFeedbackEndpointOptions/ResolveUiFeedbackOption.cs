using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The ResolveUiFeedback endpoint.</summary>
[WebMcpTool("resolve_ui_feedback", "Resolve UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "ResolveUiFeedback")]
public class ResolveUiFeedbackOption : UiFeedbackEndpointBase<ResolveUiFeedbackEndpoint>
{
}
