using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Authorization.Endpoints.Validators;

namespace FdwReferenceAuth.Endpoints.Validators;

/// <summary>
/// The create-role rules of this host: FDW's (<see cref="CreateRoleRequestValidator"/>), with nothing the host adds (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceCreateRoleRequestValidator : CreateRoleRequestValidator;

/// <summary>
/// Validator for <see cref="UpdateRoleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateRoleRequestValidator : Validator<UpdateRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRoleRequestValidator"/> class.
    /// </summary>
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required in the route");

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName)
                .MaximumLength(200)
                .WithMessage("Display name cannot exceed 200 characters");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");
        });
    }
}

/// <summary>
/// Validator for <see cref="GetRoleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetRoleRequestValidator : Validator<GetRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetRoleRequestValidator"/> class.
    /// </summary>
    public GetRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required");
    }
}

/// <summary>
/// Validator for <see cref="SetRolePermissionsRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetRolePermissionsRequestValidator : Validator<SetRolePermissionsRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetRolePermissionsRequestValidator"/> class.
    /// </summary>
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required in the route");

        // Why: empty PermissionNames is a valid request — semantically "clear all permissions
        // for this role". The 'NotEmpty' rule made the validator reject this before the auth
        // policy could run, returning 400 where the test expected 403.
        RuleForEach(x => x.PermissionNames)
            .NotEmpty()
            .WithMessage("Permission name cannot be empty")
            .MaximumLength(200)
            .WithMessage("Permission name cannot exceed 200 characters");
    }
}
