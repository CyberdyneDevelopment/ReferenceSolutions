using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;

namespace FdwReferenceAuth.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="RedeemPasswordLinkRequest"/> (FDW-941).
/// </summary>
/// <remarks>
/// Checked before the link is used, so a password that fails these rules leaves the link working.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RedeemPasswordLinkRequestValidator : Validator<RedeemPasswordLinkRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedeemPasswordLinkRequestValidator"/> class.
    /// </summary>
    public RedeemPasswordLinkRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("The link has no token");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters");
    }
}
