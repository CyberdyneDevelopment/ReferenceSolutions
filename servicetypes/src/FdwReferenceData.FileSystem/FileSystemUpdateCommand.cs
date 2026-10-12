using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that mutates existing rows IN PLACE (the literal, non-versioning
/// <c>Update</c> the config <c>UpdateCommand</c> emits). Carries the record POCO whose mapped values
/// replace the matched rows' non-key columns, plus the <see cref="Filter"/> that identifies which rows
/// to mutate. The <c>FileSystemConfigurationWriter</c> keeps each matched row's physical RowId and
/// version flags untouched and rewrites the whole file.
/// </summary>
public sealed class FileSystemUpdateCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemUpdateCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="record">The record POCO whose mapped values replace the matched rows' columns.</param>
    /// <param name="filter">The filter expression identifying which rows to mutate.</param>
    public FileSystemUpdateCommand(
        string relativePath,
        IDataContainer container,
        object record,
        IFilterExpression? filter)
        : base(relativePath, container)
    {
        Record = record;
        Filter = filter;
    }

    /// <summary>Gets the record POCO whose mapped values replace the matched rows' non-key columns.</summary>
    public object Record { get; }

    /// <summary>Gets the filter expression (WHERE clause) identifying which rows to mutate.</summary>
    public IFilterExpression? Filter { get; }
}
