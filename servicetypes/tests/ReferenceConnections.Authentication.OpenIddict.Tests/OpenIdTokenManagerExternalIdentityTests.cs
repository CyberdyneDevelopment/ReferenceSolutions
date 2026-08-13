using Fdw.Services.Authentication.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using ReferenceAuthentication.OpenIddict.Storage;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions.Tokens;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using ReferenceAuthentication.OpenIddict;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Unit tests for the external-identity provisioning mechanism consulted by
/// <c>OpenIdTokenManager.IssueForExternalIdentity</c> at a <c>FindUserId</c> miss, BEFORE returning
/// <c>ExternalIdentityNotFound</c>. Covers: binding absent → unchanged (fail-loud) behavior; binding
/// present + resolved provisioner succeeds → user provisioned and issuance continues; provisioner
/// resolution failure → propagated; lookup hit → binding/provisioner never consulted at all (existing
/// behavior untouched).
/// </summary>
public sealed class OpenIdTokenManagerExternalIdentityTests
{
    private const string ExternalProvider = "test-idp";
    private const string ExternalSubject = "ext-subject-1";
    private const string ProvisionerName = "test-provisioner";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static (TokenManagerConfiguration Header, OpenIddictTokenManagerConfiguration Typed) BuildConfig()
    {
        var headerId = Guid.NewGuid();
        var header = new TokenManagerConfiguration
        {
            Id = headerId,
            Name = "OpenIddict",
            ServiceOptionType = "OpenIddict",
        };
        var typed = new OpenIddictTokenManagerConfiguration
        {
            TokenManagerId = headerId,
            Authority = "https://issuer.example.test",
        };
        header.Configuration = typed;
        return (header, typed);
    }

    // Why: ExternalIdentityService is sealed and DataGateway-backed — construct the real store over a
    // mocked IDataGateway rather than mocking the store itself (Moq cannot proxy a sealed class).
    private static ExternalIdentityService BuildExternalIdentityService(Guid? existingUserId)
    {
        var gatewayMock = new Mock<IDataGateway>(MockBehavior.Strict);
        var records = existingUserId.HasValue
            ? new[]
            {
                new ExternalIdentityRecord
                {
                    Provider = ExternalProvider,
                    ExternalSubject = ExternalSubject,
                    UserId = existingUserId.Value,
                    IsActive = true,
                },
            }
            : Array.Empty<ExternalIdentityRecord>();

        gatewayMock
            .Setup(g => g.Execute<IEnumerable<ExternalIdentityRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExternalIdentityRecord>>.Success(records));

        return new ExternalIdentityService(
            new Lazy<IDataGateway>(() => gatewayMock.Object), new AuthenticationContextAccessor(), NullLogger<ExternalIdentityService>.Instance);
    }

