using System.Net;
using FdwReferenceHosts.ForwardedHeaders.Configuration;
using FdwReferenceHosts.ForwardedHeaders.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace FdwReferenceHosts.ForwardedHeaders.Registration.Tests;

/// <summary>
/// FDW-2729: the ForwardedHeaders row states which header names the person's address, and the host applies exactly that one. Behind Cloudflare the
/// person's address is in CF-Connecting-IP; the last hop, which the default reads, is a proxy of this host.
/// </summary>
public sealed class ForwardedHeadersHostTypeTests
{
    private static ForwardedHeadersHostConfiguration Row(string? header, params string[] networks) => new()
    {
        Name = "ForwardedHeaders",
        ForwardedForHeaderName = header!,
        TrustedProxyNetworks = [.. networks.Select(n => new ForwardedHeadersTrustedProxyNetwork { Network = n })],
    };

    private static async Task<IPAddress?> AddressAfterTheMiddleware(ForwardedHeadersOptions options, IPAddress hop, params (string Name, string Value)[] headers)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = hop;
        foreach (var (name, value) in headers)
            context.Request.Headers[name] = value;

        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        await middleware.Invoke(context);
        return context.Connection.RemoteIpAddress;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheRowNamesTheHeaderThatCarriesThePersonsAddress()
        => ForwardedHeadersHostType.OptionsFor(Row("CF-Connecting-IP")).ForwardedForHeaderName.ShouldBe("CF-Connecting-IP");

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ARowThatDoesNotStateTheHeaderIsRefusedAndNotGivenOne(string? header)
    {
        var refused = Should.Throw<InvalidOperationException>(() => ForwardedHeadersHostType.OptionsFor(Row(header)));

        refused.Message.ShouldContain("ForwardedForHeaderName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void NoDeclaredNetworkTrustsAnyUpstream()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("X-Forwarded-For"));

        options.KnownIPNetworks.ShouldBeEmpty();
        options.KnownProxies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ADeclaredNetworkIsTheOnlyOneTrusted()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("X-Forwarded-For", "10.0.0.0/8"));

        options.KnownIPNetworks.ShouldHaveSingleItem().ToString().ShouldBe("10.0.0.0/8");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task BehindCloudflareThePersonsAddressIsTheOneCloudflareSet()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("CF-Connecting-IP"));

        // The hop in front of the app is the gateway. Cloudflare set the person's address in CF-Connecting-IP; the proxies after it
        // put their own hops in X-Forwarded-For, the last of which is the gateway.
        var address = await AddressAfterTheMiddleware(
            options, IPAddress.Parse("127.0.0.1"),
            ("CF-Connecting-IP", "203.0.113.9"), ("X-Forwarded-For", "203.0.113.9, 172.69.1.1, 10.0.0.1"));

        address.ShouldBe(IPAddress.Parse("203.0.113.9"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheDefaultHeaderReadsOnlyTheLastHopWhichIsAProxy()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("X-Forwarded-For"));

        var address = await AddressAfterTheMiddleware(
            options, IPAddress.Parse("127.0.0.1"),
            ("X-Forwarded-For", "203.0.113.9, 172.69.1.1, 10.0.0.1"));

        address.ShouldBe(IPAddress.Parse("10.0.0.1"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ARequestWithoutTheNamedHeaderKeepsTheAddressOfItsHop()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("CF-Connecting-IP"));

        var address = await AddressAfterTheMiddleware(options, IPAddress.Parse("10.0.0.100"));

        address.ShouldBe(IPAddress.Parse("10.0.0.100"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AHeaderOtherThanTheNamedOneIsNotTaken()
    {
        var options = ForwardedHeadersHostType.OptionsFor(Row("CF-Connecting-IP"));

        var address = await AddressAfterTheMiddleware(options, IPAddress.Parse("10.0.0.100"), ("X-Forwarded-For", "198.51.100.7"));

        address.ShouldBe(IPAddress.Parse("10.0.0.100"));
    }
}
