using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for <see cref="DataStoreTypesBuilderSelector"/> — the concrete, connection-aware
/// <see cref="IDataStoreBuilderSelector"/> that dispatches to a transport's registered
/// <see cref="DataStoreTypes"/> option.
/// </summary>
/// <remarks>
/// Why no test-only option is registered here: <see cref="DataStoreTypes"/> is a shared static
/// collection that freezes on first read, and its members come from module initializers in the
/// entry-point assembly. A test cannot hand-register into it — probing with <c>ByName</c> to decide
/// whether to register is itself the read that freezes it, so the registration that follows can only
/// throw. These tests therefore cover the selector's failure paths only; the Found path belongs with
/// a real registered option, not a hand-built one.
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataStoreTypesBuilderSelectorTests
{
    private readonly DataStoreTypesBuilderSelector _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectThrowsArgumentNullExceptionWhenConfigurationIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Select(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectReturnsFailureWhenImplementationIsNotRegistered()
    {
        // Arrange — a name guaranteed not to match any [Implementation]/[TypeOption] registered
        // against DataStoreTypes anywhere in the loaded test assemblies.
        var configuration = new DataStoreImplementationConfiguration
        {
            Name = "OrphanStore",
            Implementation = "ThisTransportDoesNotExist_" + Guid.NewGuid().ToString("N"),
        };

        // Act
        var result = _sut.Select(configuration, NullLogger.Instance);

        // Assert — NoDataStoreTypeFoundAtStartup failure; no builder to return.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectReturnsFailureWhenImplementationIsNull()
    {
        // Arrange — DataStoreTypes.ByName(null) resolves to the NotFound sentinel, same failure path
        // as an unregistered name.
        // null! deliberately: Implementation is non-nullable, and a row that reached this code with no
        // implementation is precisely the malformed input under test. Making the property nullable to
        // satisfy the compiler here would weaken the contract every other caller relies on.
        var configuration = new DataStoreImplementationConfiguration { Name = "OrphanStore", Implementation = null! };

        // Act
        var result = _sut.Select(configuration);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
