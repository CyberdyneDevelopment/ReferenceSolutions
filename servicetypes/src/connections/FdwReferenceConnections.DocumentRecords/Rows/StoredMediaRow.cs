using System;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.StoredMedia (FDW-3731).</summary>
internal sealed class StoredMediaRow : IStoredMedia
{
    /// <summary>Initializes a new instance of the <see cref="StoredMediaRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="documentId">The DocumentId.</param>
    /// <param name="fileType">The FileType.</param>
    /// <param name="length">The Length.</param>
    /// <param name="contentHash">The ContentHash.</param>
    /// <param name="purpose">The Purpose.</param>
    /// <param name="path">The Path.</param>
    /// <param name="storedUnder">The StoredUnder.</param>
    internal StoredMediaRow(Guid id, Guid documentId, string fileType, long length, string contentHash, string purpose, DocumentPath path, Guid storedUnder)
    {
        Id = id;
        DocumentId = documentId;
        FileType = fileType;
        Length = length;
        ContentHash = contentHash;
        Purpose = purpose;
        Path = path;
        StoredUnder = storedUnder;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid DocumentId { get; }

    /// <inheritdoc/>
    public string FileType { get; }

    /// <inheritdoc/>
    public long Length { get; }

    /// <inheritdoc/>
    public string ContentHash { get; }

    /// <inheritdoc/>
    public string Purpose { get; }

    /// <inheritdoc/>
    public DocumentPath Path { get; }

    /// <inheritdoc/>
    public Guid StoredUnder { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row, or the refusal of a stored path that is not a document path.</returns>
    internal static IGenericResult<IStoredMedia> From(SqlDataReader reader)
    {
        var path = DocumentPath.Parse(reader.GetString(reader.GetOrdinal("Path")));
        if (path.IsFailure)
        {
            return path.ToNewResult<IStoredMedia>();
        }

        return GenericResult<IStoredMedia>.Success(new StoredMediaRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("DocumentId")),
            reader.GetString(reader.GetOrdinal("FileType")),
            reader.GetInt64(reader.GetOrdinal("Length")),
            reader.GetString(reader.GetOrdinal("ContentHash")),
            reader.GetString(reader.GetOrdinal("Purpose")),
            path.Value,
            reader.GetGuid(reader.GetOrdinal("StoredUnder"))));
    }
}
