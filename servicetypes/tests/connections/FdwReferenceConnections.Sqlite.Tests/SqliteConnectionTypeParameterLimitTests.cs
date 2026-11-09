using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.Sqlite.Tests;

/// <summary>The Sqlite connection type declares, on its service type option, the default of how many parameters a key list carries before it goes to one JSON parameter the statement reads (FDW-4267).</summary>
public sealed class SqliteConnectionTypeParameterLimitTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheTypeDeclaresALowDefaultOnItsServiceTypeOption()
    {
        ServiceTypeDefaults.TryResolve(new SqliteConnectionType(), nameof(IDatabaseConnectionConfiguration.MaxParametersBeforeTempTable), out var declared).ShouldBeTrue();
        declared.ShouldBe(50);
    }
}
