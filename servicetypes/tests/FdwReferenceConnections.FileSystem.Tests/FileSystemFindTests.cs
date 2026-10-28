using Fdw.Commands.Data;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Find on a file: the rows in which a searched field contains the search term, in file order, each with the
/// fields that matched.
/// </summary>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemFindTests
{
    private static readonly string[] Fields = ["Id", "Name", "City"];

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindAnswersTheRowsWhoseTextFieldsContainTheTermWithTheFieldsThatMatched()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"), Row("2", "Bob", "Adelaide"), Row("3", "Cy", "Lyon"));

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ad" });

        result.IsSuccess.ShouldBeTrue(result.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "find failed");
        var found = result.Value!.ToList();
        found.Select(f => f.Record["Id"]).ShouldBe(new object?[] { "1", "2" });
        found[0].MatchedFields.ShouldBe(new[] { "Name" });
        found[1].MatchedFields.ShouldBe(new[] { "City" });
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindSearchesOnlyTheFieldsItNames()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"), Row("2", "Bob", "Adelaide"));

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ad", FieldNames = ["City"] });

        result.IsSuccess.ShouldBeTrue(result.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "find failed");
        result.Value!.Select(f => f.Record["Id"]).ShouldBe(new object?[] { "2" });
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindIgnoresCaseUnlessToldToRespectIt()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"));

        var loose = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ADA" });
        var strict = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ADA", CaseSensitive = true });

        loose.Value!.Count().ShouldBe(1);
        strict.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindAnswersNoMoreRowsThanMaxResults()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"), Row("2", "Adam", "Rome"), Row("3", "Adel", "Oslo"));

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ad", MaxResults = 2 });

        result.Value!.Select(f => f.Record["Id"]).ShouldBe(new object?[] { "1", "2" });
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindWithNoSearchTermFailsLoud()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"));

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = " " });

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("empty search term");
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindByAFieldTheContainerDoesNotDeclareFailsLoud()
    {
        using var harness = new FileHarness();
        await harness.Seed(Row("1", "Ada", "Paris"));

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ad", FieldNames = ["Country"] });

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Country");
    }

    [Fact]
    [Trait("Category", "Find")]
    public async Task FindOnAFileThatDoesNotExistFailsLoud()
    {
        using var harness = new FileHarness();

        var result = await harness.Find(new FindCommand<Dictionary<string, object?>> { SearchTerm = "ad" });

        result.IsSuccess.ShouldBeFalse();
    }

    private static Dictionary<string, object?> Row(string id, string name, string city)
        => new() { ["Id"] = id, ["Name"] = name, ["City"] = city };

    private sealed class FileHarness : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("fdw-fs-find-").FullName;

        public FileHarness()
        {
            Connection = new FileSystemConnection(new FileSystemConnectionConfiguration { Root = _root }, logger: null);
            Container = ContainerStub.Build(
                "people.csv", "Delimited", Fields, new Dictionary<string, object> { ["HasHeader"] = true, ["Separator"] = "," }, null);
        }

        public FileSystemConnection Connection { get; }

        public IDataContainer Container { get; }

        public async Task Seed(params Dictionary<string, object?>[] rows)
        {
            var written = await Connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                Container,
                TestContext.Current.CancellationToken);
            written.IsSuccess.ShouldBeTrue(written.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "seed failed");
        }

        public Task<Fdw.Results.IGenericResult<IEnumerable<FindResult<Dictionary<string, object?>>>>> Find(
            FindCommand<Dictionary<string, object?>> command)
            => Connection.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(
                command, Container, TestContext.Current.CancellationToken);

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
