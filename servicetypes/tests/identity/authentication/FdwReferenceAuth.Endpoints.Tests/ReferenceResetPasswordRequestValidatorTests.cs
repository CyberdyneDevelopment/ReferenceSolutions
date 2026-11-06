using Fdw.Services.Users.Endpoints;
using FdwReferenceAuth.Endpoints.Validators;

namespace FdwReferenceAuth.Endpoints.Tests;

/// <summary>Tests that the host now runs FDW's reset-password rules; before FDW-3572 it ran none.</summary>
public sealed class ReferenceResetPasswordRequestValidatorTests
{
    [Fact]
    public void APasswordOfEightCharactersIsAccepted() =>
        new ReferenceResetPasswordRequestValidator().Validate(new ResetPasswordRequest { IdOrName = "analyst_one", NewPassword = "12345678" }).IsValid.ShouldBeTrue();

    [Fact]
    public void AShortPasswordIsRefused() =>
        new ReferenceResetPasswordRequestValidator().Validate(new ResetPasswordRequest { IdOrName = "analyst_one", NewPassword = "short" }).Errors
            .ShouldContain(error => error.PropertyName == "NewPassword");
}
