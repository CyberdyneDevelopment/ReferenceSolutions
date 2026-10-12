using Fdw.Services.Abstractions;

using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.FileSystem;

/// <summary>Supplies the FileSystemConnection implementation's own configuration.</summary>
public interface IFileSystemConnectionConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
{
}
