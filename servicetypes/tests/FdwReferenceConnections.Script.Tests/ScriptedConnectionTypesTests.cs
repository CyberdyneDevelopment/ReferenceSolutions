using System.Linq;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.Script.Tests;

/// <summary>
/// The script extensions this package registers (FDW-1447): one for each kind of database connection, each wrapping only its own
/// kind of connection.
/// </summary>
[Collection(nameof(ScriptTestCollection))]
public sealed class ScriptedConnectionTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ThePackageRegistersAnExtensionForSqlServerPostgreSqlAndSqlite()
    {
        ScriptedConnectionTypes.All().Select(type => type.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["MsSql", "PostgreSql", "Sqlite"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NoExtensionWrapsAConnectionOfAnotherKind()
    {
        var other = new Mock<IDataConnection>().Object;

        ScriptedConnectionTypes.All().Select(type => type.Adapt(other)).ShouldAllBe(adapted => adapted == null);
    }
}
