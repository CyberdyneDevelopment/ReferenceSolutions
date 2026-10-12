using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Dismisses a message.
/// </summary>
[ExcludeFromCodeCoverage]
public class DismissMessageEndpoint : DismissMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DismissMessageEndpoint"/> class.
    /// </summary>
    public DismissMessageEndpoint(IMessageService messageService, ILogger<DismissMessageEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
