using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;
using FdwReferenceConnections.FileSystem;

namespace FdwReferenceConnections.FileSystem.Validation;

/// <summary>States that <see cref="FileSystemConnectionConfiguration"/> is checked by <see cref="FileSystemConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "FileSystemConnectionConfiguration")]
public sealed class FileSystemConnectionConfigurationValidation : ValidatedConfiguration<FileSystemConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FileSystemConnectionConfigurationValidation"/> class.</summary>
    public FileSystemConnectionConfigurationValidation()
        : base("FileSystemConnectionConfiguration", new FileSystemConnectionConfigurationValidator())
    {
    }
}
