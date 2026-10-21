using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Reopens a resolved UI comment (POST /ui-feedback/{Id}/reopen).
/// </summary>
[ExcludeFromCodeCoverage]
public class ReopenUiFeedbackEndpoint : ReopenUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReopenUiFeedbackEndpoint"/> class.
    /// </summary>
    public ReopenUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<ReopenUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
