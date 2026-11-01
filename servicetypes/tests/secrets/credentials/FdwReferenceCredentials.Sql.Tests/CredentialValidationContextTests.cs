using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Sql;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceCredentials.Sql.Services;

namespace FdwReferenceCredentials.Sql.Tests;

public sealed class CredentialValidationContextTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ValidationUsesSystemContextAndRestoresCaller(bool personalToken, bool hasCaller)
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

        await Invoke(personalToken, true, stores.Object, accessor);

        accessor.Current.ShouldBeSameAs(previous);
        stores.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledValidationRestoresCaller(bool personalToken)
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var stores = new Mock<ICredentialStoreConfigurationProvider>();
        stores.Setup(x => x.Get("CredentialStore", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                throw new OperationCanceledException();
            });

        await Should.ThrowAsync<OperationCanceledException>(() => Invoke(personalToken, true, stores.Object, accessor));

        accessor.Current.ShouldBeSameAs(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListingRetainsCallerContext(bool personalToken)
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

        await Invoke(personalToken, false, stores.Object, accessor);

        accessor.Current.ShouldBeSameAs(previous);
        stores.VerifyAll();
    }

    private static async Task Invoke(bool personalToken, bool validate,
        ICredentialStoreConfigurationProvider stores, IAuthenticationContextAccessor accessor)
    {
        var config = new CredentialServiceConfigurationProvider(
            NullLogger<CredentialServiceConfigurationProvider>.Instance,
            Mock.Of<IConfigurationGatewayProvider>(), "ConfigurationDb", "sec", "CredentialService");
        var vaults = Mock.Of<IDataVaultProvider>();
        var generator = Mock.Of<IPersonalAccessTokenGenerator>();
        var cancellationToken = TestContext.Current.CancellationToken;
        if (personalToken)
        {
            using var service = new SqlPersonalAccessTokenService(vaults, generator, stores, config, accessor);
            if (validate)
                (await service.ValidateToken("test-token", cancellationToken)).IsSuccess.ShouldBeFalse();
            else
                (await service.ListTokens(Guid.NewGuid(), cancellationToken)).IsSuccess.ShouldBeFalse();
        }
        else
        {
            using var service = new SqlAgentKeyService(vaults, generator, stores, config, accessor);
            if (validate)
                (await service.ValidateKey("test-key", cancellationToken)).IsSuccess.ShouldBeFalse();
            else
                (await service.ListKeys(Guid.NewGuid(), cancellationToken)).IsSuccess.ShouldBeFalse();
        }
    }
}
