using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The CreateUiFeedback endpoint.</summary>
[WebMcpTool("create_ui_feedback", "Create UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "CreateUiFeedback")]
public class CreateUiFeedbackOption : UiFeedbackEndpointBase<CreateUiFeedbackEndpoint>
{
}
