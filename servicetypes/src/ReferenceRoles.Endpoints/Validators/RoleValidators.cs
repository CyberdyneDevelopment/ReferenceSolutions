using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Authorization.Endpoints;

namespace ReferenceRoles.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateRoleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateRoleRequestValidator : Validator<CreateRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleRequestValidator"/> class.
    /// </summary>
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MinimumLength(3)
            .WithMessage("Role name must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, underscores, or hyphens");

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

        When(x => !string.IsNullOrEmpty(x.ParentRoleName), () =>
        {
            RuleFor(x => x.ParentRoleName)
                .MaximumLength(100)
                .WithMessage("Parent role name cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
                .WithMessage("Parent role name must start with a letter and contain only letters, numbers, underscores, or hyphens");
        });
    }
}

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

        When(x => !string.IsNullOrEmpty(x.ParentRoleName), () =>
        {
            RuleFor(x => x.ParentRoleName)
                .MaximumLength(100)
                .WithMessage("Parent role name cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
                .WithMessage("Parent role name must start with a letter and contain only letters, numbers, underscores, or hyphens");
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
