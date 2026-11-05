using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using FdwReferenceData.FileSystem;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// An Insert into a file adds to the rows the file already holds, as an Insert does on every other connection
/// kind, and a command type the file does not answer is reported as having no translator so a command that
/// expands into simpler ones (Upsert, BulkUpsert, BulkUpdate) runs those.
/// </summary>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemInsertAddsRowsTests
{
    private static readonly string[] Fields = ["Id", "Name"];

    [Fact]
    [Trait("Category", "Write")]
    public async Task InsertAddsToTheRowsTheFileAlreadyHolds()
    {
        using var harness = new FileHarness();

        (await harness.Insert(Row("1", "Ada"), Row("2", "Bob"))).Value.ShouldBe(2);
        (await harness.Insert(Row("3", "Cy"))).Value.ShouldBe(1);

        var names = await harness.Names();
        names.ShouldBe(new[] { "Ada", "Bob", "Cy" });
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task InsertIntoAFileThatDoesNotExistCreatesIt()
    {
        using var harness = new FileHarness();
        File.Exists(harness.FilePath).ShouldBeFalse();

        var result = await harness.Insert(Row("1", "Ada"));

        result.IsSuccess.ShouldBeTrue(result.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "insert failed");
        File.Exists(harness.FilePath).ShouldBeTrue();
        (await harness.Names()).ShouldBe(new[] { "Ada" });
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ACommandThatExpandsRunsItsStepsOnAFile()
    {
        using var harness = new FileHarness();
        await harness.Insert(Row("1", "Ada"));
        var upsert = CommandStub.Expandable(
            "Upsert",
            CommandStub.Insert([Row("2", "Bob")]),
            CommandStub.Insert([Row("3", "Cy")]));

        var result = await harness.Connection.Execute<int>(upsert, harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "expansion failed");
        result.Value.ShouldBe(2);
        (await harness.Names()).ShouldBe(new[] { "Ada", "Bob", "Cy" });
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ACommandTypeTheFileDoesNotAnswerAndCannotExpandFails()
    {
        using var harness = new FileHarness();

        var result = await harness.Connection.Execute<int>(
            CommandStub.OfType("Frobnicate"), harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Query")]
    [InlineData("Insert")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("Find")]
    [InlineData("insert")]
    public void TheTranslatorAnswersTheCommandTypesAFileSupports(string commandType)
        => new FileSystemCommandTranslator("files", logger: null).Handles(commandType).ShouldBeTrue();

    [Theory]
    [InlineData("Upsert")]
    [InlineData("BulkUpsert")]
    [InlineData("BulkUpdate")]
    [InlineData("ConfigurationSave")]
    [InlineData("ConfigurationRetire")]
    public void TheTranslatorDoesNotAnswerTheOthers(string commandType)
        => new FileSystemCommandTranslator("files", logger: null).Handles(commandType).ShouldBeFalse();

    [Fact]
    [Trait("Category", "Write")]
    public async Task ACommandThatExpandsInsideAUnitOfWorkIsUndoneWithIt()
    {
        using var harness = new FileHarness();
        var unit = (await harness.Connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;
        var upsert = CommandStub.Expandable("Upsert", CommandStub.Insert([Row("1", "Ada")]), CommandStub.Insert([Row("2", "Bob")]));

        var result = await unit.Execute<int>(upsert, harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "expansion failed");
        result.Value.ShouldBe(2);
        File.Exists(harness.FilePath).ShouldBeTrue();
        (await unit.Rollback(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        File.Exists(harness.FilePath).ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task AnExpansionWhoseSecondStepFailsLeavesTheFileAsItWas()
    {
        // FDW-3601: outside a unit of work the connection runs an expansion's steps in one unit, so a failed second step puts back
        // what the first one removed.
        using var harness = new FileHarness();
        await harness.Insert(Row("1", "Ada"), Row("2", "Bob"));
        var upsert = CommandStub.Expandable("Upsert", CommandStub.Delete(), CommandStub.OfType("Frobnicate"));

        var result = await harness.Connection.Execute<int>(upsert, harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        (await harness.Names()).ShouldBe(new[] { "Ada", "Bob" });
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ADeleteThatStatesNoRowsIsRefusedAndTheFileKeepsItsRows()
    {
        // FDW-3660: a delete with no filter, no soft delete and no AllRows is refused by the translator; the file is untouched.
        using var harness = new FileHarness();
        await harness.Insert(Row("1", "Ada"), Row("2", "Bob"));

        var result = await harness.Connection.Execute<int>(CommandStub.UnscopedDelete(), harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        (await harness.Names()).ShouldBe(new[] { "Ada", "Bob" });
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ADeleteThatAsksForEveryRowEmptiesTheFile()
    {
        using var harness = new FileHarness();
        await harness.Insert(Row("1", "Ada"), Row("2", "Bob"));

        var result = await harness.Connection.Execute<int>(CommandStub.Delete(), harness.Container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        (await harness.Names()).ShouldBeEmpty();
    }

    private static Dictionary<string, object?> Row(string id, string name) => new() { ["Id"] = id, ["Name"] = name };

    private sealed class FileHarness : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("fdw-fs-insert-").FullName;

        public FileHarness()
        {
            Connection = new FileSystemConnection(new FileSystemConnectionConfiguration { Root = _root }, logger: null);
            Container = ContainerStub.Build(
                "people.csv", "Delimited", Fields, new Dictionary<string, object> { ["HasHeader"] = true, ["Separator"] = "," }, null);
        }

        public FileSystemConnection Connection { get; }

        public IDataContainer Container { get; }

        public string FilePath => Path.Combine(_root, "people.csv");

        public Task<Fdw.Results.IGenericResult<int>> Insert(params Dictionary<string, object?>[] rows)
            => Connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                Container,
                TestContext.Current.CancellationToken);

        public async Task<IReadOnlyList<string>> Names()
        {
            var read = await Connection.Execute<IEnumerable<IReadOnlyDictionary<string, object?>>>(
                CommandStub.Query(), Container, TestContext.Current.CancellationToken);
            read.IsSuccess.ShouldBeTrue(read.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "read failed");
            return read.Value!.Select(r => Convert.ToString(r["Name"], System.Globalization.CultureInfo.InvariantCulture)!).ToList();
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
