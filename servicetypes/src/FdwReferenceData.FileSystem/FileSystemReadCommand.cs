using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that reads records from a configured file container through the
/// config-driven record source factory. Carries the source <c>QueryCommand</c>'s filter/join,
/// copied through unchanged, so the connection can apply them over the decoded rows via the shared
/// <c>RecordQueryEvaluator</c>.
/// </summary>
public sealed class FileSystemReadCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemReadCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to read.</param>
    /// <param name="filter">The filter expression (WHERE clause), copied through unchanged from the source command.</param>
    /// <param name="joins">The join expressions, copied through unchanged from the source command.</param>
    /// <param name="ordering">The ordering to apply over the decoded rows, or null.</param>
    /// <param name="paging">The page to apply over the decoded rows, or null.</param>
    public FileSystemReadCommand(
        string relativePath,
        IDataContainer container,
        IFilterExpression? filter,
        IReadOnlyList<IJoinExpression> joins,
        IOrderingExpression? ordering = null,
        IPagingExpression? paging = null)
        : base(relativePath, container)
    {
        Filter = filter;
        Joins = joins;
        Ordering = ordering;
        Paging = paging;
    }

    /// <summary>Gets the filter expression (WHERE clause), copied through unchanged from the source command.</summary>
    public IFilterExpression? Filter { get; }

    /// <summary>Gets the join expressions, copied through unchanged from the source command.</summary>
    public IReadOnlyList<IJoinExpression> Joins { get; }

    /// <summary>The ordering to apply, or null. A file cannot express ORDER BY, so it is carried
    /// here for the connection to apply over the decoded rows.</summary>
    public IOrderingExpression? Ordering { get; }

    /// <summary>The page to apply, or null. Carried for the same reason as <see cref="Ordering"/>.</summary>
    public IPagingExpression? Paging { get; }
}
