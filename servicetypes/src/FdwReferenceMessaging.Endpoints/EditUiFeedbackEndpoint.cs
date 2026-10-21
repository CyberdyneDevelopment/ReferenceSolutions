using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Changes the note of the caller's own UI comment (PATCH /ui-feedback/{Id}).
/// </summary>
[ExcludeFromCodeCoverage]
public class EditUiFeedbackEndpoint : EditUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditUiFeedbackEndpoint"/> class.
    /// </summary>
    public EditUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<EditUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
