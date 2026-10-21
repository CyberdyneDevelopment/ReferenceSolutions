using System.Text;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Tests for the document operations of the FileSystem connection over a temporary folder (FDW-1798): the documents of a folder are
/// uploaded, read whole or by range, listed in pages, copied, moved and deleted, and nothing outside the folder is reachable.
/// </summary>
public sealed class FileSystemDocumentStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fdw-documents-" + Guid.NewGuid().ToString("N"));
    private readonly FileSystemDocumentStore _store;

    public FileSystemDocumentStoreTests()
    {
        Directory.CreateDirectory(_root);
        _store = new FileSystemDocumentStore(Path.GetFullPath(_root), "LocalFs", NullLogger.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static DocumentPath At(string path) => DocumentPath.Parse(path).Value;

    private static Stream Bytes(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private static string Text(DocumentContent content)
    {
        using (content)
        {
            using var reader = new StreamReader(content.Content, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }

    private Task<IGenericResult<DocumentInfo>> Upload(string path, string text, bool createOnly = false)
        => _store.Execute(new UploadCommand(At(path), Bytes(text), null, createOnly, new Dictionary<string, string>()), TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentUploadedCanBeReadBackWhole()
    {
        var uploaded = await Upload("finance/2026/lease.txt", "seven years");

        uploaded.IsSuccess.ShouldBeTrue();
        uploaded.Value!.Length.ShouldBe(11);
        var downloaded = await _store.Execute(new DownloadCommand(At("finance/2026/lease.txt"), null), TestContext.Current.CancellationToken);
        downloaded.IsSuccess.ShouldBeTrue();
        Text(downloaded.Value!).ShouldBe("seven years");
        File.Exists(Path.Combine(_root, "finance", "2026", "lease.txt")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACreateOnlyUploadOverAnExistingDocumentIsRefusedAndLeavesItAlone()
    {
        await Upload("a.txt", "first");

        var second = await Upload("a.txt", "second", createOnly: true);

        second.IsFailure.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_root, "a.txt")).ShouldBe("first");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnUploadWithoutCreateOnlyReplacesTheDocument()
    {
        await Upload("a.txt", "first");

        var second = await Upload("a.txt", "second");

        second.IsSuccess.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_root, "a.txt")).ShouldBe("second");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnUploadThatCarriesMetadataIsRefusedAndWritesNothing()
    {
        var uploaded = await _store.Execute(
            new UploadCommand(At("a.txt"), Bytes("x"), null, false, new Dictionary<string, string> { ["signed"] = "2020-01-01" }),
            TestContext.Current.CancellationToken);

        uploaded.IsFailure.ShouldBeTrue();
        File.Exists(Path.Combine(_root, "a.txt")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARangeReturnsOnlyTheBytesAskedFor()
    {
        await Upload("a.txt", "0123456789");

        var ranged = await _store.Execute(new DownloadCommand(At("a.txt"), new DocumentRange(3, 4)), TestContext.Current.CancellationToken);

        Text(ranged.Value!).ShouldBe("3456");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ARangeAfterTheEndIsRefused()
    {
        await Upload("a.txt", "0123");

        var ranged = await _store.Execute(new DownloadCommand(At("a.txt"), new DocumentRange(10, 4)), TestContext.Current.CancellationToken);

        ranged.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AMissingDocumentIsNotFound()
    {
        var info = await _store.Execute(new GetInfoCommand(At("nothing.txt")), TestContext.Current.CancellationToken);
        var download = await _store.Execute(new DownloadCommand(At("nothing.txt"), null), TestContext.Current.CancellationToken);

        info.IsFailure.ShouldBeTrue();
        download.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ListingPagesInPathOrderAndContinuesFromTheLastPath()
    {
        foreach (var name in new[] { "c.txt", "a.txt", "b.txt" })
        {
            await Upload(name, name);
        }

        var first = await _store.Execute(new ListCommand(DocumentPath.Root, false, 2, null), TestContext.Current.CancellationToken);
        var second = await _store.Execute(new ListCommand(DocumentPath.Root, false, 2, first.Value!.Continuation), TestContext.Current.CancellationToken);

        first.Value.Items.Select(item => item.Path.Value).ShouldBe(["a.txt", "b.txt"]);
        first.Value.Continuation.ShouldBe("b.txt");
        second.Value!.Items.Select(item => item.Path.Value).ShouldBe(["c.txt"]);
        second.Value.Continuation.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFolderListsItsOwnDocumentsOnlyUnlessTheListIsRecursive()
    {
        await Upload("top.txt", "t");
        await Upload("sub/deep.txt", "d");

        var flat = await _store.Execute(new ListCommand(DocumentPath.Root, false, 10, null), TestContext.Current.CancellationToken);
        var recursive = await _store.Execute(new ListCommand(DocumentPath.Root, true, 10, null), TestContext.Current.CancellationToken);
        var sub = await _store.Execute(new ListCommand(At("sub"), false, 10, null), TestContext.Current.CancellationToken);

        flat.Value!.Items.Select(item => item.Path.Value).ShouldBe(["top.txt"]);
        recursive.Value!.Items.Select(item => item.Path.Value).ShouldBe(["sub/deep.txt", "top.txt"]);
        sub.Value!.Items.Select(item => item.Path.Value).ShouldBe(["sub/deep.txt"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFolderThatDoesNotExistListsNothing()
    {
        var listed = await _store.Execute(new ListCommand(At("nowhere"), true, 10, null), TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Items.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task CopyKeepsTheSourceAndMoveRemovesIt()
    {
        await Upload("a.txt", "a");

        var copied = await _store.Execute(new CopyCommand(At("a.txt"), At("copies/a.txt"), true), TestContext.Current.CancellationToken);
        var moved = await _store.Execute(new MoveCommand(At("a.txt"), At("moved/a.txt"), true), TestContext.Current.CancellationToken);

        copied.IsSuccess.ShouldBeTrue();
        moved.IsSuccess.ShouldBeTrue();
        File.Exists(Path.Combine(_root, "copies", "a.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(_root, "moved", "a.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(_root, "a.txt")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACreateOnlyCopyOverAnExistingDocumentIsRefused()
    {
        await Upload("a.txt", "a");
        await Upload("b.txt", "b");

        var copied = await _store.Execute(new CopyCommand(At("a.txt"), At("b.txt"), true), TestContext.Current.CancellationToken);

        copied.IsFailure.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_root, "b.txt")).ShouldBe("b");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteRemovesTheDocumentAndSaysWhetherThereWasOne()
    {
        await Upload("a.txt", "a");

        var first = await _store.Execute(new DeleteCommand(At("a.txt")), TestContext.Current.CancellationToken);
        var second = await _store.Execute(new DeleteCommand(At("a.txt")), TestContext.Current.CancellationToken);

        first.Value.ShouldBeTrue();
        second.Value.ShouldBeFalse();
        File.Exists(Path.Combine(_root, "a.txt")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadLinkIsRefusedBecauseTheConnectionCreatesNone()
    {
        var link = await _store.Execute(new CreateReadLinkCommand(At("a.txt"), TimeSpan.FromMinutes(5)), TestContext.Current.CancellationToken);

        link.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void APathThatLeavesTheRootCannotBeMade()
    {
        DocumentPath.Parse("../outside.txt").IsFailure.ShouldBeTrue();
        DocumentPath.Parse("sub/../../outside.txt").IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARootConfiguredWithATrailingSeparatorUploadsListsAndReadsLikeOneWithout()
    {
        var store = new FileSystemDocumentStore(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, "LocalFs", NullLogger.Instance);
        var uploaded = await store.Execute(
            new UploadCommand(At("finance/lease.txt"), Bytes("seven years"), null, false, new Dictionary<string, string>()), TestContext.Current.CancellationToken);

        var listed = await store.Execute(new ListCommand(At("finance"), true, 10, null), TestContext.Current.CancellationToken);
        var downloaded = await store.Execute(new DownloadCommand(At("finance/lease.txt"), null), TestContext.Current.CancellationToken);

        uploaded.IsSuccess.ShouldBeTrue();
        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Items.Select(info => info.Path.Value).ShouldBe(["finance/lease.txt"]);
        Text(downloaded.Value!).ShouldBe("seven years");
    }
}
