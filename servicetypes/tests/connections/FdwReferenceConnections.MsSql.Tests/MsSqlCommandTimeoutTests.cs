using System;
using Fdw.Messages;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// The command timeout a SQL Server connection gives its commands (FDW-3279): the number the factory resolved for the connection, not the
/// driver's default of 30 seconds, and a command that outruns it fails with a code that names the connection and the seconds. No SQL
/// Server is involved: the commands are built and read, never run.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlCommandTimeoutTests
{
    private static MsSqlConnection ConnectionWith(int commandTimeoutSeconds)
        => new(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            commandTimeoutSeconds: commandTimeoutSeconds,
            authenticationContextAccessor: null)
        {
            TranslationSettings = new TranslationSettings(50),
        };

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(600)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandAttachedToTheConnectionTakesTheConnectionsCommandTimeout(int seconds)
    {
        using var command = new SqlCommand("SELECT 1");
        using var sqlConnection = new SqlConnection();

        ConnectionWith(seconds).Attach(command, sqlConnection);

        command.Connection.ShouldBeSameAs(sqlConnection);
        command.CommandTimeout.ShouldBe(seconds);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandThatOutranItsTimeoutFailsWithACodeNamingTheConnectionAndTheSeconds()
    {
        var existing = new[] { new GenericMessage("the handler's own message") };

        var messages = MsSqlConnection.TimeoutFirst(NullLogger.Instance, -2, "Main", 45, "WAITFOR DELAY '00:01:00'", string.Empty, existing);

        messages.Count.ShouldBe(2);
        messages[0].Code.ShouldBe("MSSQL-81010");
        var text = messages[0].ShouldBeAssignableTo<IClientMessage>().ClientText;
        text.ShouldContain("'Main'");
        text.ShouldContain("45 seconds");
        text.ShouldContain("the connection");
        messages[1].ShouldBeSameAs(existing[0]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandGivenItsOwnTimeoutKeepsItThroughAttachOverTheConnectionsAndRoundsUp()
    {
        using var timed = new SqlCommand("SELECT 1");
        using var plain = new SqlCommand("SELECT 1");
        using var sqlConnection = new SqlConnection();
        var connection = ConnectionWith(30);

        connection.Limit(timed, TimeSpan.FromMilliseconds(1500), "secret manager 'Vault'");
        connection.Attach(timed, sqlConnection);
        connection.Attach(plain, sqlConnection);

        timed.CommandTimeout.ShouldBe(2);
        plain.CommandTimeout.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATimeoutOfACommandThatCarriedItsOwnNamesWhoSetItAndTheSecondsAppliedNotTheConnections()
    {
        var existing = new[] { new GenericMessage("the handler's own message") };

        var messages = MsSqlConnection.TimeoutFirst(NullLogger.Instance, -2, "Main", 2, "SELECT 1", "secret manager 'Vault'", existing);

        var text = messages[0].ShouldBeAssignableTo<IClientMessage>().ClientText;
        messages[0].Code.ShouldBe("MSSQL-81010");
        text.ShouldContain("2 seconds");
        text.ShouldContain("secret manager 'Vault'");
        text.ShouldContain("'Main'");
    }

    [Theory]
    [InlineData(1205)]
    [InlineData(208)]
    [InlineData(0)]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnyOtherSqlErrorIsLeftAsItsHandlerSaidIt(int errorNumber)
    {
        var existing = new[] { new GenericMessage("the handler's own message") };

        var messages = MsSqlConnection.TimeoutFirst(NullLogger.Instance, errorNumber, "Main", 45, "SELECT 1", string.Empty, existing);

        messages.ShouldBeSameAs(existing);
    }
}
