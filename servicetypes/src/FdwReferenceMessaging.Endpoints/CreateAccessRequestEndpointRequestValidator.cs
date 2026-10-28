using FastEndpoints;
using FluentValidation;
using Fdw.Services.Messaging.Endpoints.Models;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Refuses an access request that names no resource or no permission (FDW-2863), so every stored request says what it asks for.
/// </summary>
/// <remarks>
/// The request type's two members default to empty text, so a body with nothing in it deserialises and, with no validator, was stored.
/// The justification and the reference are optional. Lives in this assembly because validators are only found in the assemblies of
/// declared endpoints.
/// </remarks>
public sealed class CreateAccessRequestEndpointRequestValidator : Validator<CreateAccessRequestEndpointRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateAccessRequestEndpointRequestValidator"/> class.</summary>
    public CreateAccessRequestEndpointRequestValidator()
    {
        RuleFor(x => x.RequestedResource)
            .NotEmpty().WithMessage("RequestedResource is required");

        RuleFor(x => x.RequestedPermission)
            .NotEmpty().WithMessage("RequestedPermission is required");
    }
}
