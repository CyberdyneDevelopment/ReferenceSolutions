using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Gets a single message by its identifier.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetMessageEndpoint : GetMessageEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMessageEndpoint"/> class.
    /// </summary>
    public GetMessageEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
