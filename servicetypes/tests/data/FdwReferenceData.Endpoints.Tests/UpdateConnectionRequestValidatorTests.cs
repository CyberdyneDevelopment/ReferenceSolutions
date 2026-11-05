using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.Endpoints;
using FdwReferenceConnections.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// FDW-1177: the live connection update validator counts the members <see cref="UpdateConnectionChanges"/> lists, so credentials
/// sent by themselves are an update, and a member a connection does not store is not.
/// </summary>
public sealed class UpdateConnectionRequestValidatorTests
{
    [Fact]
    public void CredentialsSentAloneAreAnAcceptedUpdate()
    {
        var request = new UpdateConnectionRequest
        {
            Name = "ConsoleCredProbe",
            Credentials = new Dictionary<string, string> { ["UsernameSecretName"] = "user", ["PasswordSecretName"] = "secret" },
        };

        new ReferenceUpdateConnectionRequestValidator().Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AHealthCheckSettingOrADefaultValuePolicySentAloneIsAnAcceptedUpdate()
    {
        var validator = new ReferenceUpdateConnectionRequestValidator();

        validator.Validate(new UpdateConnectionRequest { Name = "Orders", HealthCheckEnabled = true }).IsValid.ShouldBeTrue();
        validator.Validate(new UpdateConnectionRequest { Name = "Orders", DefaultValuePolicy = "Strict" }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ABodyThatStatesNothingIsRefusedWithTheMessageThatNamesEveryMember()
    {
        var result = new ReferenceUpdateConnectionRequestValidator().Validate(new UpdateConnectionRequest { Name = "Orders" });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorCode).ShouldContain(UpdateConnectionChanges.NothingStatedCode);
        result.Errors.Select(error => error.ErrorMessage).ShouldContain(UpdateConnectionChanges.RefusalMessage);
        UpdateConnectionChanges.RefusalMessage.ShouldContain(nameof(UpdateConnectionRequest.Credentials));
    }

    [Fact]
    public void AMemberTheConnectionDoesNotStoreIsNotAnUpdateOnItsOwn()
    {
        var result = new ReferenceUpdateConnectionRequestValidator().Validate(new UpdateConnectionRequest { Name = "Orders", IsActive = true });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.ErrorCode).ShouldContain(UpdateConnectionChanges.NothingStatedCode);
    }

    [Fact]
    public void ValuesTheBodyBuilderDropsAreNotAnUpdate()
    {
        var request = new UpdateConnectionRequest { Name = "Orders", Server = string.Empty, Headers = new Dictionary<string, string?>() };

        new ReferenceUpdateConnectionRequestValidator().Validate(request).IsValid.ShouldBeFalse();
    }
}
