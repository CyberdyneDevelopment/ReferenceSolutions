using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Archives a message.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArchiveMessageEndpoint : ArchiveMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveMessageEndpoint"/> class.
    /// </summary>
    public ArchiveMessageEndpoint(IMessageService messageService, ILogger<ArchiveMessageEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
