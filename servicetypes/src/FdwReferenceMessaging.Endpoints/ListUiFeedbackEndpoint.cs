using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Lists the UI comments the caller reaches (GET /ui-feedback).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListUiFeedbackEndpoint : ListUiFeedbackEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListUiFeedbackEndpoint"/> class.
    /// </summary>
    public ListUiFeedbackEndpoint(IUiFeedbackService feedback, ILogger<ListUiFeedbackEndpoint> logger)
        : base(feedback, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("UiFeedback");
    }
}
