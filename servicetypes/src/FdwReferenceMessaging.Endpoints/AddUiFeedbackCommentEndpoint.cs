using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Adds a comment to a UI note (POST /ui-feedback/{Id}/comments).
/// </summary>
[ExcludeFromCodeCoverage]
public class AddUiFeedbackCommentEndpoint : AddUiFeedbackCommentEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddUiFeedbackCommentEndpoint"/> class.
    /// </summary>
    public AddUiFeedbackCommentEndpoint(IUiFeedbackService feedback, ILogger<AddUiFeedbackCommentEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
