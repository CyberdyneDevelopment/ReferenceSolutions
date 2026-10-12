using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// What a failed command becomes when its session context rides in its batch (FDW-977, with FDW-980's
/// "nothing is thrown"). The three batched execution paths (a typed read, a typed child-row read, a cursor)
/// end in the same <see cref="MsSqlConnection.SessionContextPlacement.FailureMessages"/> in their
/// <c>catch (SqlException)</c>, so what is asserted here is what each of them returns and logs. No SQL Server
/// is involved: the exceptions are real <see cref="SqlException"/>s, carrying the number and the procedure a
/// server reports, built by <see cref="TestSqlException"/>. That the paths call this from their catch is
/// read from the code, not run; nothing here can reach a catch inside a method that opens a connection.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionSessionContextFailureTests
{
    // SQL Server's own error for setting a key that is already set read-only on the session, which is what a
    // failed context statement raises. (Verified on devConfigurationDb: number 15664, severity 16, and the
    // procedure the error reports is sp_set_session_context.)
    private const int ReadOnlyKeyAlreadySet = 15664;
    private const string ContextProcedure = "sp_set_session_context";
    private const string Select = "SELECT [Id] FROM [conn].[Connection] WHERE [Name] = @p0";

    private const int TenantContextSetFailedId = 71018;
    private const int SqlObjectNotFoundId = 30000;
    private const int UnhandledSqlErrorId = 71017;

    private static MsSqlConnection Connection()
        => new(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            authenticationContextAccessor: new StubAuthenticationContextAccessor(new SystemAuthenticationContext()));

    private sealed class StubAuthenticationContextAccessor(IAuthenticationContext? current) : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; } = current;
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<int> _eventIds = [];

        public IReadOnlyList<int> EventIds
        {
            get
            {
                lock (_eventIds) return _eventIds.ToArray();
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_eventIds) _eventIds.Add(eventId.Id);
        }
    }

    // A placement whose command carries the context in its own batch, made the way every path makes one.
    private static async Task<MsSqlConnection.SessionContextPlacement> Batched()
    {
        var connection = Connection();
        var command = new SqlCommand(Select);
        command.Parameters.AddWithValue("@p0", "PlatformConfiguration");
        using var unopened = connection.CreatePooledConnection();

        var placed = await connection.PlaceSessionContext(unopened, command, TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue();
        var placement = placed.Value.ShouldNotBeNull();
        placement.IsBatched.ShouldBeTrue();
        return placement;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContextThatCouldNotBeSetInTheBatchIsLoggedAndReturnedAsTheContextFailureThenTheSqlErrorNotThrown()
    {
        var placement = await Batched();
        var logger = new RecordingLogger();
        var exception = TestSqlException.Create(ReadOnlyKeyAlreadySet, "Cannot set key read_only for this session.", ContextProcedure);

        var messages = placement.FailureMessages(logger, exception, Select);

        // The context failure comes first and says what Apply says when it fails on its own, then the SQL
        // error an unmapped number gets (which carries the number), then each exception in the chain.
        messages.Count.ShouldBeGreaterThanOrEqualTo(2);
        messages[0].Code.ShouldBe("MSSQL-71018");
        messages[1].Code.ShouldBe("MSSQL-71017");
        messages[0].Message.ShouldContain("Failed to set tenant SESSION_CONTEXT");
        messages[0].Message.ShouldContain("Cannot set key read_only for this session.");
        messages[1].Message.ShouldContain(ReadOnlyKeyAlreadySet.ToString(System.Globalization.CultureInfo.InvariantCulture));
        logger.EventIds.ShouldContain(TenantContextSetFailedId);
        logger.EventIds.ShouldContain(UnhandledSqlErrorId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContextFailureIsRecognisedFromTheProcedureNotFromTheMessage()
    {
        // The message is words a server or a locale may change; the procedure name is what SQL Server reports.
        var placement = await Batched();
        var logger = new RecordingLogger();
        var exception = TestSqlException.Create(ReadOnlyKeyAlreadySet, "a message that says nothing about the session", ContextProcedure);

        placement.FailureMessages(logger, exception, Select);

        logger.EventIds.ShouldContain(TenantContextSetFailedId);
    }

    [Theory]
    [InlineData(208, "Invalid object name 'conn.Connection'.", "")]
    [InlineData(ReadOnlyKeyAlreadySet, "the same number, from a different procedure", "sp_executesql")]
    [InlineData(229, "The SELECT permission was denied.", "")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACommandsOwnSqlErrorInABatchedCommandIsNotMistakenForAContextFailure(int number, string message, string procedure)
    {
        var placement = await Batched();
        var logger = new RecordingLogger();
        var exception = TestSqlException.Create(number, message, procedure);

        var messages = placement.FailureMessages(logger, exception, Select);

        messages.Count.ShouldBeGreaterThanOrEqualTo(1);
        logger.EventIds.ShouldNotContain(TenantContextSetFailedId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASqlErrorFromACommandWhoseContextWasSetSeparatelyIsNeverACommandBatchContextFailure()
    {
        // A failed separate apply is answered by Apply itself, before the command is sent. If an error from
        // sp_set_session_context still reached the command's catch, it came from something else.
        var logger = new RecordingLogger();
        var exception = TestSqlException.Create(ReadOnlyKeyAlreadySet, "Cannot set key read_only for this session.", ContextProcedure);

        var messages = MsSqlConnection.SessionContextPlacement.Separate.FailureMessages(logger, exception, Select);

        messages.Count.ShouldBeGreaterThanOrEqualTo(1);
        logger.EventIds.ShouldNotContain(TenantContextSetFailedId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedCommandIsLoggedWithTheCommandThatWasAskedForNeverTheBatchThatWasSent()
    {
        var placement = await Batched();
        var logger = new RecordingLogger();
        var exception = TestSqlException.Create(208, "Invalid object name 'conn.Connection'.");

        var messages = placement.FailureMessages(logger, exception, Select);

        // Error 208 is handled by ObjectNotFoundHandler, which logs SqlObjectNotFound for the command asked for. The
        // catch site logs SqlExecutionError (71002) itself, before it calls FailureMessages, so it is not asserted here.
        logger.EventIds.ShouldContain(SqlObjectNotFoundId);
        messages.Any(m => m.Message.Contains(Select, StringComparison.Ordinal)).ShouldBeTrue();
        messages.Any(m => m.Message.Contains("BEGIN TRY", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(ContextProcedure, true)]
    [InlineData("SP_SET_SESSION_CONTEXT", true)]
    [InlineData("sp_executesql", false)]
    [InlineData("", false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASqlExceptionIsAContextFailureWhenAnErrorInItCameFromTheContextProcedure(string procedure, bool expected)
    {
        var exception = TestSqlException.Create(ReadOnlyKeyAlreadySet, "x", procedure);

        BatchedSessionContext.IsContextFailure(exception).ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASqlExceptionWithSeveralErrorsIsAContextFailureWhenAnyOfThemCameFromTheContextProcedure()
    {
        var exception = TestSqlException.Create(
            (208, "Invalid object name.", string.Empty),
            (ReadOnlyKeyAlreadySet, "Cannot set key read_only for this session.", ContextProcedure));

        exception.Errors.Count.ShouldBe(2);
        BatchedSessionContext.IsContextFailure(exception).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheTestExceptionReadsBackAsBuilt()
    {
        // Guards the builder itself: if a SqlClient upgrade changes what reflection can make, this names it.
        var exception = TestSqlException.Create(ReadOnlyKeyAlreadySet, "m", ContextProcedure);

        exception.Number.ShouldBe(ReadOnlyKeyAlreadySet);
        exception.Errors[0].Procedure.ShouldBe(ContextProcedure);
        exception.Message.ShouldBe("m");
    }
}
