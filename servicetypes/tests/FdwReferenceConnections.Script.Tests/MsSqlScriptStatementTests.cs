using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace FdwReferenceConnections.Script.Tests;

/// <summary>
/// What SQL Server is sent for a script (FDW-1435): the text as written, each value as a named parameter, a null as a database
/// null. No SQL Server is involved: the statement is built and read, never run.
/// </summary>
[Collection(nameof(ScriptTestCollection))]
public sealed class MsSqlScriptStatementTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTextIsSentAsWrittenAndEveryValueTravelsAsANamedParameter()
    {
        var command = new ScriptReadCommand(
            "SELECT * FROM Teams WHERE Year = @Year AND Name = @Name",
            [new ScriptParameter("Year", 2026), new ScriptParameter("Name", "x'; DROP TABLE Teams; --")],
            null);

        using var statement = MsSqlScriptConnection.ScriptStatement(command);

        statement.CommandText.ShouldBe("SELECT * FROM Teams WHERE Year = @Year AND Name = @Name");
        statement.Parameters.Count.ShouldBe(2);
        statement.Parameters["@Year"].Value.ShouldBe(2026);
        statement.Parameters["@Name"].Value.ShouldBe("x'; DROP TABLE Teams; --");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANullValueIsADatabaseNull()
    {
        var command = new ScriptRunCommand("UPDATE T SET Note = @Note", [new ScriptParameter("Note", null)]);

        using var statement = MsSqlScriptConnection.ScriptStatement(command);

        statement.Parameters["@Note"].Value.ShouldBe(System.DBNull.Value);
    }
}
