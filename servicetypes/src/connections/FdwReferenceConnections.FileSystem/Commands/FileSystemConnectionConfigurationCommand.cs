using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceConnections.FileSystem.Commands;

/// <summary>ConfigurationCommands TypeOption for the FileSystemConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "FileSystemConnectionConfiguration")]
public sealed class FileSystemConnectionConfigurationCommand : ConfigurationCommandBase<FileSystemConnectionConfiguration>
{
    /// <summary>Initializes the command with table name 'FileSystemConnection'.</summary>
    public FileSystemConnectionConfigurationCommand() : base("FileSystemConnectionConfiguration") { }
}
