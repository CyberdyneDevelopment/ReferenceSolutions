using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem.Registration;

/// <summary>The FileSystemConnection implementation of its domain.</summary>
public interface IFileSystemConnectionProvider : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
