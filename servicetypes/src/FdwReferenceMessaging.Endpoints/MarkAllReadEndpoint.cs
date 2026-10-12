using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Marks all messages as read for the current user.
/// </summary>
[ExcludeFromCodeCoverage]
public class MarkAllReadEndpoint : MarkAllReadEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllReadEndpoint"/> class.
    /// </summary>
    public MarkAllReadEndpoint(IMessageService messageService, ILogger<MarkAllReadEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
