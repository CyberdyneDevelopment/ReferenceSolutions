using Fdw.Services.Connections.MsSql.Authentication;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Fixture that ensures MsSqlAuthenticationTypes TypeCollection is fully initialized
/// before any tests run. This prevents race conditions in the source-generated
/// EnsureFrozen() method when xUnit runs tests in parallel.
/// </summary>
public sealed class MsSqlProcessorsFixture
{
    public MsSqlProcessorsFixture()
    {
        _ = MsSqlAuthenticationTypes.All();
    }
}

[CollectionDefinition(nameof(MsSqlTestCollection))]
public sealed class MsSqlTestCollection : ICollectionFixture<MsSqlProcessorsFixture>
{
}

/// <summary>
/// Collection that serializes DictionaryPool tests to avoid race conditions
/// with the static pool state.
/// </summary>
[CollectionDefinition("DictionaryPoolTestCollection")]
public sealed class DictionaryPoolTestCollection
{
}
