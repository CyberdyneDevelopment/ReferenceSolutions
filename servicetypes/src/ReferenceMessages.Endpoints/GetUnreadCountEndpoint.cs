using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints;

/// <summary>
/// Gets the unread message count for the current user.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetUnreadCountEndpoint : GetUnreadCountEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUnreadCountEndpoint"/> class.
    /// </summary>
    public GetUnreadCountEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
