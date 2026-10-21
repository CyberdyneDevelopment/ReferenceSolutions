using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Takes back the caller's agreement with a UI comment (DELETE /ui-feedback/{Id}/agree).
/// </summary>
[ExcludeFromCodeCoverage]
public class TakeBackUiFeedbackAgreementEndpoint : TakeBackUiFeedbackAgreementEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TakeBackUiFeedbackAgreementEndpoint"/> class.
    /// </summary>
    public TakeBackUiFeedbackAgreementEndpoint(IUiFeedbackService feedback, ILogger<TakeBackUiFeedbackAgreementEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
