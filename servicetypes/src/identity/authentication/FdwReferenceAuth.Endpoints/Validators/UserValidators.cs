using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Endpoints.Validators;
using Fdw.Services.Users.Clients.Models;

namespace FdwReferenceAuth.Endpoints.Validators;

/// <summary>
/// The create-user rules of this host: FDW's (<see cref="CreateUserRequestValidator{TRequest}"/>), with nothing the host adds (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceCreateUserRequestValidator : CreateUserRequestValidator<CreateUserRequest>;

/// <summary>
/// The reset-password rules of this host: FDW's (<see cref="ResetPasswordRequestValidator{TRequest}"/>), with nothing the host adds. Before
/// this class the host ran no validator on the request (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceResetPasswordRequestValidator : ResetPasswordRequestValidator<ResetPasswordRequest>;

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
        // Why: UpdateUserRequest binds {IdOrName} as string from the route.
        RuleFor(x => x.IdOrName)
            .NotEmpty()
            .WithMessage("A user id or name is required");

        // Why: PATCH with no updateable fields is a no-op. Require Email or IsActive.
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
