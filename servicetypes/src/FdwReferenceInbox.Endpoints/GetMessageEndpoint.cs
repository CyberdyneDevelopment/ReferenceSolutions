using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Gets a single message by its identifier.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetMessageEndpoint : GetMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMessageEndpoint"/> class.
    /// </summary>
    public GetMessageEndpoint(IMessageService messageService, ILogger<GetMessageEndpoint> logger)
        : base(messageService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
