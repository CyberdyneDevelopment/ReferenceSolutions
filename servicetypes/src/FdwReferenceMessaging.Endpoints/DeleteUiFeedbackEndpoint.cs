using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Deletes the caller's own UI comment (DELETE /ui-feedback/{Id}).
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteUiFeedbackEndpoint : DeleteUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUiFeedbackEndpoint"/> class.
    /// </summary>
    public DeleteUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<DeleteUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
