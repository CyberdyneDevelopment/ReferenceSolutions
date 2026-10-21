using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Acknowledges a UI comment as the caller (POST /ui-feedback/{Id}/acknowledge).
/// </summary>
[ExcludeFromCodeCoverage]
public class AcknowledgeUiFeedbackEndpoint : AcknowledgeUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcknowledgeUiFeedbackEndpoint"/> class.
    /// </summary>
    public AcknowledgeUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<AcknowledgeUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
