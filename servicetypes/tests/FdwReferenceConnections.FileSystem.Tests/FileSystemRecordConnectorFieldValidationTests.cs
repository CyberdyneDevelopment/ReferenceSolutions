using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Covers the <c>Fields()</c> failure path added when <see cref="FileSystemRecordConnector"/> stopped
/// throwing on a container child node that isn't an <see cref="IDataField"/> and started returning it as
/// a Failure instead (the connections/reference-servicetypes connector Result-discipline fix).
/// </summary>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemRecordConnectorFieldValidationTests
{
    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task WriteReturnsFailureWhenContainerChildIsNotADataField()
    {
        var root = Directory.CreateTempSubdirectory("fdw-fs-fieldvalidation-").FullName;
        try
        {
            var connection = new FileSystemConnection(
                new FileSystemConnectionConfiguration { Root = root },
                logger: null);

            const string fileName = "people.json";
            var container = BuildContainerWithNonFieldNode(fileName, "Json");
            var rows = new List<Dictionary<string, object?>> { new() { ["Id"] = "1" } };

            var writeResult = await connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                container,
                TestContext.Current.CancellationToken);

            writeResult.IsSuccess.ShouldBeFalse();
            writeResult.Messages.Select(m => m.ToString())
                .ShouldContain(m => m != null && m.Contains("is not an IDataField", StringComparison.Ordinal));
            File.Exists(Path.Combine(root, fileName)).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Why: a plain IDataNode mock (not also set up as IDataField) reproduces the "child node that
    // isn't a field" defect — ContainerStub.Build always builds valid IDataField children, so this
    // needs its own minimal container rather than reusing it.
    private static IDataContainer BuildContainerWithNonFieldNode(string fileName, string format)
    {
        // The family and the separator come from the real format of that name: the connector finds its reader and writer by
        // the family (FDW-1315), so a mock that stated only a name would be read by no reader.
        var realFormat = FormatTypes.ByName(format);
        var formatType = new Mock<IFormatType>();
        formatType.Setup(f => f.Name).Returns(format);
        formatType.Setup(f => f.Family).Returns(realFormat.Family);
        formatType.Setup(f => f.Separator).Returns(realFormat.Separator);

        var path = new Mock<IPath>();
        path.Setup(p => p.Domain).Returns("File");
        path.Setup(p => p.PathValue).Returns(fileName);

        var badNode = new Mock<IDataNode>();
        badNode.Setup(n => n.Name).Returns("NotAField");

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("TestContainer");
        container.Setup(c => c.Format).Returns(formatType.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        container.Setup(c => c.Path).Returns(path.Object);
        container.Setup(c => c.Nodes).Returns([badNode.Object]);
        return container.Object;
    }
}
