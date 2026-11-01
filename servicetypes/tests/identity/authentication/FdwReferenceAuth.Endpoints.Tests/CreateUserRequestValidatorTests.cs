using Fdw.Services.Users.Clients.Models;
using FdwReferenceAuth.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace FdwReferenceAuth.Endpoints.Tests;

/// <summary>
/// What a username may be (FDW-1056): an identifier, or an email address for an account that signs in
/// through an identity provider. Each refusal names the rule it broke.
/// </summary>
public class CreateUserRequestValidatorTests
{
    private static readonly CreateUserRequestValidator Validator = new();

    [Theory]
    [InlineData("newuser")]
    [InlineData("new_user1")]
    [InlineData("jane.doe@example.com")]
    [InlineData("jane+idp@example.co.uk")]
    [InlineData("a_b-c@x.io")]
    public void AnIdentifierOrAnEmailAddressIsAValidUsername(string username)
    {
        Validator.Validate(Account(username)).Errors.ShouldNotContain(e => e.PropertyName == "Username");
    }

    [Theory]
    [InlineData("9lives", "must start with a letter")]
    [InlineData("has space", "must start with a letter")]
    [InlineData("under-score", "must start with a letter")]
    [InlineData("jane@", "valid email address")]
    [InlineData("@example.com", "valid email address")]
    [InlineData("jane@@example.com", "valid email address")]
    public void AUsernameThatIsNeitherFormIsRefusedWithTheRuleItBroke(string username, string reason)
    {
        Validator.Validate(Account(username)).Errors
            .Where(e => e.PropertyName == "Username")
            .Select(e => e.ErrorMessage)
            .ShouldContain(message => message.Contains(reason, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("jane:x@example.com")]
    [InlineData("jane/x@example.com")]
    [InlineData("jane%40x@example.com")]
    [InlineData("jane x@example.com")]
    [InlineData("jané@example.com")]
    public void AnEmailAddressWithACharacterTheNameCannotCarryIsRefused(string username)
    {
        // The name is a URL path segment, a VARCHAR key and half of the '{username}:{tenantId}' membership name.
        Validator.Validate(Account(username)).Errors
            .Where(e => e.PropertyName == "Username")
            .Select(e => e.ErrorMessage)
            .ShouldContain(message => message.Contains("may contain only ASCII letters", StringComparison.Ordinal));
    }

    [Fact]
    public void TheLengthLimitsStillApplyToAnEmailAddress()
    {
        var tooLong = new string('a', 96) + "@x.com";
        var tooShort = "ab";

        Validator.Validate(Account(tooLong)).Errors.ShouldContain(
            e => e.PropertyName == "Username" && e.ErrorMessage.Contains("cannot exceed 100", StringComparison.Ordinal));
        Validator.Validate(Account(tooShort)).Errors.ShouldContain(
            e => e.PropertyName == "Username" && e.ErrorMessage.Contains("at least 3", StringComparison.Ordinal));
    }

    [Fact]
    public void AnIdentityProviderAccountTypedAsOneEmailAddressIsAccepted()
    {
        // The console sends the one value the admin typed as username, email and invitedEmail.
        const string address = "jane.doe@example.com";
        var request = Account(address);
        request.Email = address;
        request.SignInMethods = new SignInMethodsRequest
        {
            External = new ExternalSignInMethod { Issuer = "https://login.example.com/", InvitedEmail = address },
        };

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    private static CreateUserRequest Account(string username) => new()
    {
        Username = username,
        SignInMethods = new SignInMethodsRequest { Password = new PasswordSignInMethod { InitialPassword = "Initial#Pass1" } },
        Roles = [],
    };
}
