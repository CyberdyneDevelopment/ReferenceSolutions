using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Agrees with a UI comment as the caller (POST /ui-feedback/{Id}/agree).
/// </summary>
[ExcludeFromCodeCoverage]
public class AgreeUiFeedbackEndpoint : AgreeUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgreeUiFeedbackEndpoint"/> class.
    /// </summary>
    public AgreeUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<AgreeUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
