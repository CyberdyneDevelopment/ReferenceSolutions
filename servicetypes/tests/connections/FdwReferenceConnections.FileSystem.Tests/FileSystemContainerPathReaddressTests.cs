using Fdw.Data.Abstractions;
using FdwReferenceData.FileSystem;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// A FileSystem container's path made again at another directory or file name (FDW-3089): a pipeline reads a list of files through one declared
/// container, each file at its own name.
/// </summary>
public sealed class FileSystemContainerPathReaddressTests
{
    [Fact]
    [Trait("Category", "Readdress")]
    public void ThePathIsReaddressable() =>
        new FileSystemContainerPath("walk/Rows.csv").ShouldBeAssignableTo<IReaddressablePath>();

    [Theory]
    [Trait("Category", "Readdress")]
    [InlineData("walk/Rows.csv", null, "a.csv", "walk/a.csv")]
    [InlineData("walk/Rows.csv", "", " ", "walk/Rows.csv")]
    [InlineData("walk/Rows.csv", "other", null, "other/Rows.csv")]
    [InlineData("walk/Rows.csv", "other/", "b.csv", "other/b.csv")]
    [InlineData("walk/sub/Rows.csv", null, "a.csv", "walk/sub/a.csv")]
    [InlineData("Rows.csv", null, "a.csv", "a.csv")]
    [InlineData("Rows.csv", "in", "a.csv", "in/a.csv")]
    public void TheStatedPartsReplaceTheDirectoryAndTheFileNameAndABlankPartKeepsWhatThePathHas(string path, string? pathPart, string? namePart, string expected)
    {
        var moved = new FileSystemContainerPath(path).Readdress(pathPart, namePart);

        moved.ShouldBeOfType<FileSystemContainerPath>();
        moved.PathValue.ShouldBe(expected);
        moved.Domain.ShouldBe("File");
    }

    [Theory]
    [Trait("Category", "Readdress")]
    [InlineData("walk/Rows.csv", "Rows.csv", ".csv")]
    [InlineData("Rows.csv", "Rows.csv", ".csv")]
    [InlineData("walk/sub/Players.tar.gz", "Players.tar.gz", ".gz")]
    [InlineData("walk/Rows", "Rows", "")]
    public void TheFileNameIsTheLastSegmentAndTheExtensionIsItsOwn(string path, string fileName, string extension)
    {
        var file = new FileSystemContainerPath(path);

        file.ShouldBeAssignableTo<IFilePath>();
        file.FileName.ShouldBe(fileName);
        file.Extension.ShouldBe(extension);
    }

    [Fact]
    [Trait("Category", "Readdress")]
    public void TheOriginalPathIsNotChanged()
    {
        var original = new FileSystemContainerPath("walk/Rows.csv");

        _ = original.Readdress("other", "a.csv");

        original.PathValue.ShouldBe("walk/Rows.csv");
    }
}
