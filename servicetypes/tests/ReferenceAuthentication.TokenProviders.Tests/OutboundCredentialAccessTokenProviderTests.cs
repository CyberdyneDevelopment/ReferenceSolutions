using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Tokens.Outbound;
using Fdw.Web.Http.Authentication;
using Moq;
using ReferenceAuthentication.TokenProviders;
using Shouldly;
using Xunit;

namespace ReferenceAuthentication.TokenProviders.Tests;

/// <summary>
/// Covers <see cref="OutboundCredentialAccessTokenProvider"/> — the client-credentials path used
/// when a host calls another service under its own machine identity rather than a caller's.
/// </summary>
public class OutboundCredentialAccessTokenProviderTests
{
    private static Mock<IOutboundCredentialService> ServiceReturning(IGenericResult<OutboundCredential> result)
    {
        var mock = new Mock<IOutboundCredentialService>();
        mock.Setup(s => s.Acquire(It.IsAny<OutboundCredentialRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    private static OutboundCredentialAccessTokenProvider Provider(Mock<IOutboundCredentialService> service)
        => new(service.Object, "fdw.scheduler", "s3cret", logger: null);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsTheAcquiredAccessToken()
    {
        var service = ServiceReturning(GenericResult<OutboundCredential>.Success(
            new OutboundCredential { AccessToken = "issued.token" }));

        var token = await Provider(service).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("issued.token");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task PassesTheConfiguredClientIdentityToTheCredentialService()
    {
        var service = ServiceReturning(GenericResult<OutboundCredential>.Success(
            new OutboundCredential { AccessToken = "t" }));

        await Provider(service).GetAccessToken(TestContext.Current.CancellationToken);

        service.Verify(s => s.Acquire(
            It.Is<OutboundCredentialRequest>(r => r.ClientId == "fdw.scheduler" && r.ClientSecret == "s3cret"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsNullWhenAcquisitionFails()
    {
        // Why null rather than throwing: BearerTokenHandler then sends no Authorization header and
        // the downstream answers 401, which is the honest outcome. Throwing here would surface a
        // credential problem as an unhandled fault at an unrelated call site.
        var service = ServiceReturning(GenericResult<OutboundCredential>.Failure(Mock.Of<IGenericMessage>()));

        var token = await Provider(service).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsNullWhenAcquisitionSucceedsWithNoResponse()
    {
        var service = ServiceReturning(GenericResult<OutboundCredential>.Success(null!));

        var token = await Provider(service).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void RejectsAMissingCredentialService()
        => Should.Throw<ArgumentNullException>(() =>
            new OutboundCredentialAccessTokenProvider(null!, "id", "secret", logger: null));

    [Theory]
    [InlineData("", "secret")]
    [InlineData("id", "")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void RejectsAnEmptyClientIdentity(string clientId, string clientSecret)
    {
        // Why fail at construction: an empty credential would acquire nothing and surface as a 401
        // from an unrelated downstream call. The configuration error belongs at composition time.
        Should.Throw<ArgumentException>(() => new OutboundCredentialAccessTokenProvider(
            new Mock<IOutboundCredentialService>().Object, clientId, clientSecret, logger: null));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public void IsAnAccessTokenProvider()
        => Provider(ServiceReturning(GenericResult<OutboundCredential>.Success(
               new OutboundCredential { AccessToken = "t" })))
           .ShouldBeAssignableTo<IAccessTokenProvider>();
}
