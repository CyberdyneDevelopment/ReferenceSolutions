using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Leaves a UI comment (POST /ui-feedback).
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateUiFeedbackEndpoint : CreateUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUiFeedbackEndpoint"/> class.
    /// </summary>
    public CreateUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<CreateUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
