using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// The FileSystem connection's unit of work: writes land as they happen and every file they
/// overwrote goes back if the unit rolls back.
/// </summary>
/// <remarks>
/// Why this had to exist at all: the domain configuration providers write every record inside a unit
/// of work, so a connection that cannot open one cannot be written through its own provider — which
/// is what made <c>SaveThroughProviderPersistsDomainAndImplementationAndIsVisibleOnReread</c> fail
/// with "Connection 'PlatformConfiguration' does not support transactions".
/// </remarks>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemUnitOfWorkTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "fs-uow-" + Guid.NewGuid().ToString("N"));

    public FileSystemUnitOfWorkTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private FileSystemConnection Connection() =>
        new(new FileSystemConnectionConfiguration { Name = "Store", Root = _root }, NullLogger<FileSystemConnection>.Instance);

    private string Read(string relativePath) => File.ReadAllText(Path.Combine(_root, relativePath));

    private bool Exists(string relativePath) => File.Exists(Path.Combine(_root, relativePath));

    private void Seed(string relativePath, string text)
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AConnectionCanOpenAUnitOfWork()
    {
        var connection = Connection();

        var unit = await connection.BeginTransaction(TestContext.Current.CancellationToken);

        unit.IsSuccess.ShouldBeTrue(unit.CurrentMessage?.ToString());
        unit.Value!.IsActive.ShouldBeTrue();
        await unit.Value!.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ACommittedUnitLeavesItsWritesInPlace()
    {
        Seed("sec/Thing.json", "before");
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;

        await connection.Client.WriteText("sec/Thing.json", "after", TestContext.Current.CancellationToken);
        (await unit.Commit(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        Read("sec/Thing.json").ShouldBe("after");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ARolledBackUnitPutsTheOriginalContentBack()
    {
        Seed("sec/Thing.json", "before");
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;

        await connection.Client.WriteText("sec/Thing.json", "after", TestContext.Current.CancellationToken);
        Read("sec/Thing.json").ShouldBe("after", "writes apply as they happen, they are not buffered");

        (await unit.Rollback(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        Read("sec/Thing.json").ShouldBe("before");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ARolledBackUnitRemovesAFileThatDidNotExistBefore()
    {
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;

        await connection.Client.WriteText("sec/New.json", "created", TestContext.Current.CancellationToken);
        Exists("sec/New.json").ShouldBeTrue();

        await unit.Rollback(TestContext.Current.CancellationToken);

        // Undoing a create means removing it, not restoring empty content.
        Exists("sec/New.json").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RollbackUndoesEveryPathTheUnitTouchedNotJustTheLast()
    {
        Seed("sec/One.json", "one-before");
        Seed("sec/Two.json", "two-before");
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;

        await connection.Client.WriteText("sec/One.json", "one-after", TestContext.Current.CancellationToken);
        await connection.Client.WriteText("sec/Two.json", "two-after", TestContext.Current.CancellationToken);
        await unit.Rollback(TestContext.Current.CancellationToken);

        // A record is a row plus its children across several files; undoing only one of them is the
        // half-written record the unit exists to prevent.
        Read("sec/One.json").ShouldBe("one-before");
        Read("sec/Two.json").ShouldBe("two-before");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RepeatedWritesToOnePathUndoToWhatTheUnitFoundNotTheWriteBefore()
    {
        Seed("sec/Thing.json", "original");
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;

        await connection.Client.WriteText("sec/Thing.json", "first", TestContext.Current.CancellationToken);
        await connection.Client.WriteText("sec/Thing.json", "second", TestContext.Current.CancellationToken);
        await unit.Rollback(TestContext.Current.CancellationToken);

        // Capturing again on the second write would undo to "first" - a state that never existed
        // before the unit began.
        Read("sec/Thing.json").ShouldBe("original");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DisposingAUnitThatWasNeverCommittedUndoesIt()
    {
        Seed("sec/Thing.json", "before");
        var connection = Connection();

        await using (var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!)
        {
            await connection.Client.WriteText("sec/Thing.json", "after", TestContext.Current.CancellationToken);
        }

        // An early return or a thrown exception must not leave half a record behind.
        Read("sec/Thing.json").ShouldBe("before");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ASecondUnitIsRefusedWhileOneIsOpen()
    {
        var connection = Connection();
        var first = await connection.BeginTransaction(TestContext.Current.CancellationToken);
        first.IsSuccess.ShouldBeTrue();

        var second = await connection.BeginTransaction(TestContext.Current.CancellationToken);

        // Two units would interleave their writes and neither could be undone without undoing part
        // of the other, so the second is refused rather than silently replacing the first.
        second.IsSuccess.ShouldBeFalse();
        await first.Value!.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AUnitCanBeOpenedAgainOnceTheFirstHasEnded()
    {
        var connection = Connection();
        var first = await connection.BeginTransaction(TestContext.Current.CancellationToken);
        await first.Value!.Commit(TestContext.Current.CancellationToken);

        var second = await connection.BeginTransaction(TestContext.Current.CancellationToken);

        second.IsSuccess.ShouldBeTrue(second.CurrentMessage?.ToString());
        await second.Value!.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task CommittingTwiceFailsLoudRatherThanSilentlySucceeding()
    {
        var connection = Connection();
        var unit = (await connection.BeginTransaction(TestContext.Current.CancellationToken)).Value!;
        await unit.Commit(TestContext.Current.CancellationToken);

        (await unit.Commit(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
        unit.IsActive.ShouldBeFalse();
    }
}
