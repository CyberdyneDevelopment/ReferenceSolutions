using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;
using Microsoft.Extensions.Logging;

namespace ReferenceEscalation.Endpoints;

/// <summary>
/// Updates an escalation policy.
/// </summary>
public sealed class UpdateEscalationPolicyEndpoint : UpdateEscalationPolicyEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEscalationPolicyEndpoint"/> class.
    /// </summary>
    public UpdateEscalationPolicyEndpoint(IEscalationService escalationService, ILogger<UpdateEscalationPolicyEndpointBase>? logger)
        : base(escalationService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
