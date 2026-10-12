using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for DataStoreTypeBase virtual methods, tested through MsSqlDataStoreType
/// as a concrete representative implementation.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataStoreTypeBaseTests
{
    // Why: MsSqlDataStoreType replaces the deleted FileDataStoreType as the concrete
    // representative for exercising DataStoreTypeBase virtual methods. Both are sealed
    // DataStoreTypeBase subclasses; the specific transport is irrelevant to these tests.
    private readonly MsSqlDataStoreType _sut = new();

    // --- Computed Properties ---


    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConfigurationTypeReturnsExpectedType()
    {
        _sut.ConfigurationType.ShouldNotBeNull();
    }

    // --- TypeOptionBase properties ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NameIsNotEmpty()
    {
        _sut.Name.ShouldNotBeNullOrEmpty();
    }
}

/// <summary>
/// Minimal concrete DataStoreTypeBase implementation used only to provide a second
/// distinct name in <see cref="DataStoreTypeBaseTests.IdIsDifferentForDifferentNames"/>.
/// Not registered in DataStoreTypes — test-local use only.
/// </summary>
// Why: DataStoreTypeBase now takes a single config type arg; DataStoreImplementationConfiguration is the
// simplest available config that does not require additional assembly references beyond what the
// test project already has.
file sealed class TestOnlyDataStoreType
    : DataStoreTypeBase<DataStoreImplementationConfiguration>
{
    public TestOnlyDataStoreType() : base(
        name: "TestOnly",
        displayName: "Test Only DataStore",
        description: "Test-only concrete DataStoreTypeBase for IdIsDifferentForDifferentNames")
    {
    }


    // Why: the never-called per-container Build was replaced by SupplyBuilder (the per-transport
    // IDataStoreBuilder). This test type is used only for IdIsDifferentForDifferentNames, so its
    // builder is never invoked — a fail-loud stub satisfies the abstract member.
    public override IDataStoreBuilder SupplyBuilder(ILogger? logger = null) => new TestOnlyDataStoreBuilder();
}

// Why: minimal IDataStoreBuilder stub for TestOnlyDataStoreType — never invoked by the tests that use it.
file sealed class TestOnlyDataStoreBuilder : IDataStoreBuilder
{
    public IGenericResult Configure(IGenericConfiguration storeConfig)
        => GenericResult.Failure(new GenericMessage("TestOnly builder has no Configure implementation"));

    public void DeclareByHost() => throw new System.NotSupportedException("TestOnly builder builds no store");

    public IGenericResult Add(Fdw.Data.Abstractions.IDataNodePath path)
        => GenericResult.Failure(new GenericMessage("TestOnly builder has no Add(path) implementation"));

    public IGenericResult Add(Fdw.Data.Abstractions.IDataContainer container)
        => GenericResult.Failure(new GenericMessage("TestOnly builder has no Add(container) implementation"));

    public System.Threading.Tasks.Task<IGenericResult<Fdw.Data.Abstractions.IDataStore>> Build(
        System.Threading.CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult(
            GenericResult<Fdw.Data.Abstractions.IDataStore>.Failure(new GenericMessage("TestOnly builder has no Build implementation")));
}
