using Fdw.Data;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.Sqlite.Tests;

public sealed class SqliteTypeCollectionFixture
{
    public SqliteTypeCollectionFixture()
    {
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
        _ = ContainerTypes.All();
    }
}

[CollectionDefinition(nameof(SqliteTestCollection))]
public sealed class SqliteTestCollection : ICollectionFixture<SqliteTypeCollectionFixture>
{
}
