using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Clients.Models;

namespace FdwReferenceAuth.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateUserRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateUserRequestValidator : Validator<CreateUserRequest>
{
    // The characters an email-form username may carry; see UsernameRules.
    private static bool EmailFormCarriesOnlySafeCharacters(string username)
        => username.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '+' or '@');

    // What a username may be: an identifier, or an email address for an account that signs in
    // through an identity provider (FDW-1056). The two forms are told apart by the '@', and each says
    // what is wrong with it. The email form is also held to characters every consumer of the name can
    // carry: the name is a URL path segment (/users/{IdOrName}), a VARCHAR key, and the left half of
    // the '{username}:{tenantId}' membership name, so no ':' '/' '%' or non-ASCII.
    private void UsernameRules()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters");

        When(x => !string.IsNullOrEmpty(x.Username) && !x.Username.Contains('@', StringComparison.Ordinal), () =>
            RuleFor(x => x.Username)
                .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
                .WithMessage("Username must start with a letter and contain only letters, numbers, or underscores, or be an email address"));

        When(x => !string.IsNullOrEmpty(x.Username) && x.Username.Contains('@', StringComparison.Ordinal), () =>
            RuleFor(x => x.Username)
                .EmailAddress()
                .WithMessage("A username containing '@' must be a valid email address")
                .Must(EmailFormCarriesOnlySafeCharacters)
                .WithMessage("An email address used as a username may contain only ASCII letters, numbers and the characters . _ - + @"));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator"/> class.
    /// </summary>
    public CreateUserRequestValidator()
    {
        UsernameRules();

        // An account signs in with a password, through an external provider, with an API key, or any
        // combination; what it may not have is none of them.
        RuleFor(x => x.SignInMethods)
            .NotNull()
            .WithMessage("At least one sign-in method is required")
            .Must(m => m is not null && (m.Password is not null || m.External is not null || m.ApiKey is not null))
            .WithMessage("At least one sign-in method is required: a password, an external identity, or an API key");

        When(x => x.SignInMethods?.Password is not null, () =>
        {
            RuleFor(x => x.SignInMethods.Password!.InitialPassword)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters");
        });

        When(x => x.SignInMethods?.External is not null, () =>
        {
            RuleFor(x => x.SignInMethods.External!.Issuer)
                .NotEmpty()
                .WithMessage("An external identity needs the issuer that asserts it");

            // An address is not a binding key, but it is an invitation key (FDW-910): exactly one.
            RuleFor(x => x.SignInMethods.External!)
                .Must(e => string.IsNullOrWhiteSpace(e.Subject) != string.IsNullOrWhiteSpace(e.InvitedEmail))
                .WithName("SignInMethods.External.Subject")
                .WithMessage("An external identity needs exactly one of the issuer's subject identifier (bind now) or an invitedEmail (invite)");

            When(x => !string.IsNullOrWhiteSpace(x.SignInMethods.External!.InvitedEmail), () =>
                RuleFor(x => x.SignInMethods.External!.InvitedEmail)
                    .EmailAddress()
                    .WithMessage("invitedEmail must be an email address"));
        });

        When(x => x.SignInMethods?.ApiKey is not null, () =>
        {
            RuleFor(x => x.SignInMethods.ApiKey!.Label)
                .NotEmpty()
                .WithMessage("An API key needs a label");

            RuleFor(x => x.SignInMethods.ApiKey!.Kind)
                .Must(k => string.Equals(k, ApiKeyKinds.User, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(k, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase))
                .WithMessage("API key kind must be 'user' or 'agent'");
        });

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(320)
                .WithMessage("Email cannot exceed 320 characters");
        });

        // Stated, not defaulted: there is no role every deployment has, so the caller names them (FDW-910).
        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles is required: name the roles the account holds in its tenant, or send [] for none");

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
        // Why: UpdateUserRequest binds {IdOrName} as string from the route.
        RuleFor(x => x.IdOrName)
            .NotEmpty()
            .WithMessage("A user id or name is required");

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
