using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Lists messages for the current user with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListMessagesEndpoint : ListMessagesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListMessagesEndpoint"/> class.
    /// </summary>
    public ListMessagesEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
