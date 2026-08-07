using Fdw.Data.JsonSchema;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Fixture that ensures DataServiceResultCodes TypeCollection is fully initialized
/// before any tests run. This prevents race conditions in the source-generated
/// EnsureFrozen() method when xUnit runs tests in parallel.
/// </summary>
public sealed class DataServiceResultCodesFixture
{
    public DataServiceResultCodesFixture()
    {
        _ = DataServiceResultCodes.All();
        // Why: JsonSchemaConverters must be materialized (converter assembly loaded) before
        // any BuildStorageContainer path runs in tests.
        _ = JsonSchemaConverters.All();
    }
}

[CollectionDefinition(nameof(DataServiceTestCollection))]
public sealed class DataServiceTestCollection : ICollectionFixture<DataServiceResultCodesFixture>
{
}
