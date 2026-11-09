using System.Runtime.CompilerServices;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Sql;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// The route a SQL Server connection and its unit of work give a command (FDW-1152): serial SQL up to MaxSerialInsert rows, the
/// bulk copy above it, on the unit of work's own connection and transaction when there is one. No SQL Server is involved: the
/// bulk copy is replaced by a recording writer, and the unit of work's transaction is an object that is never used to run SQL.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlBulkRouteTests
{
    private const int MaxSerialInsert = 1000;
    private const int CommandTimeoutSeconds = 45;

    // A container of the given number of insertable columns, the first named Name.
    private static IDataContainer Container(int columns = 1)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(t => t.ClrType).Returns(typeof(string));
        fieldType.Setup(t => t.TypeName).Returns("String");

        var fields = Enumerable.Range(0, columns).Select(i =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(i == 0 ? "Name" : $"Extra{i}");
            field.Setup(f => f.FieldType).Returns(fieldType.Object);
            return field.Object;
        }).ToArray();

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Customers");
        container.Setup(c => c.Path).Returns(new DatabasePath("", "dbo", "Customers"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container.Object;
    }

    private static IDataContainer ContainerWithoutADatabasePath()
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Customers");
        container.Setup(c => c.Path).Returns(new Mock<IPath>().Object);
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container.Object;
    }

    private static InsertCommand<IDictionary<string, object?>> Insert(int rows) => new(
        Enumerable.Range(1, rows).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Name"] = $"Row {i}" }).ToList());

    private sealed class RecordingWriter
    {
        public int Calls { get; private set; }

        public MsSqlBulkCopyPlan? Plan { get; private set; }

        public SqlConnection? Connection { get; private set; }

        public SqlTransaction? Transaction { get; private set; }

        public Task<int> Write(MsSqlBulkCopyPlan plan, SqlConnection connection, SqlTransaction? transaction, CancellationToken cancellationToken)
        {
            Calls++;
            Plan = plan;
            Connection = connection;
            Transaction = transaction;
            return Task.FromResult(plan.Rows.Rows.Count);
        }
    }

    // A SqlTransaction cannot be built without a server; the unit of work only hands it on, so an uninitialized one is enough to
    // see that the very same transaction reaches the bulk copy.
    private static SqlTransaction NeverUsedTransaction() => (SqlTransaction)RuntimeHelpers.GetUninitializedObject(typeof(SqlTransaction));

    private static MsSqlDataConnectionTransaction UnitOfWork(SqlConnection connection, SqlTransaction transaction, RecordingWriter writer)
        => new(connection, transaction, NullLogger.Instance, "Main", MaxSerialInsert, CommandTimeoutSeconds, writer.Write)
        {
            TranslationSettings = new TranslationSettings(2100),
        };

    [Theory]
    [InlineData(1000, false)]
    [InlineData(1001, true)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInsertTakesTheBulkRouteOnlyAboveTheLimit(int rows, bool takesBulk)
    {
        MsSqlBulkRoute.Takes(Insert(rows), Container(), MaxSerialInsert, 2100).ShouldBe(takesBulk);
    }

    [Theory]
    [InlineData(140, false)]
    [InlineData(141, true)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInsertTakesTheBulkRouteWhereOneRequestCannotCarryItsRows(int rows, bool takesBulk)
    {
        // Fifteen columns, as the earthquake table has: one request carries 2100 / 15 = 140 rows however high MaxSerialInsert is
        // set, so 141 rows (well under the 1000 limit) cannot go as INSERT statements (FDW-1173).
        MsSqlBulkRoute.Takes(Insert(rows), Container(columns: 15), MaxSerialInsert, 2100).ShouldBe(takesBulk);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatLowersTheParameterLimitCarriesFewerRowsPerRequest()
    {
        // Fifteen columns: 1500 parameters carry 100 rows where SQL Server's 2100 carry 140 (FDW-4267).
        MsSqlBulkRoute.SerialLimit(Container(columns: 15), MaxSerialInsert, 1500).ShouldBe(100);
        MsSqlBulkRoute.SerialLimit(Container(columns: 15), MaxSerialInsert, 50).ShouldBe(3);
        MsSqlBulkRoute.Takes(Insert(101), Container(columns: 15), MaxSerialInsert, 1500).ShouldBeTrue();
        MsSqlBulkRoute.Takes(Insert(100), Container(columns: 15), MaxSerialInsert, 1500).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheSerialLimitIsTheLowerOfTheConfiguredLimitAndWhatOneRequestCarries()
    {
        MsSqlBulkRoute.SerialLimit(Container(columns: 15), MaxSerialInsert, 2100).ShouldBe(140);
        MsSqlBulkRoute.SerialLimit(Container(columns: 1), MaxSerialInsert, 2100).ShouldBe(MaxSerialInsert);
        MsSqlBulkRoute.SerialLimit(Container(columns: 15), 50, 2100).ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AUnitOfWorkCopiesAnInsertThatOneRequestCannotCarry()
    {
        var writer = new RecordingWriter();
        var unit = UnitOfWork(new SqlConnection(), NeverUsedTransaction(), writer);

        var result = await unit.Execute<int>(Insert(141), Container(columns: 15), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(141);
        writer.Calls.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandThatDoesNotRouteNeverTakesTheBulkRoute()
    {
        MsSqlBulkRoute.Takes(new DeleteCommand(), Container(), 0, 2100).ShouldBeFalse();
        MsSqlBulkRoute.Takes(new Mock<IDataCommand>().Object, Container(), 0, 2100).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandThatIsNotAnInsertIsRefusedTheBulkModeWithACodedResult()
    {
        var plan = MsSqlBulkRoute.Plan(new DeleteCommand(), Container(), NullLogger.Instance, "Main", MaxSerialInsert, 2100, inTransaction: false);

        plan.IsSuccess.ShouldBeFalse();
        plan.CurrentMessage.ShouldNotBeNull().ShouldContain("bulk");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AUnitOfWorkCopiesAnInsertAboveTheLimitOnItsConnectionAndTransaction()
    {
        var writer = new RecordingWriter();
        var connection = new SqlConnection();
        var transaction = NeverUsedTransaction();
        var unit = UnitOfWork(connection, transaction, writer);

        var result = await unit.Execute<int>(Insert(MaxSerialInsert + 1), Container(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(MaxSerialInsert + 1);
        writer.Calls.ShouldBe(1);
        writer.Connection.ShouldBeSameAs(connection);
        writer.Transaction.ShouldBeSameAs(transaction);
        writer.Plan.ShouldNotBeNull().Destination.ShouldBe("[dbo].[Customers]");
        writer.Plan.Columns.ShouldBe(["Name"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AUnitOfWorkDoesNotCopyAnInsertUpToTheLimit()
    {
        var writer = new RecordingWriter();
        var unit = UnitOfWork(new SqlConnection(), NeverUsedTransaction(), writer);

        // Up to the limit the insert is translated to SQL like any other command; a container that is not a database path fails
        // that translation before anything runs, which is how this shows the bulk copy was not taken.
        var result = await unit.Execute<int>(Insert(MaxSerialInsert), ContainerWithoutADatabasePath(), TestContext.Current.CancellationToken);

        writer.Calls.ShouldBe(0);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommandInAUnitOfWorkTakesTheConnectionsCommandTimeout()
    {
        var unit = UnitOfWork(new SqlConnection(), NeverUsedTransaction(), new RecordingWriter());
        using var command = new SqlCommand("UPDATE t SET x = 1");

        // The connection is closed, so the run fails; what matters is the timeout the command was given before it ran (FDW-3279).
        await unit.ExecuteNative<int>(command, Container(), TestContext.Current.CancellationToken);

        command.CommandTimeout.ShouldBe(CommandTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedBulkCopyInAUnitOfWorkIsACodedFailureNotAnException()
    {
        var unit = new MsSqlDataConnectionTransaction(
            new SqlConnection(), NeverUsedTransaction(), NullLogger.Instance, "Main", MaxSerialInsert, CommandTimeoutSeconds,
            (_, _, _, _) => throw new InvalidOperationException("the copy failed"))
        {
            TranslationSettings = new TranslationSettings(2100),
        };

        var result = await unit.Execute<int>(Insert(MaxSerialInsert + 1), Container(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatDoesNotStateMaxSerialInsertIsRefusedNamingIt()
    {
        var factory = new MsSqlConnectionFactory(NullLogger<MsSqlConnectionFactory>.Instance, NullLogger<MsSqlConnection>.Instance) { DefaultMaxParametersBeforeTempTable = 50 };
        var configuration = new MsSqlConnectionConfiguration
        {
            Port = 1433,
            Encrypt = true,
            EnableConnectionPooling = true,
            MaxPoolSize = 100,
            DefaultSchema = "dbo",
            Name = "Main",
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

        var result = factory.Create(configuration, resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Main");
        result.CurrentMessage.ShouldContain("MaxSerialInsert");
    }
}
