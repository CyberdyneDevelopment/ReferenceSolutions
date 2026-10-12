using System.Collections.Generic;
using System;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// Retires the current rows of a configuration container whose columns hold the given values: the
/// FileSystem form of <c>ConfigurationRetire</c>. The values are a row's Logical key, or the owner key
/// values its children hang from.
/// </summary>
public sealed class FileSystemConfigurationDeleteCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationDeleteCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="matchValues">The column=value pairs a retired row holds; never empty.</param>
    /// <param name="markDeleted">Whether the retired rows are marked deleted as well.</param>
    public FileSystemConfigurationDeleteCommand(
        string relativePath,
        IDataContainer container,
        IReadOnlyDictionary<string, object?> matchValues,
        bool markDeleted)
        : base(relativePath, container)
    {
        MatchValues = matchValues;
        MarkDeleted = markDeleted;
    }

    /// <summary>Gets the column=value pairs a retired row holds.</summary>
    public IReadOnlyDictionary<string, object?> MatchValues { get; }

    /// <summary>Gets whether the retired rows are marked deleted as well.</summary>
    public bool MarkDeleted { get; }
}
