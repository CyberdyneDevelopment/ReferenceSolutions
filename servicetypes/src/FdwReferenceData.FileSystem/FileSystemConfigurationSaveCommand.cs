using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that persists a NEW logical version of a configuration record
/// (the version-on-write CREATE the config <c>ConfigurationSave</c> command emits). Carries the record
/// POCO the connection maps to columns, plus the <see cref="FieldValues"/> written with it (the owner's
/// key values). The <c>FileSystemConfigurationWriter</c> reads the container's current rows, assigns a
/// new physical RowId, sets <c>IsCurrent=true</c>/<c>IsDeleted=false</c> explicitly, retires the current
/// row it replaces when <see cref="ReplacesCurrent"/> says so, then rewrites the whole file.
/// </summary>
public sealed class FileSystemConfigurationSaveCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationSaveCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="record">The configuration record POCO to persist.</param>
    /// <param name="fieldValues">
    /// Column=value pairs written with the record, beyond its mapped columns: the owner's key values,
    /// filled into the Foreign key the container declares to its owner.
    /// </param>
    /// <param name="replacesCurrent">Whether the row replaces the current row it matches, which it retires.</param>
    public FileSystemConfigurationSaveCommand(
        string relativePath,
        IDataContainer container,
        object record,
        IReadOnlyDictionary<string, object?> fieldValues,
        bool replacesCurrent)
        : base(relativePath, container)
    {
        Record = record;
        FieldValues = fieldValues;
        ReplacesCurrent = replacesCurrent;
    }

    /// <summary>Gets the configuration record POCO to persist.</summary>
    public object Record { get; }

    /// <summary>Gets the column=value pairs written with the record beyond its mapped columns.</summary>
    public IReadOnlyDictionary<string, object?> FieldValues { get; }

    /// <summary>Gets whether the row replaces the current row it matches.</summary>
    public bool ReplacesCurrent { get; }
}
