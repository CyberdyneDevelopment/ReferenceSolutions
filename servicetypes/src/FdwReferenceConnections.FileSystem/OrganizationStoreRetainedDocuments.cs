using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceConnections.FileSystem.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// The reference application's use of <see cref="IRetainedDocuments"/>: an organization's documents are the files in its document
/// store, one folder per document type (FDW-1575).
/// </summary>
/// <remarks>
/// <para>
/// This is an example of what a developer supplies, not framework behaviour. The folder per type, the <c>Documents</c> purpose, the single
/// date a file has (<see cref="ModifiedDate"/>, because a file keeps no other) and the two end actions <c>Delete</c> and <c>Archive</c>
/// are this application's choices. A developer whose documents live elsewhere, or carry a signed date, registers their own
/// <see cref="IRetainedDocuments"/> and the platform's policy, hold and job work unchanged.
/// </para>
/// <para>
/// The store is the one the organization is assigned for the purpose, narrowed to its folder, so the job reaches only that
/// organization's files. <c>Archive</c> moves a document to the same path below an <c>Archive</c> folder of the store, outside every type's folder.
/// </para>
/// </remarks>
public sealed class OrganizationStoreRetainedDocuments : IRetainedDocuments
{
    /// <summary>The name of the date a file has: when its content last changed.</summary>
    public const string ModifiedDate = "ModifiedDate";

    private const string Purpose = "Documents";
    private const string DeleteAction = "Delete";
    private const string ArchiveAction = "Archive";
    private const string ArchiveFolder = "Archive";
    private const int PageSize = 200;

    private readonly IDocumentStores _stores;
    private readonly ILogger<OrganizationStoreRetainedDocuments> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationStoreRetainedDocuments"/> class.</summary>
    /// <param name="stores">Finds the document store an organization is assigned.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationStoreRetainedDocuments(IDocumentStores stores, ILogger<OrganizationStoreRetainedDocuments>? logger)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        _logger = logger ?? NullLogger<OrganizationStoreRetainedDocuments>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<RetainedDocument>>> List(
        Guid tenantId, Guid organizationId, string documentType, CancellationToken cancellationToken = default)
    {
        var folder = DocumentPath.Parse(documentType);
        if (folder.IsFailure)
        {
            return GenericResult<IReadOnlyList<RetainedDocument>>.Failure(OrganizationStoreRetainedDocumentsLog.NotAPath(_logger, documentType, folder.CurrentMessage));
        }

        var store = await _stores.For(organizationId, Purpose, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } resolved)
        {
            return store.ToNewResult<IReadOnlyList<RetainedDocument>>();
        }

        var documents = new List<RetainedDocument>();
        string? continuation = null;
        do
        {
            var page = await resolved.Execute(new ListCommand(folder.Value, true, PageSize, continuation), cancellationToken).ConfigureAwait(false);
            if (page.IsFailure || page.Value is not { } listed)
            {
                return page.ToNewResult<IReadOnlyList<RetainedDocument>>();
            }

            foreach (var info in listed.Items)
            {
                documents.Add(new RetainedDocument(info.Path.Value, documentType, new Dictionary<string, DateTimeOffset> { [ModifiedDate] = info.LastModified }));
            }

            continuation = listed.Continuation;
        }
        while (continuation is not null);

        return GenericResult<IReadOnlyList<RetainedDocument>>.Success(documents);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> End(
        Guid tenantId, Guid organizationId, RetainedDocument document, string endAction, CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var path = DocumentPath.Parse(document.DocumentId);
        if (path.IsFailure)
        {
            return GenericResult.Failure(OrganizationStoreRetainedDocumentsLog.NotAPath(_logger, document.DocumentId, path.CurrentMessage));
        }

        var store = await _stores.For(organizationId, Purpose, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } resolved)
        {
            return store;
        }

        IGenericResult ended;
        if (string.Equals(endAction, DeleteAction, StringComparison.OrdinalIgnoreCase))
        {
            ended = await resolved.Execute(new DeleteCommand(path.Value), cancellationToken).ConfigureAwait(false);
        }
        else if (string.Equals(endAction, ArchiveAction, StringComparison.OrdinalIgnoreCase))
        {
            ended = await resolved.Execute(
                new MoveCommand(path.Value, DocumentPath.Parse(ArchiveFolder).Value.Combine(path.Value), false), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return GenericResult.Failure(OrganizationStoreRetainedDocumentsLog.EndActionUnknown(_logger, endAction));
        }

        if (ended.IsSuccess)
        {
            OrganizationStoreRetainedDocumentsLog.DocumentEnded(_logger, document.DocumentId, endAction);
        }

        return ended;
    }
}
