using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Resolves a UI comment (POST /ui-feedback/{Id}/resolve).
/// </summary>
[ExcludeFromCodeCoverage]
public class ResolveUiFeedbackEndpoint : ResolveUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveUiFeedbackEndpoint"/> class.
    /// </summary>
    public ResolveUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<ResolveUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
