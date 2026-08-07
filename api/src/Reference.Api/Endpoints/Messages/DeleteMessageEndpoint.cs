using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Deletes a message (DELETE /messages/{Id}).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteMessageEndpoint : DeleteMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageEndpoint"/> class.
    /// </summary>
    public DeleteMessageEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
