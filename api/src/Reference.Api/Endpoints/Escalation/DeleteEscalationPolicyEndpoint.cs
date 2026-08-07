using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Escalation;

/// <summary>
/// Deletes an escalation policy.
/// </summary>
public sealed class DeleteEscalationPolicyEndpoint : DeleteEscalationPolicyEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEscalationPolicyEndpoint"/> class.
    /// </summary>
    public DeleteEscalationPolicyEndpoint(IEscalationService escalationService, ILogger<DeleteEscalationPolicyEndpointBase>? logger)
        : base(escalationService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
