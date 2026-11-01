using FdwReferenceConnections.FileSystem.Registration;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// FDW-1068: a file-system connection carries no credential of its own; it reads and writes under its root with the
/// host process's own access, so every such connection acts as the host and only a platform administrator may save one.
/// </summary>
public class FileSystemHostIdentityTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EveryFileSystemConnectionActsAsTheHost()
    {
        var answer = new FileSystemConnectionType().UsesHostIdentity(new FileSystemConnectionConfiguration { Name = "LocalFs" });

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeTrue();
    }
}
