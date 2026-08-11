using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints;

/// <summary>
/// Dismisses a message.
/// </summary>
[ExcludeFromCodeCoverage]
public class DismissMessageEndpoint : DismissMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DismissMessageEndpoint"/> class.
    /// </summary>
    public DismissMessageEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
