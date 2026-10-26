using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Reads the picture of the screen a UI comment was left on (GET /ui-feedback/{Id}/screenshot).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetUiFeedbackScreenshotEndpoint : GetUiFeedbackScreenshotEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUiFeedbackScreenshotEndpoint"/> class.
    /// </summary>
    public GetUiFeedbackScreenshotEndpoint(IUiFeedbackService feedback, ILogger<GetUiFeedbackScreenshotEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
