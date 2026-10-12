using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Lists messages for the current user with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListMessagesEndpoint : ListMessagesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListMessagesEndpoint"/> class.
    /// </summary>
    public ListMessagesEndpoint(IMessageService messageService, ILogger<ListMessagesEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
