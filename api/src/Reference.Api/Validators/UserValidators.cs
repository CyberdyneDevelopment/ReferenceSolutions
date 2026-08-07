using System.Diagnostics.CodeAnalysis;
using System;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Clients.Models;

namespace Reference.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateUserRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateUserRequestValidator : Validator<CreateUserRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator"/> class.
    /// </summary>
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage("Username must start with a letter and contain only letters, numbers, or underscores");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters");

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(320)
                .WithMessage("Email cannot exceed 320 characters");
        });

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .WithMessage("Role name cannot be empty")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters");
    }
}

/// <summary>
/// Validator for <see cref="UpdateUserRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateUserRequestValidator : Validator<UpdateUserRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRequestValidator"/> class.
    /// </summary>
    public UpdateUserRequestValidator()
    {
        // Why: UpdateUserRequest now binds {Name} as string from the route.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("A user name is required");

        // Why: PUT with no updateable fields is a no-op. Require Email or IsActive.
        RuleFor(x => x)
            .Must(req => req.Email is not null || req.IsActive.HasValue)
            .WithMessage("At least one updateable field must be supplied (Email or IsActive)");

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(320)
                .WithMessage("Email cannot exceed 320 characters");
        });
    }
}

/// <summary>
/// Validator for <see cref="UserScopedRequest"/>.
/// </summary>
/// <remarks>
/// API-118: route accepts {IdOrName} — either a Guid or a username. The endpoint
/// handler routes by Guid.TryParse on IdOrName, so validating UserId != Guid.Empty
/// here would reject every username lookup. Validate the raw IdOrName instead.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class UserScopedRequestValidator : Validator<UserScopedRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserScopedRequestValidator"/> class.
    /// </summary>
    public UserScopedRequestValidator()
    {
        RuleFor(x => x.IdOrName)
            .NotEmpty()
            .WithMessage("A user id or username is required");
    }
}
