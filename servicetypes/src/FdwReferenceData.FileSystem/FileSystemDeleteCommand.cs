using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that removes rows from a file: the <c>Delete</c> data command. A file container is
/// one file whose rows are read whole and written whole, so the <c>FileSystemConfigurationWriter</c> drops the rows
/// the <see cref="Filter"/> selects (every row when the command has no filter, as on the SQL kinds) and rewrites the
/// file with the rest. When <see cref="SoftDelete"/> is set the writer flags the selected rows instead (the
/// deleted-flag column set to the options' value, the deleted-at column stamped) and keeps every row in the file.
/// </summary>
public sealed class FileSystemDeleteCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDeleteCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container whose rows are removed.</param>
    /// <param name="filter">The filter expression selecting the rows to remove; none removes every row.</param>
    /// <param name="softDelete">The soft-delete options that flag the selected rows instead of removing them; none removes them.</param>
    public FileSystemDeleteCommand(string relativePath, IDataContainer container, IFilterExpression? filter, SoftDeleteOptions? softDelete = null)
        : base(relativePath, container)
    {
        Filter = filter;
        SoftDelete = softDelete;
    }

    /// <summary>Gets the filter expression (WHERE clause) selecting the rows to remove; none selects every row.</summary>
    public IFilterExpression? Filter { get; }

    /// <summary>Gets the soft-delete options that flag the selected rows instead of removing them, or <see langword="null"/> to remove them.</summary>
    public SoftDeleteOptions? SoftDelete { get; }
}
