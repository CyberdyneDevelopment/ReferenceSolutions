using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Marks all messages as read for the current user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MarkAllReadEndpoint : MarkAllReadEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllReadEndpoint"/> class.
    /// </summary>
    public MarkAllReadEndpoint(IMessageService messageService, ILoggerFactory loggerFactory)
        : base(messageService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
