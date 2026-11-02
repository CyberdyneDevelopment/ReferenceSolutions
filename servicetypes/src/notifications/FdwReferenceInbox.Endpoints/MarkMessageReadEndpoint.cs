using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Marks a message as read.
/// </summary>
[ExcludeFromCodeCoverage]
public class MarkMessageReadEndpoint : MarkMessageReadEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkMessageReadEndpoint"/> class.
    /// </summary>
    public MarkMessageReadEndpoint(IMessageService messageService, ILogger<MarkMessageReadEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
