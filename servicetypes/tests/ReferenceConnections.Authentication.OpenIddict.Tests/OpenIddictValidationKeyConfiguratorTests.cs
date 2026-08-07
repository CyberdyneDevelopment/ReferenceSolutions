using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Hosting;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenIddict.Validation;
using Shouldly;
using Xunit;
using ReferenceAuthentication.OpenIddict;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Unit tests for <see cref="OpenIddictValidationKeyConfigurator"/> — the validation-only
/// <c>IConfigureOptions&lt;OpenIddictValidationOptions&gt;</c> that replaces <c>UseLocalServer()</c>
/// for a host with no co-resident <c>AddServer()</c> (e.g. etl, scheduler). Covers the NO-FALLBACKS
/// fail-loud paths: no enabled OpenIddict configuration, and a relative/invalid <c>Authority</c>.
/// </summary>
public sealed class OpenIddictValidationKeyConfiguratorTests
{
    private static Mock<TokenManagerConfigurationProvider> BuildHeaderProviderMock()
        // Why: Castle DynamicProxy ignores optional ctor params — pass trailing invalidator explicitly
        // so the 5-param ctor arity matches. (object?)null! per repo convention.
        => new(
            NullLogger<TokenManagerConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "auth",
            (object?)null!);

    private static Mock<OpenIddictTokenManagerConfigurationProvider> BuildTypedProviderMock()
        => new(
            NullLogger<OpenIddictTokenManagerConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "auth",
            (object?)null!);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigureWithNoEnabledConfigurationThrows()
    {
        // Arrange — Why: no auth.TokenManager row with ServiceOptionType='OpenIddict' means
        // this validation-only host has no key/issuer to configure — fail loud, never register
        // unvalidated options.
        var headerProviderMock = BuildHeaderProviderMock();
        headerProviderMock
            .Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TokenManagerConfiguration>>.Success([]));

        var services = new ServiceCollection();
        services.AddSingleton(headerProviderMock.Object);
        var provider = services.BuildServiceProvider();

        var sut = new OpenIddictValidationKeyConfigurator(
            provider.GetRequiredService<IServiceScopeFactory>(), loggerFactory: null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.Configure(new OpenIddictValidationOptions()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigureWithRelativeAuthorityThrows()
    {
        // Arrange — Why: a relative/missing Authority cannot pin an absolute issuer — fail loud rather
        // than validate against no issuer at all.
        var headerId = Guid.NewGuid();
        var header = new TokenManagerConfiguration
        {
            Id = headerId,
            Name = "OpenIddict",
            ServiceOptionType = "OpenIddict",
            SecretManagerName = "EnvSecrets",
            SecretKeyName = "oidc-signing-key",
        };

        var headerProviderMock = BuildHeaderProviderMock();
        headerProviderMock
            .Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TokenManagerConfiguration>>.Success([header]));

        var typedProviderMock = BuildTypedProviderMock();
        typedProviderMock
            .Setup(x => x.Get(headerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OpenIddictTokenManagerConfiguration>.Success(
                new OpenIddictTokenManagerConfiguration
                {
                    TokenManagerId = headerId,
                    Authority = "not-an-absolute-uri",
                    TokenEndpoint = "/connect/token",
                }));

        var services = new ServiceCollection();
        services.AddSingleton(headerProviderMock.Object);
        services.AddSingleton(typedProviderMock.Object);
        var provider = services.BuildServiceProvider();

        var sut = new OpenIddictValidationKeyConfigurator(
            provider.GetRequiredService<IServiceScopeFactory>(), loggerFactory: null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.Configure(new OpenIddictValidationOptions()));
    }
}
