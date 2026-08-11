using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints;

/// <summary>
/// Archives a message.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArchiveMessageEndpoint : ArchiveMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveMessageEndpoint"/> class.
    /// </summary>
    public ArchiveMessageEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
