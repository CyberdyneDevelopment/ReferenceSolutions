using FdwReferenceConnections.FileSystem.Abstractions.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Tests;

public class FileSystemResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_AllFollowCatalogInvariants()
    {
        // Codes are categorized catalog numbers: Code == "FS-{number}", Id == EventId == number,
        // Domain == "FS" (the prefix the base ctor passes). Assert the invariants rather than
        // hardcoding the (renumber-prone) per-code numbers.
        foreach (var code in FileSystemResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"FS-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("FS");
        }
    }
}
