using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Sql;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceCredentials.Sql.Services;

namespace FdwReferenceCredentials.Sql.Tests;

/// <summary>
/// FDW-941: a set-password link is redeemed by an anonymous caller, so redemption resolves the vault as
/// system and restores the caller afterwards; creating a link is an administrator's act and keeps theirs.
/// </summary>
public sealed class PasswordLinkServiceContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RedeemUsesSystemContextAndRestoresCaller(bool hasCaller)
    {
        var previous = hasCaller ? Mock.Of<IAuthenticationContext>() : null;
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var stores = new Mock<ICredentialStoreConfigurationProvider>();
        stores.Setup(x => x.Get("CredentialStore", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                return Task.FromResult(GenericResult<ICredentialStoreImplementationConfiguration>.Success(null!));
            });

        using var service = Service(stores.Object, accessor);
        (await service.Redeem("test-token", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();

        accessor.Current.ShouldBeSameAs(previous);
        stores.VerifyAll();
    }

    [Fact]
    public async Task CreatingALinkKeepsTheAdministratorsContext()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var stores = new Mock<ICredentialStoreConfigurationProvider>();
        stores.Setup(x => x.Get("CredentialStore", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                accessor.Current.ShouldBeSameAs(previous);
                return Task.FromResult(GenericResult<ICredentialStoreImplementationConfiguration>.Success(null!));
            });

        using var service = Service(stores.Object, accessor);
        var created = await service.CreateLink(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), TestContext.Current.CancellationToken);

        created.IsSuccess.ShouldBeFalse();
        accessor.Current.ShouldBeSameAs(previous);
        stores.VerifyAll();
    }

    [Fact]
    public async Task RedeemWithNoCredentialServiceNamedIsRefused()
    {
        var accessor = new AuthenticationContextAccessor();
        var stores = new Mock<ICredentialStoreConfigurationProvider>();
        stores.Setup(x => x.Get("CredentialStore", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialStoreImplementationConfiguration>.Success(null!));

        using var service = Service(stores.Object, accessor);

        // No credential service is named, so nothing resolves and the link is refused, not faked.
        (await service.Redeem("test-token", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    private static SqlPasswordLinkService Service(ICredentialStoreConfigurationProvider stores, IAuthenticationContextAccessor accessor)
    {
        var config = new CredentialServiceConfigurationProvider(
            NullLogger<CredentialServiceConfigurationProvider>.Instance,
            Mock.Of<IConfigurationGatewayProvider>(), "ConfigurationDb", "sec", "CredentialService");
        return new SqlPasswordLinkService(Mock.Of<IDataVaultProvider>(), stores, config, accessor);
    }
}
