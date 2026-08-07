using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Escalation;

/// <summary>
/// Creates an escalation policy.
/// </summary>
public sealed class CreateEscalationPolicyEndpoint : CreateEscalationPolicyEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEscalationPolicyEndpoint"/> class.
    /// </summary>
    public CreateEscalationPolicyEndpoint(IEscalationService escalationService, ILogger<CreateEscalationPolicyEndpointBase>? logger)
        : base(escalationService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
