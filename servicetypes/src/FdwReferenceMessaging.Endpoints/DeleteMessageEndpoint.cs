using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Deletes a message (DELETE /messages/{Id}).
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteMessageEndpoint : DeleteMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageEndpoint"/> class.
    /// </summary>
    public DeleteMessageEndpoint(IMessageService messageService, ILogger<DeleteMessageEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
