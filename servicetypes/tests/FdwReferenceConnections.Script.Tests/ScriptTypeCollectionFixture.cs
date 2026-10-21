using Fdw.Data;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.Script.Tests;

public sealed class ScriptTypeCollectionFixture
{
    public ScriptTypeCollectionFixture()
    {
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
        _ = ContainerTypes.All();
    }
}

[CollectionDefinition(nameof(ScriptTestCollection))]
public sealed class ScriptTestCollection : ICollectionFixture<ScriptTypeCollectionFixture>
{
}
