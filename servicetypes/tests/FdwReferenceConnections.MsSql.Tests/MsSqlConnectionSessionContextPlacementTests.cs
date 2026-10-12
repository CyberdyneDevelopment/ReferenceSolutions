using System.Data;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// Where a command's session context goes (FDW-977): in the command's own batch when the command can
/// carry it, on the connection ahead of it when it cannot. Every execution path asks
/// <see cref="MsSqlConnection.PlaceSessionContext"/>, so what is asserted here is what every path does.
/// No SQL Server is involved: a batched placement never touches the connection, and the separate apply is
/// recognised by its trying to run on a connection that was never opened.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionSessionContextPlacementTests
{
    private const string Select = "SELECT [Id] FROM [conn].[Connection] WHERE [Name] = @p0";

    private static MsSqlConnection Connection(IAuthenticationContext? context)
        => new(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            authenticationContextAccessor: new StubAuthenticationContextAccessor(context));

    private sealed class StubAuthenticationContextAccessor(IAuthenticationContext? current) : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; } = current;
    }

    private static SqlCommand TextCommand(string text = Select)
    {
        var command = new SqlCommand(text);
        command.Parameters.AddWithValue("@p0", "PlatformConfiguration");
        return command;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APlainTextCommandCarriesTheContextAndTheConnectionIsNotTouched()
    {
        var connection = Connection(new SystemAuthenticationContext());
        var command = TextCommand();
        using var unopened = connection.CreatePooledConnection();

        var placed = await connection.PlaceSessionContext(unopened, command, TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue();
        placed.Value.ShouldNotBeNull().IsBatched.ShouldBeTrue();
        command.CommandText.ShouldStartWith("BEGIN TRY EXEC sp_set_session_context @key = N'System', @value = N'1', @read_only = 1;");
        command.CommandText.ShouldEndWith(" END TRY BEGIN CATCH THROW; END CATCH; " + Select);
        unopened.State.ShouldBe(ConnectionState.Closed);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheDenyPrincipalIsCarriedToo()
    {
        // No accessor context at all resolves to the reserved deny principal, and a carried context must
        // say so exactly as the separate apply does: a UserId key, and nothing that widens it.
        var connection = Connection(null);
        var command = TextCommand();
        using var unopened = connection.CreatePooledConnection();

        var placed = await connection.PlaceSessionContext(unopened, command, TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue();
        command.CommandText.ShouldStartWith("BEGIN TRY EXEC sp_set_session_context @key = N'UserId', @value = @__fdwctx_userId, @read_only = 1;");
        command.CommandText.ShouldNotContain("N'System'");
        command.Parameters["@__fdwctx_userId"].Value.ShouldBe(AuthConstants.NoAccessPrincipalId);
        command.Parameters["@p0"].Value.ShouldBe("PlatformConfiguration");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoredProcedureCommandGetsTheSeparateApply()
    {
        // A stored procedure is called by name and cannot carry a T-SQL prefix, so it keeps the apply it always had. The
        // separate apply is recognised by its trying to run on the connection that was never opened. That is a mistake in the application and
        // not an error from the database, so it is still an exception; a SqlException would come back as a
        // failure result (FDW-980).
        var connection = Connection(new SystemAuthenticationContext());
        var command = new SqlCommand("dbo.usp_LoadOrders") { CommandType = CommandType.StoredProcedure };
        using var unopened = connection.CreatePooledConnection();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await connection.PlaceSessionContext(unopened, command, TestContext.Current.CancellationToken));

        command.CommandText.ShouldBe("dbo.usp_LoadOrders");
        command.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACommandUsingAReservedParameterNameFailsWithAResultAndSendsNothing()
    {
        var connection = Connection(new SystemAuthenticationContext());
        var command = TextCommand();
        command.Parameters.AddWithValue("@__fdwctx_tenantId", Guid.NewGuid());
        using var unopened = connection.CreatePooledConnection();

        var placed = await connection.PlaceSessionContext(unopened, command, TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeFalse();
        command.CommandText.ShouldBe(Select);
        unopened.State.ShouldBe(ConnectionState.Closed);
    }
}
