using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Authorization.Endpoints;

namespace ReferenceUsers.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="AssignRoleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AssignRoleRequestValidator : Validator<AssignRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignRoleRequestValidator"/> class.
    /// </summary>
    public AssignRoleRequestValidator()
    {
        // Why IdOrName: AssignRoleRequest binds the user through UserScopedRequest, which carries a
        // single {IdOrName} route value — a Guid string OR a user name — not a typed Guid UserId.
        RuleFor(x => x.IdOrName)
            .NotEmpty()
            .WithMessage("A user id or name is required");

        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, underscores, or hyphens");
    }
}

/// <summary>
/// Validator for <see cref="RevokeRoleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RevokeRoleRequestValidator : Validator<RevokeRoleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeRoleRequestValidator"/> class.
    /// </summary>
    public RevokeRoleRequestValidator()
    {
        // Why IdOrName: RevokeRoleRequest binds the user through UserScopedRequest, whose single
        // {IdOrName} route value accepts a Guid string or a user name.
        RuleFor(x => x.IdOrName)
            .NotEmpty()
            .WithMessage("A user id or name is required");

        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, underscores, or hyphens");
    }
}
