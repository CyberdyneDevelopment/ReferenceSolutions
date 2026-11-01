using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Sends a message into a conversation thread.
/// </summary>
[ExcludeFromCodeCoverage]
public class SendMessageEndpoint : SendMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageEndpoint"/> class.
    /// </summary>
    public SendMessageEndpoint(IMessageService messageService, ILogger<SendMessageEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
