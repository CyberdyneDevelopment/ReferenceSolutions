using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Tests for the reference application's retained documents (FDW-1575) over real files in a temporary folder: an organization's documents
/// are the files of the type's folder, dated by when they last changed, and ending one deletes it or moves it to the archive.
/// </summary>
public sealed class OrganizationStoreRetainedDocumentsTests : IDisposable
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private readonly string _root = Path.Combine(Path.GetTempPath(), "fdw-retained-" + Guid.NewGuid().ToString("N"));

    public OrganizationStoreRetainedDocumentsTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FolderStore(FileSystemDocumentStore inner) : IDocumentStore
    {
        public DocumentScope Scope { get; } = new(DocumentPath.Root);

        public IReadOnlyCollection<IDocumentCapability> Capabilities { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default)
            => inner.Execute(command, cancellationToken);
    }

    private OrganizationStoreRetainedDocuments Documents(bool noStore = false)
    {
        var stores = new Mock<IDocumentStores>();
        stores.Setup(s => s.For(Finance, "Documents", It.IsAny<CancellationToken>())).ReturnsAsync(noStore
            ? GenericResult<IDocumentStore>.Failure(new GenericMessage("the organization has no document store"))
            : GenericResult<IDocumentStore>.Success(new FolderStore(new FileSystemDocumentStore(Path.GetFullPath(_root), "LocalFs", NullLogger.Instance))));
        return new OrganizationStoreRetainedDocuments(stores.Object, NullLogger<OrganizationStoreRetainedDocuments>.Instance);
    }

    private string Write(string relative, DateTime modifiedUtc)
    {
        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "content");
        File.SetLastWriteTimeUtc(full, modifiedUtc);
        return full;
    }

    private static RetainedDocument Document(string id) => new(id, "Contracts", new Dictionary<string, DateTimeOffset>());

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheDocumentsOfATypeAreTheFilesOfItsFolderDatedByWhenTheyLastChanged()
    {
        Write("Contracts/2020/acme.pdf", new DateTime(2020, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        Write("Contracts/beta.pdf", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Write("Invoices/other.pdf", new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var listed = await Documents().List(Tenant, Finance, "Contracts", TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(document => document.DocumentId).OrderBy(id => id, StringComparer.Ordinal).ShouldBe(["Contracts/2020/acme.pdf", "Contracts/beta.pdf"]);
        listed.Value!.Single(document => document.DocumentId == "Contracts/2020/acme.pdf").Dates[OrganizationStoreRetainedDocuments.ModifiedDate]
            .ShouldBe(new DateTimeOffset(2020, 3, 15, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteRemovesTheFile()
    {
        var file = Write("Contracts/acme.pdf", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var ended = await Documents().End(Tenant, Finance, Document("Contracts/acme.pdf"), "Delete", TestContext.Current.CancellationToken);

        ended.IsSuccess.ShouldBeTrue();
        File.Exists(file).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ArchiveMovesTheFileBelowTheArchiveFolderAndOutOfItsTypesFolder()
    {
        var file = Write("Contracts/acme.pdf", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var ended = await Documents().End(Tenant, Finance, Document("Contracts/acme.pdf"), "Archive", TestContext.Current.CancellationToken);

        ended.IsSuccess.ShouldBeTrue();
        File.Exists(file).ShouldBeFalse();
        File.Exists(Path.Combine(_root, "Archive", "Contracts", "acme.pdf")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnEndActionThisApplicationDoesNotPerformFailsAndLeavesTheFile()
    {
        var file = Write("Contracts/acme.pdf", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var ended = await Documents().End(Tenant, Finance, Document("Contracts/acme.pdf"), "Shred", TestContext.Current.CancellationToken);

        ended.IsFailure.ShouldBeTrue();
        File.Exists(file).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADocumentIdThatClimbsOutOfTheStoreIsRefusedAndNothingIsTouched()
    {
        var outside = Path.Combine(Path.GetTempPath(), "fdw-outside-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(outside, "keep");
        try
        {
            var ended = await Documents().End(Tenant, Finance, Document("../" + Path.GetFileName(outside)), "Delete", TestContext.Current.CancellationToken);

            ended.IsFailure.ShouldBeTrue();
            File.Exists(outside).ShouldBeTrue();
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationWithNoDocumentStoreFailsLoudInsteadOfListingNothing()
    {
        var listed = await Documents(noStore: true).List(Tenant, Finance, "Contracts", TestContext.Current.CancellationToken);

        listed.IsFailure.ShouldBeTrue();
    }
}
