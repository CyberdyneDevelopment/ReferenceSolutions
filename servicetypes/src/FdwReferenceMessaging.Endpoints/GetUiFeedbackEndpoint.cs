using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Reads one UI comment (GET /ui-feedback/{Id}).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetUiFeedbackEndpoint : GetUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUiFeedbackEndpoint"/> class.
    /// </summary>
    public GetUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<GetUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
