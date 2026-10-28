using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The TakeBackUiFeedbackAgreement endpoint.</summary>
[WebMcpTool("take_back_ui_feedback_agreement", "Take back the agreement with UI feedback.")]
[TypeOption(typeof(UiFeedbackEndpoints), "TakeBackUiFeedbackAgreement")]
public class TakeBackUiFeedbackAgreementOption : UiFeedbackEndpointBase<TakeBackUiFeedbackAgreementEndpoint>
{
}
