using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Multitenancy.Abstractions;

namespace FdwReferenceAuthentication.AuthorizeSignInMethod.Registration.Tests;

/// <summary>
/// The per-method gate (FDW-910): a principal holding the flow's permission passes, one lacking it is
/// refused with a decision rather than a failure, and settings that name no permission never load.
/// </summary>
[Trait("Category", "Security")]
public sealed class AuthorizeSignInMethodStepTypeTests
{
    private const string PasswordGate = """{"RequiredPermission":"signin:password"}""";

    [Fact]
    public void SettingsNamingAPermissionAreAccepted()
    {
        var step = new AuthorizeSignInMethodStepType();

        step.ValidateConfiguration(PasswordGate).IsSuccess.ShouldBeTrue();
        step.RequiredPermission(PasswordGate).Value.ShouldBe("signin:password");
    }

    [Fact]
    public void SettingsWrittenForAPermissionReadBackAsThatPermission()
    {
        var step = new AuthorizeSignInMethodStepType();

        var written = step.ConfigurationRequiring("signin:password");

        written.ShouldBe(PasswordGate);
        step.RequiredPermission(written).Value.ShouldBe("signin:password");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ not json")]
    [InlineData("{}")]
    [InlineData("""{"RequiredPermission":"  "}""")]
    public void MalformedOrEmptySettingsAreRefused(string? configuration)
    {
        new AuthorizeSignInMethodStepType().ValidateConfiguration(configuration).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task APrincipalHoldingThePermissionPassesWithoutDeciding()
    {
        var result = await new AuthorizeSignInMethodStepType().Execute(
            Context(("signin:password", ClaimSources.Local)), PasswordGate, Mock.Of<ITenantProvider>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Permitting is the eligibility step's decision; this step contributes none when it admits.
        result.Value.ShouldBeOfType<StepOutcome.Contributed>().Contribution.Decision.ShouldBeNull();
    }

    [Fact]
    public async Task APrincipalLackingThePermissionIsRefusedByDecisionNotFailure()
    {
        var result = await new AuthorizeSignInMethodStepType().Execute(
            Context(("datasets:read", ClaimSources.Local)), PasswordGate, Mock.Of<ITenantProvider>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var decision = result.Value.ShouldBeOfType<StepOutcome.Contributed>().Contribution.Decision.ShouldNotBeNull();
        decision.Permitted.ShouldBeFalse();
        decision.Reason.ShouldContain("signin:password");
    }

    [Fact]
    public async Task APermissionAnOutsideProviderAssertedDoesNotCount()
    {
        var result = await new AuthorizeSignInMethodStepType().Execute(
            Context(("signin:password", ClaimSources.External)), PasswordGate, Mock.Of<ITenantProvider>(), TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<StepOutcome.Contributed>().Contribution.Decision.ShouldNotBeNull().Permitted.ShouldBeFalse();
    }

    [Fact]
    public async Task RunWithoutItsFlowsSettingsItRefusesToAnswer()
    {
        var result = await new AuthorizeSignInMethodStepType().Execute(
            Context(("signin:password", ClaimSources.Local)), Mock.Of<ITenantProvider>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ItRunsOnlyAfterPermissionsAreBaked()
    {
        new AuthorizeSignInMethodStepType().Requires.ShouldContain(ContextElements.Claims);
    }

    private static AuthenticationContext Context(params (string Permission, IClaimSource Source)[] claims) => new()
    {
        Principal = new Principal { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() },
        Claims = ClaimSet.Empty.Add(claims.Select(c => new Claim { Type = ClaimDefinitions.perm.Name, Value = c.Permission, Source = c.Source })),
    };
}
