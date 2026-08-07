using Fdw.Services.Connections.Http;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;

namespace ReferenceConnections.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpDataStoreType"/> — the per-transport DataStoreType that lets an HTTP-backed
/// DataStore build (without it, <c>DataStoreProvider</c> drops every Http store at tree build and HTTP
/// source reads fail with "No DataStoreType 'Http' found").
/// </summary>
public class HttpDataStoreTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NameIsHttp()
    {
        new HttpDataStoreType().Name.ShouldBe("Http");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigurationTypeIsBodylessHeader()
    {
        // Why: an Http DataStore has no typed body table — it uses the base DataStoreConfiguration header.
        new HttpDataStoreType().ConfigurationType.ShouldBe(typeof(DataStoreConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SupplyBuilderReturnsAGenericBuilder()
    {
        IDataStoreBuilder builder = new HttpDataStoreType().SupplyBuilder();

        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<Fdw.Services.Data.Builders.GenericDataStoreBuilder>();
    }

    // Why: registration into DataStoreTypes is performed by the entry-point app's
    // Registration.SourceGenerators module initializer (RegisterMember), which this test assembly does
    // not reference — so ByName("Http") is asserted at the integration/deploy level, not here.
}
