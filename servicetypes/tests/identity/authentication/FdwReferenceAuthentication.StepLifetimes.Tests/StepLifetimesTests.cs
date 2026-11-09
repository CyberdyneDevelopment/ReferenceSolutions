using System.Reflection;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Users.Abstractions;
using FdwReferenceAuthentication.AuthorizeIssuance.Registration;
using FdwReferenceAuthentication.ForeignToken;
using FdwReferenceAuthentication.ForeignToken.Registration;
using FdwReferenceAuthentication.OidcRedirect;
using FdwReferenceAuthentication.OidcRedirect.Registration;
using FdwReferenceAuthentication.PasswordCredential.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceAuthentication.StepLifetimes.Tests;

/// <summary>
/// Tests that an authentication step takes only what a process-wide instance can keep, and that what really lives for one request reaches it through the runner (FDW-4640): a
/// host that validates scopes initializes the step, a scoped service is refused at Initialize, and two logins in two scopes each get their own services.
/// </summary>
[Trait("Category", "Security")]
public sealed class StepLifetimesTests
{
    private static HostApplicationBuilder Builder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(new AuthenticationContextAccessor());
        return builder;
    }

    private static bool Initializes(IHost host, Func<IHost, object> initialize)
    {
        try
        {
            return initialize(host) is IGenericResult { IsSuccess: true };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    // ── AuthorizeIssuance ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AuthorizeIssuanceInitializesWhenItsEligibilityIsASingleton()
    {
        var builder = Builder();
        builder.Services.AddSingleton(Mock.Of<IIssuanceEligibility>());
        using var host = builder.Build();

        Initializes(host, h => new AuthorizeIssuanceStepType().Initialize(h, null)).ShouldBeTrue();
    }

    [Fact]
    public void AuthorizeIssuanceDoesNotInitializeAgainstAScopedEligibility()
    {
        var builder = Builder();
        builder.Services.AddScoped(_ => Mock.Of<IIssuanceEligibility>());
        using var host = builder.Build();

        Initializes(host, h => new AuthorizeIssuanceStepType().Initialize(h, null)).ShouldBeFalse();
    }

    [Fact]
    public async Task TwoLoginsInTwoScopesAreDecidedForTheirOwnPrincipalAndTenants()
    {
        var eligibility = new Mock<IIssuanceEligibility>();
        var asked = new List<(Guid Principal, ITenantProvider Tenants)>();
        eligibility.Setup(e => e.MayBeIssued(It.IsAny<Principal>(), It.IsAny<ITenantProvider>(), It.IsAny<CancellationToken>()))
            .Callback<Principal, ITenantProvider, CancellationToken>((principal, tenants, _) => asked.Add((principal.Id, tenants)))
            .ReturnsAsync(GenericResult<Decision>.Success(new Decision { Permitted = true, Reason = "eligible" }));
        var builder = Builder();
        builder.Services.AddSingleton(eligibility.Object);
        using var host = builder.Build();
        var step = new AuthorizeIssuanceStepType();
        step.Initialize(host, null);
        var first = (Principal: new Principal { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() }, Tenants: Mock.Of<ITenantProvider>());
        var second = (Principal: new Principal { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() }, Tenants: Mock.Of<ITenantProvider>());

        await step.Execute(new AuthenticationContext { Principal = first.Principal }, first.Tenants, TestContext.Current.CancellationToken);
        await step.Execute(new AuthenticationContext { Principal = second.Principal }, second.Tenants, TestContext.Current.CancellationToken);

        asked.Select(a => a.Principal).ShouldBe([first.Principal.Id, second.Principal.Id]);
        asked[0].Tenants.ShouldBeSameAs(first.Tenants);
        asked[1].Tenants.ShouldBeSameAs(second.Tenants);
    }

    // ── ForeignToken ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForeignTokenInitializesWhenWhatItCapturesAreSingletons()
    {
        var builder = Builder();
        builder.Services.AddSingleton(new ForeignAuthorityReader(Mock.Of<IForeignAuthorityConfigurationProvider>()));
        builder.Services.AddSingleton(Mock.Of<ISigningKeyProvider>());
        builder.Services.AddSingleton(Mock.Of<IForeignTokenAccessor>());
        using var host = builder.Build();

        Initializes(host, h => new ForeignTokenStepType().Initialize(h, null)).ShouldBeTrue();
    }

    [Fact]
    public void ForeignTokenDoesNotInitializeAgainstAScopedAccessor()
    {
        var builder = Builder();
        builder.Services.AddSingleton(new ForeignAuthorityReader(Mock.Of<IForeignAuthorityConfigurationProvider>()));
        builder.Services.AddSingleton(Mock.Of<ISigningKeyProvider>());
        builder.Services.AddScoped(_ => Mock.Of<IForeignTokenAccessor>());
        using var host = builder.Build();

        Initializes(host, h => new ForeignTokenStepType().Initialize(h, null)).ShouldBeFalse();
    }

    // ── OidcRedirect ─────────────────────────────────────────────────────────────────────────────

    private static void OidcSingletons(IServiceCollection services, bool callbackIsScoped)
    {
        services.AddSingleton(new OidcAuthorityReader(Mock.Of<IOidcAuthorityConfigurationProvider>()));
        services.AddSingleton(Mock.Of<ISigningKeyProvider>());
        services.AddSingleton(Mock.Of<IAuthorizationRequestStore>());
        services.AddSingleton(Mock.Of<ISecretManagerProvider>());
        services.AddHttpClient();
        if (callbackIsScoped)
            services.AddScoped(_ => Mock.Of<IOidcCallbackAccessor>());
        else
            services.AddSingleton(Mock.Of<IOidcCallbackAccessor>());
    }

    [Fact]
    public void OidcRedirectInitializesWhenWhatItCapturesAreSingletons()
    {
        var builder = Builder();
        OidcSingletons(builder.Services, callbackIsScoped: false);
        using var host = builder.Build();

        Initializes(host, h => new OidcRedirectStepType().Initialize(h, null)).ShouldBeTrue();
    }

    [Fact]
    public void OidcRedirectDoesNotInitializeAgainstAScopedCallbackAccessor()
    {
        var builder = Builder();
        OidcSingletons(builder.Services, callbackIsScoped: true);
        using var host = builder.Build();

        Initializes(host, h => new OidcRedirectStepType().Initialize(h, null)).ShouldBeFalse();
    }

    // ── Asserted methods (FDW-4697) ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(ForeignTokenStepType))]
    [InlineData(typeof(OidcRedirectStepType))]
    public void AStepWhoseMethodsComeFromItsAuthorityDeclaresNoneOfItsOwn(Type step)
    {
        var instance = (IAuthenticationStep)Activator.CreateInstance(step)!;

        instance.AuthenticationMethods.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(typeof(ForeignTokenStepType))]
    [InlineData(typeof(OidcRedirectStepType))]
    public void AStepSharedByEveryLoginKeepsNoListOfMethodsInAField(Type step)
    {
        var fields = step.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        fields.Where(f => typeof(IEnumerable<string>).IsAssignableFrom(f.FieldType) && f.FieldType != typeof(string)).ShouldBeEmpty();
    }

    private static ForeignTokenStepType InitializedForeignToken(IGenericResult<IReadOnlyList<IForeignAuthorityImplementationConfiguration>> rows)
    {
        var provider = new Mock<IForeignAuthorityConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        var builder = Builder();
        builder.Services.AddSingleton(new ForeignAuthorityReader(provider.Object));
        builder.Services.AddSingleton(Mock.Of<ISigningKeyProvider>());
        builder.Services.AddSingleton(Mock.Of<IForeignTokenAccessor>());
        var host = builder.Build();
        var step = new ForeignTokenStepType();
        step.Initialize(host, null);
        return step;
    }

    private static IForeignAuthorityImplementationConfiguration Authority(string assertableMethods)
    {
        var row = new Mock<IForeignAuthorityImplementationConfiguration>();
        row.SetupGet(r => r.Name).Returns("corp");
        row.SetupGet(r => r.IsCurrent).Returns(true);
        row.SetupGet(r => r.IsDeleted).Returns(false);
        row.SetupGet(r => r.Issuer).Returns("https://login.example");
        row.SetupGet(r => r.JwksUri).Returns("https://login.example/keys");
        row.SetupGet(r => r.ValidAudiences).Returns("api");
        row.SetupGet(r => r.ValidAlgorithms).Returns("RS256");
        row.SetupGet(r => r.AssertableMethods).Returns(assertableMethods);
        return row.Object;
    }

    [Fact]
    public async Task TheForeignTokenStepStatesTheMethodsItsAuthorityPermitsWhenAsked()
    {
        var step = InitializedForeignToken(GenericResult<IReadOnlyList<IForeignAuthorityImplementationConfiguration>>.Success([Authority("pwd,mfa")]));

        var methods = await step.ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsSuccess.ShouldBeTrue();
        methods.Value.ShouldBe(["pwd", "mfa"]);
    }

    [Fact]
    public async Task TheForeignTokenStepRefusesWithTheReadersReasonWhenItsAuthorityCannotBeRead()
    {
        var step = InitializedForeignToken(GenericResult<IReadOnlyList<IForeignAuthorityImplementationConfiguration>>.Success([]));

        var methods = await step.ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsFailure.ShouldBeTrue();
        methods.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AForeignTokenStepNobodyInitializedHasNoAuthorityToRead()
    {
        var methods = await new ForeignTokenStepType().ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsFailure.ShouldBeTrue();
    }

    private static OidcAuthorityConfiguration OidcRow(string assertableMethods) => new()
    {
        Name = "Entra",
        Issuer = "https://login.example/tenant/v2.0",
        AuthorizationEndpoint = "https://login.example/tenant/authorize",
        TokenEndpoint = "https://login.example/tenant/token",
        JwksUri = "https://login.example/tenant/keys",
        ClientId = "client-id",
        RedirectUri = "https://app.example/callback",
        Scopes = "openid,profile",
        SubjectClaim = "oid",
        ValidAudiences = "client-id",
        ValidAlgorithms = "RS256",
        AssertableMethods = assertableMethods,
        ClockSkewSeconds = 30,
        IsCurrent = true,
        ClientAuthenticationType = "ClientSecret",
        AdditionalProperties = new Dictionary<string, string?>
        {
            ["SecretManagerName"] = "MsSqlSecrets",
            ["SecretKeyName"] = "authentik-client",
        },
    };

    private static OidcRedirectStepType InitializedOidc(IGenericResult<IReadOnlyList<IOidcAuthorityImplementationConfiguration>> rows)
    {
        var provider = new Mock<IOidcAuthorityConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        var builder = Builder();
        OidcSingletons(builder.Services, callbackIsScoped: false);
        builder.Services.AddSingleton(new OidcAuthorityReader(provider.Object));
        var host = builder.Build();
        var step = new OidcRedirectStepType();
        step.Initialize(host, null);
        return step;
    }

    private static IGenericResult<IReadOnlyList<IOidcAuthorityImplementationConfiguration>> OidcRows(params IOidcAuthorityImplementationConfiguration[] rows) =>
        GenericResult<IReadOnlyList<IOidcAuthorityImplementationConfiguration>>.Success(rows);

    [Fact]
    public async Task TheOidcStepStatesTheMethodsItsAuthorityPermitsWhenAsked()
    {
        var step = InitializedOidc(OidcRows(OidcRow("pwd,mfa")));

        var methods = await step.ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsSuccess.ShouldBeTrue();
        methods.Value.ShouldBe(["pwd", "mfa"]);
    }

    [Fact]
    public async Task TheOidcStepRefusesWithTheReadersReasonWhenItsAuthorityCannotBeRead()
    {
        var step = InitializedOidc(OidcRows());

        var methods = await step.ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsFailure.ShouldBeTrue();
        methods.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnOidcStepNobodyInitializedHasNoAuthorityToRead()
    {
        var methods = await new OidcRedirectStepType().ProvableMethods(TestContext.Current.CancellationToken);

        methods.IsFailure.ShouldBeTrue();
    }

    private sealed class OneFlow(AuthenticationFlow flow) : IAuthenticationFlowProvider
    {
        public Task<IGenericResult<AuthenticationFlow>> Get(string flowName, CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericResult<AuthenticationFlow>.Success(flow));

        public Task<IGenericResult<IReadOnlyList<AuthenticationFlow>>> Get(CancellationToken cancellationToken = default) =>
            Task.FromResult(GenericResult<IReadOnlyList<AuthenticationFlow>>.Success([flow]));

        public Task<IGenericResult> LoadAndValidate(CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private static SignInMethodAdmission AdmissionThrough(OidcRedirectStepType step) => new(
        new OneFlow(new AuthenticationFlow
        {
            Name = "oidc-flow",
            Steps = [new AuthenticationFlowStep { Name = "oidc", Configuration = null }],
            RequiredPermissions = [],
            Audience = "test-audience",
            ExecutionLifetime = TimeSpan.FromMinutes(5),
        }),
        Mock.Of<ITenantResolver>(), Mock.Of<IUserPermissions>(), new AuthenticationContextAccessor(), name => name == "oidc" ? step : null);

    [Fact]
    public async Task AdmissionAdmitsAMethodTheOidcAuthorityPermits()
    {
        var admission = AdmissionThrough(InitializedOidc(OidcRows(OidcRow("pwd,mfa"))));

        var admitted = await admission.MayUse(Guid.NewGuid(), "mfa", TestContext.Current.CancellationToken);

        admitted.IsSuccess.ShouldBeTrue();
        admitted.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task AdmissionRefusesWithTheReadersFailureWhenTheOidcAuthorityCannotBeRead()
    {
        var admission = AdmissionThrough(InitializedOidc(OidcRows()));

        var admitted = await admission.MayUse(Guid.NewGuid(), "pwd", TestContext.Current.CancellationToken);

        admitted.IsFailure.ShouldBeTrue();
        admitted.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    // ── PasswordCredential ───────────────────────────────────────────────────────────────────────

    private static IUserCredentialService? Held(IAuthenticationStep step)
    {
        var request = typeof(PasswordCredentialStepType).GetField("_request", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(step);
        return (request as PasswordCredentialRequestServices)?.Credentials;
    }

    [Fact]
    public void TwoLoginsInTwoScopesVerifyAgainstTheirOwnCredentialService()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IUserCredentialService>());
        services.AddScoped(sp => new PasswordCredentialRequestServices(() => sp.GetRequiredService<IUserCredentialService>()));
        using var container = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var first = container.CreateScope();
        using var second = container.CreateScope();
        var step = new PasswordCredentialStepType();

        var forFirst = step.BindToRequest(first.ServiceProvider.GetRequiredService<PasswordCredentialRequestServices>());
        var forSecond = step.BindToRequest(second.ServiceProvider.GetRequiredService<PasswordCredentialRequestServices>());

        Held(forFirst).ShouldBeSameAs(first.ServiceProvider.GetRequiredService<IUserCredentialService>());
        Held(forSecond).ShouldBeSameAs(second.ServiceProvider.GetRequiredService<IUserCredentialService>());
        Held(forFirst).ShouldNotBeSameAs(Held(forSecond));
    }

    [Fact]
    public void ThePasswordStepSharedByEveryLoginHoldsNoRequestServices()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IUserCredentialService>());
        services.AddScoped(sp => new PasswordCredentialRequestServices(() => sp.GetRequiredService<IUserCredentialService>()));
        using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        var step = new PasswordCredentialStepType();

        step.BindToRequest(scope.ServiceProvider.GetRequiredService<PasswordCredentialRequestServices>());

        Held(step).ShouldBeNull();
    }

    [Fact]
    public async Task APasswordStepNobodyBoundToARequestRefusesInsteadOfVerifying()
    {
        var step = new PasswordCredentialStepType();

        var outcome = await step.Execute(new AuthenticationContext(), Mock.Of<ITenantProvider>(), TestContext.Current.CancellationToken);

        outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ThePasswordStepAsksForItsOwnCarrier() =>
        new PasswordCredentialStepType().RequestServicesType.ShouldBe(typeof(PasswordCredentialRequestServices));
}
