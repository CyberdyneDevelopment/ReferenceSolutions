using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that searches a file's rows: the <c>Find</c> data command. A file container is one
/// file whose rows are read whole, so the connection reads them and keeps the rows in which a searched field contains
/// the search term.
/// </summary>
public sealed class FileSystemFindCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemFindCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container whose rows are searched.</param>
    /// <param name="searchTerm">The text a searched field must contain.</param>
    /// <param name="fieldNames">The fields to search; none searches every field the container declares as text.</param>
    /// <param name="caseSensitive">Whether the match respects case.</param>
    /// <param name="maxResults">The most rows to answer; none answers every match.</param>
    public FileSystemFindCommand(
        string relativePath,
        IDataContainer container,
        string searchTerm,
        IReadOnlyList<string>? fieldNames,
        bool caseSensitive,
        int? maxResults)
        : base(relativePath, container)
    {
        SearchTerm = searchTerm;
        FieldNames = fieldNames;
        CaseSensitive = caseSensitive;
        MaxResults = maxResults;
    }

    /// <summary>Gets the text a searched field must contain.</summary>
    public string SearchTerm { get; }

    /// <summary>Gets the fields to search; none searches every field the container declares as text.</summary>
    public IReadOnlyList<string>? FieldNames { get; }

    /// <summary>Gets whether the match respects case.</summary>
    public bool CaseSensitive { get; }

    /// <summary>Gets the most rows to answer; none answers every match.</summary>
    public int? MaxResults { get; }
}
