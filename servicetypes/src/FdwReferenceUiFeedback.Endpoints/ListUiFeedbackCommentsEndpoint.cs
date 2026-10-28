using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Lists the comments on a UI note (GET /ui-feedback/{Id}/comments).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListUiFeedbackCommentsEndpoint : ListUiFeedbackCommentsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListUiFeedbackCommentsEndpoint"/> class.
    /// </summary>
    public ListUiFeedbackCommentsEndpoint(IUiFeedbackService feedback, ILogger<ListUiFeedbackCommentsEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
