using Fdw.Data.Abstractions;
using FdwReferenceData.FileSystem;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// A load into a file that already holds rows (FDW-3193): without a truncate in its unit of work the rows are added to the file, with an unfiltered
/// Delete before the Insert in the same unit they replace it, and a unit that is rolled back leaves the file as it was. A truncate of one file does
/// not touch another. This is what a destination's truncateFirst setting chooses between.
/// </summary>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemReplaceOrAddTests : IDisposable
{
    private static readonly string[] Fields = ["Id", "Name"];

    private readonly string _root = Directory.CreateTempSubdirectory("fdw-fs-replace-").FullName;
    private readonly FileSystemConnection _connection;
    private readonly IDataContainer _chiefs = Container("Chiefs.csv");
    private readonly IDataContainer _broncos = Container("Broncos.csv");

    public FileSystemReplaceOrAddTests() =>
        _connection = new FileSystemConnection(new FileSystemConnectionConfiguration { Root = _root }, logger: null);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    [Trait("Category", "Write")]
    public async Task ALoadWithNoTruncateAddsToTheRowsTheFileHolds()
    {
        await Seed(_chiefs, Row("1", "Ada"), Row("2", "Bob"));

        await Load(_chiefs, truncate: false, Row("3", "Cy"));

        (await Names(_chiefs)).ShouldBe(["Ada", "Bob", "Cy"]);
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ALoadWithATruncateInTheSameUnitReplacesTheRowsTheFileHolds()
    {
        await Seed(_chiefs, Row("1", "Ada"), Row("2", "Bob"));

        await Load(_chiefs, truncate: true, Row("3", "Cy"));

        (await Names(_chiefs)).ShouldBe(["Cy"]);
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task ATruncateOfOneFileLeavesTheOtherFileAsItWas()
    {
        await Seed(_chiefs, Row("1", "Ada"));
        await Seed(_broncos, Row("2", "Bob"));

        await Load(_chiefs, truncate: true, Row("3", "Cy"));

        (await Names(_chiefs)).ShouldBe(["Cy"]);
        (await Names(_broncos)).ShouldBe(["Bob"]);
    }

    [Fact]
    [Trait("Category", "Write")]
    public async Task AReplaceThatIsRolledBackLeavesTheFileAsItWas()
    {
        await Seed(_chiefs, Row("1", "Ada"), Row("2", "Bob"));
        var unit = (await _connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;
        (await unit.Execute<int>(CommandStub.Delete(), _chiefs, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await unit.Execute<int>(Insert(Row("3", "Cy")), _chiefs, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        (await unit.Rollback(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        (await Names(_chiefs)).ShouldBe(["Ada", "Bob"]);
    }

    private static IDataContainer Container(string file) =>
        ContainerStub.Build(file, "Delimited", Fields, new Dictionary<string, object> { ["HasHeader"] = true, ["Separator"] = "," }, null);

    private static Dictionary<string, object?> Row(string id, string name) => new() { ["Id"] = id, ["Name"] = name };

    // The rows a pipeline's load sends are mutable dictionaries, not read-only ones.
    private static Fdw.Commands.Data.Abstractions.IDataCommand Insert(params Dictionary<string, object?>[] rows) =>
        CommandStub.InsertMutable(rows.Cast<IDictionary<string, object?>>().ToList());

    private async Task Seed(IDataContainer container, params Dictionary<string, object?>[] rows) =>
        (await _connection.Execute<int>(Insert(rows), container, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

    private async Task Load(IDataContainer container, bool truncate, params Dictionary<string, object?>[] rows)
    {
        var unit = (await _connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;
        if (truncate)
        {
            (await unit.Execute<int>(CommandStub.Delete(), container, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        }

        (await unit.Execute<int>(Insert(rows), container, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await unit.Commit(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    private async Task<IReadOnlyList<string>> Names(IDataContainer container)
    {
        var read = await _connection.Execute<IEnumerable<IReadOnlyDictionary<string, object?>>>(
            CommandStub.Query(), container, TestContext.Current.CancellationToken);
        read.IsSuccess.ShouldBeTrue(read.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "read failed");
        return read.Value!.Select(row => Convert.ToString(row["Name"], System.Globalization.CultureInfo.InvariantCulture)!).ToList();
    }
}