    // Why: ExternalIdentityProvisionerBindingConfigurationProvider's own read methods are not virtual
    // (mirrors DefaultConfigurationProvider's non-mockable shape) — construct the real provider over a
    // mocked IConfigurationGateway rather than mocking the provider class itself, the same pattern
    // BuildExternalIdentityService already uses for ExternalIdentityService.
    private static ExternalIdentityProvisionerBindingConfigurationProvider BuildBindingProvider(
        params ExternalIdentityProvisionerBindingConfiguration[] rows)
    {
        var gatewayMock = new Mock<IConfigurationGateway>(MockBehavior.Strict);
        gatewayMock
            .Setup(g => g.Execute<IEnumerable<ExternalIdentityProvisionerBindingConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExternalIdentityProvisionerBindingConfiguration>>.Success(rows));

        return new ExternalIdentityProvisionerBindingConfigurationProvider(
            NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gatewayMock.Object));
    }

    // Why: every OTHER dependency is irrelevant to the external_identity path — construct/mock them
    // Strict so an accidental call anywhere else in the grant would fail the test loudly, keeping this
    // suite focused on the provisioning mechanism alone.
    private static OpenIdTokenManager BuildManager(
        Guid? existingUserId,
        ExternalIdentityProvisionerBindingConfigurationProvider bindingProvider,
        IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration> provisionerProvider)
    {
        var (header, typed) = BuildConfig();

        var userProvider = new UserConfigurationProvider(
            NullLogger<UserConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!));

        var credentialServiceMock = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var permissionResolverMock = new Mock<IEffectivePermissionResolver>(MockBehavior.Strict);
        var secretManagerProviderMock = new Mock<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>(MockBehavior.Strict);

        var revokedTokenStore = new RevokedAccessTokenStore(
            new Lazy<IDataGateway>(() => null!), new AuthenticationContextAccessor(), NullLogger<RevokedAccessTokenStore>.Instance);
        var authorizationStore = new OpenIddictAuthorizationStore(
            new Lazy<IDataGateway>(() => null!), new AuthenticationContextAccessor(), NullLogger<OpenIddictAuthorizationStore>.Instance);

        return new OpenIdTokenManager(
            header,
            typed,
            userProvider,
            credentialServiceMock.Object,
            BuildExternalIdentityService(existingUserId),
            permissionResolverMock.Object,
            secretManagerProviderMock.Object,
            revokedTokenStore,
            authorizationStore,
            NullLogger<OpenIdTokenManager>.Instance,
            bindingProvider,
            provisionerProvider);
    }

    private static ClaimsPrincipal BuildExternalPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim("iss", ExternalProvider),
            new Claim("sub", ExternalSubject),
        }));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task IssueForExternalIdentityFailsLoudWhenBindingAbsentAndLookupMisses()
    {
        // Arrange — Why: no binding row for (null tenant, ExternalProvider) is the DEFAULT; a lookup
        // miss must behave exactly as it did before the provisioning mechanism existed (ExternalIdentityNotFound).
        var bindingProvider = BuildBindingProvider(); // no rows at all
        var provisionerProviderMock = new Mock<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);

        var sut = BuildManager(existingUserId: null, bindingProvider, provisionerProviderMock.Object);
        var request = new TokenIssuanceRequest { GrantType = "external_identity", ExternalPrincipal = BuildExternalPrincipal() };

        // Act
        var result = await sut.Issue(request, Ct);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        provisionerProviderMock.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task IssueForExternalIdentityProvisionsUserWhenBindingResolvesAndProvisionerSucceeds()
    {
        // Arrange — Why: a binding row that resolves to a registered provisioner, whose Provision call
        // succeeds on a lookup miss, must let issuance continue with the newly-provisioned user.
        var newUserId = Guid.NewGuid();
        var bindingProvider = BuildBindingProvider(new ExternalIdentityProvisionerBindingConfiguration
        {
            TenantId = null,
            ProviderName = ExternalProvider,
            ProvisionerName = ProvisionerName,
        });

        var provisionerMock = new Mock<IExternalIdentityProvisioner>(MockBehavior.Strict);
        provisionerMock
            .Setup(p => p.Provision(ExternalProvider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(newUserId));

        var provisionerProviderMock = new Mock<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        provisionerProviderMock
            .Setup(p => p.Get(ProvisionerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(provisionerMock.Object));

        var sut = BuildManager(existingUserId: null, bindingProvider, provisionerProviderMock.Object);
        var request = new TokenIssuanceRequest { GrantType = "external_identity", ExternalPrincipal = BuildExternalPrincipal() };

        // Act
        var result = await sut.Issue(request, Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindFirstValue(ClaimDefinitions.sub.Name).ShouldBe(newUserId.ToString());
        provisionerMock.Verify(
            p => p.Provision(ExternalProvider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task IssueForExternalIdentityPropagatesProvisionerResolutionFailure()
    {
        // Arrange — Why: the binding resolved to a provisionerName, but that name does not resolve to a
        // registered IExternalIdentityProvisioner — a hard error, never masked as "not found".
        var bindingProvider = BuildBindingProvider(new ExternalIdentityProvisionerBindingConfiguration
        {
            TenantId = null,
            ProviderName = ExternalProvider,
            ProvisionerName = ProvisionerName,
        });

        var provisionerProviderMock = new Mock<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        provisionerProviderMock
            .Setup(p => p.Get(ProvisionerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Failure(
                new Fdw.Messages.GenericMessage($"provisioner '{ProvisionerName}' is not registered.")));

        var sut = BuildManager(existingUserId: null, bindingProvider, provisionerProviderMock.Object);
        var request = new TokenIssuanceRequest { GrantType = "external_identity", ExternalPrincipal = BuildExternalPrincipal() };

        // Act
        var result = await sut.Issue(request, Ct);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task IssueForExternalIdentitySucceedsWhenLookupHitsExistingUserWithoutConsultingBindingOrProvisioner()
    {
        // Arrange — Why: an existing auth.ExternalIdentity link is the common case; neither the binding
        // provider nor the provisioner provider must ever be consulted when the lookup already resolves a
        // user (Strict mocks with no setups throw if either is touched).
        var existingUserId = Guid.NewGuid();
        var bindingGatewayMock = new Mock<IConfigurationGateway>(MockBehavior.Strict); // no Execute setup — must never be called
        var bindingProvider = new ExternalIdentityProvisionerBindingConfigurationProvider(
            NullLogger<ExternalIdentityProvisionerBindingConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => bindingGatewayMock.Object));
        var provisionerProviderMock = new Mock<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);

        var sut = BuildManager(existingUserId, bindingProvider, provisionerProviderMock.Object);
        var request = new TokenIssuanceRequest { GrantType = "external_identity", ExternalPrincipal = BuildExternalPrincipal() };

        // Act
        var result = await sut.Issue(request, Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindFirstValue(ClaimDefinitions.sub.Name).ShouldBe(existingUserId.ToString());
        provisionerProviderMock.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
