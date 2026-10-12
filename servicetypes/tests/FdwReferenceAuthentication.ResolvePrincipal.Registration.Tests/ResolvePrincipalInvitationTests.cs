using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration.Tests;

/// <summary>
/// A flow opted in to invitations binds a pre-created account to the first sign-in carrying the invited,
/// verified address; a flow that did not opt in never looks (FDW-910).
/// </summary>
[Trait("Category", "Security")]
public sealed class ResolvePrincipalInvitationTests
{
    private const string Issuer = "https://login.example/tenant/v2.0";
    private const string ByVerifiedClaim = """{"Invitations":{"EmailClaim":"email","EmailVerifiedClaim":"email_verified"}}""";
    private const string ByTrustedIssuer = """{"Invitations":{"EmailClaim":"email","TrustIssuerEmail":true}}""";
    private static readonly Principal Invited = new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };

    [Theory]
    [InlineData(ByVerifiedClaim)]
    [InlineData(ByTrustedIssuer)]
    public void CompleteInvitationSettingsAreAccepted(string configuration)
        => new ResolvePrincipalStepType().ValidateConfiguration(configuration).IsSuccess.ShouldBeTrue();

    [Fact]
    public void NoSettingsIsTheStepAsItAlwaysWas()
        => new ResolvePrincipalStepType().ValidateConfiguration(null).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{}")]
    [InlineData("""{"Invitations":{"EmailVerifiedClaim":"email_verified"}}""")]
    [InlineData("""{"Invitations":{"EmailClaim":"email"}}""")]
    [InlineData("""{"Invitations":{"EmailClaim":"email","EmailVerifiedClaim":"email_verified","TrustIssuerEmail":true}}""")]
    public void IncompleteOrAmbiguousSettingsAreRefused(string configuration)
        => new ResolvePrincipalStepType().ValidateConfiguration(configuration).IsFailure.ShouldBeTrue();

    [Fact]
    public async Task AVerifiedAddressRedeemsTheInvitation()
    {
        var (step, bindings) = Step();

        var result = await step.Execute(
            Context(("email", "vendor@partner.example"), ("email_verified", "true")), ByVerifiedClaim,
            Tenants(), TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<StepOutcome.Contributed>().Contribution.Principal.ShouldBe(Invited);
        bindings.Verify(b => b.RedeemInvitation(Issuer, "oid-123", "vendor@partner.example", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnUnverifiedAddressRedeemsNothing()
    {
        var (step, bindings) = Step();

        await step.Execute(
            Context(("email", "vendor@partner.example"), ("email_verified", "false")), ByVerifiedClaim,
            Tenants(), TestContext.Current.CancellationToken);

        bindings.Verify(b => b.RedeemInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AFlowThatDidNotOptInNeverLooksForAnInvitation()
    {
        var (step, bindings) = Step();

        await step.Execute(
            Context(("email", "vendor@partner.example"), ("email_verified", "true")), configuration: null,
            Tenants(), TestContext.Current.CancellationToken);

        bindings.Verify(b => b.RedeemInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Past an invitation lies provisioning, which these tests do not exercise: it is answered with a
    // failure, so reaching it is visible and never mistaken for a redemption.
    private static ITenantProvider Tenants()
    {
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetAllTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ITenant>>.Failure(new Fdw.Messages.GenericMessage("provisioning is not under test")));
        return tenants.Object;
    }

    private static AuthenticationContext Context(params (string Type, string Value)[] claims) => new()
    {
        Subject = new Subject { Issuer = Issuer, SubjectId = "oid-123", AuthenticatedAt = DateTimeOffset.UtcNow },
        Claims = ClaimSet.Empty.Add(claims.Select(c => new Claim { Type = c.Type, Value = c.Value, Source = ClaimSources.External })),
    };

    private static (ResolvePrincipalStepType Step, Mock<IPrincipalBinding> Bindings) Step()
    {
        var bindings = new Mock<IPrincipalBinding>();
        bindings.Setup(b => b.Resolve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Principal?>.Success(null));
        bindings.Setup(b => b.RedeemInvitation(Issuer, "oid-123", "vendor@partner.example", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Principal?>.Success(Invited));

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(new AuthenticationContextAccessor());
        builder.Services.AddSingleton(bindings.Object);
        builder.Services.AddSingleton(Mock.Of<IExternalIdentityProvisionerBindingConfigurationProvider>());
        builder.Services.AddSingleton(Mock.Of<IExternalIdentityProvisionerServiceProvider>());
        builder.Services.AddSingleton(Mock.Of<ITenantResolver>());
        var host = builder.Build();
        var step = new ResolvePrincipalStepType();
        step.Initialize(host, null);
        return (step, bindings);
    }
}
