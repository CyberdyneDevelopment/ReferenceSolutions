using Fdw.Data;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Proves the connection hands a query's Ordering and Paging to the evaluator.
/// </summary>
/// <remarks>
/// The evaluator's own sort and page semantics are covered in FDW
/// (RecordQueryEvaluatorOrderingPagingTests). What is covered here is only the wiring: a file expresses
/// neither ORDER BY nor OFFSET, so the translator carries both onto the native command and the
/// connection applies them over the decoded rows. The original defect was that the translator dropped
/// them, so a file-backed ordered query returned rows in decoder order and reported success — every
/// assertion below passes trivially if the two values stop reaching the evaluator, which is why each
/// asserts an order that differs from the order the rows are written in.
///
/// Wins are single digits so text and numeric collation agree. That keeps this test about whether the
/// values arrive, not about how they sort, which is FDW's question and not this seam's.
/// </remarks>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemOrderingPagingWiringTests
{
    // Deliberately not in wins order — file order and sorted order must disagree, or these prove nothing.
    private static readonly List<Dictionary<string, object?>> Teams =
    [
        new() { ["Team"] = "Bears", ["Wins"] = "3" },
        new() { ["Team"] = "Lions", ["Wins"] = "5" },
        new() { ["Team"] = "Colts", ["Wins"] = "1" },
        new() { ["Team"] = "Jets", ["Wins"] = "4" },
        new() { ["Team"] = "Rams", ["Wins"] = "2" },
    ];

    [Fact]
    public async Task OrderingReachesTheEvaluator()
    {
        var read = await WriteThenRead(Descending("Wins"), paging: null);

        read.Select(r => r["Team"]!.ToString()).ShouldBe(["Lions", "Jets", "Bears", "Rams", "Colts"]);
    }

    [Fact]
    public async Task PagingReachesTheEvaluator()
    {
        var read = await WriteThenRead(ordering: null, paging: new PagingExpression { Skip = 1, Take = 2 });

        read.Count.ShouldBe(2);
        read.Select(r => r["Team"]!.ToString()).ShouldBe(["Lions", "Colts"]);
    }

    [Fact]
    public async Task OrderingIsAppliedBeforePagingThroughTheConnection()
    {
        // Why this is the load-bearing case: skip/take over the FILE order would yield Lions, Colts.
        // Over the SORTED order it yields Jets, Bears. Only the second is correct, and only if both
        // values reached the evaluator and ordering ran first.
        var read = await WriteThenRead(Descending("Wins"), new PagingExpression { Skip = 1, Take = 2 });

        read.Select(r => r["Team"]!.ToString()).ShouldBe(["Jets", "Bears"]);
    }

    [Fact]
    public async Task WithoutEitherTheFileOrderIsKept()
    {
        var read = await WriteThenRead(ordering: null, paging: null);

        read.Select(r => r["Team"]!.ToString())
            .ShouldBe(["Bears", "Lions", "Colts", "Jets", "Rams"]);
    }

    private static IOrderingExpression Descending(string field) =>
        new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = field, Direction = SortDirections.ByName("Descending") }],
        };

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> WriteThenRead(
        IOrderingExpression? ordering,
        IPagingExpression? paging)
    {
        var root = Directory.CreateTempSubdirectory("fdw-fs-orderpage-").FullName;
        try
        {
            var connection = new FileSystemConnection(
                new FileSystemConnectionConfiguration { Root = root },
                logger: null);

            var container = ContainerStub.Build(
                "teams.csv",
                "Delimited",
                ["Team", "Wins"],
                new Dictionary<string, object> { ["HasHeader"] = true, ["Separator"] = "," },
                fieldMetadata: null);

            var write = await connection.Execute<int>(
                CommandStub.Insert(Teams.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                container,
                TestContext.Current.CancellationToken);
            write.IsSuccess.ShouldBeTrue(
                write.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "write failed");

            var read = await connection.Execute<IEnumerable<IReadOnlyDictionary<string, object?>>>(
                CommandStub.Query(ordering, paging),
                container,
                TestContext.Current.CancellationToken);
            read.IsSuccess.ShouldBeTrue(
                read.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "read failed");

            return read.Value!.ToList();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
