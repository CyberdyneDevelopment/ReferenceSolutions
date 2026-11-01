using System.IO.Compression;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// FDW-1298: a container whose format is binary (Excel) is written through the same format-driven writer as any other format,
/// and reaches the file as bytes, not as text.
/// </summary>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemBinaryFormatTests
{
    private static readonly string[] Fields = ["Id", "Name"];

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task AnXlsxContainerIsWrittenAsAWorkbook()
    {
        var root = Directory.CreateTempSubdirectory("fdw-fs-xlsx-").FullName;
        try
        {
            var connection = new FileSystemConnection(new FileSystemConnectionConfiguration { Root = root }, logger: null);
            var container = ContainerStub.Build("people.xlsx", "Xlsx", Fields, new Dictionary<string, object>(), null);
            var rows = new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = 1, ["Name"] = "Ada" },
                new() { ["Id"] = 2, ["Name"] = "=1+1" },
            };

            var written = await connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                container,
                TestContext.Current.CancellationToken);

            written.IsSuccess.ShouldBeTrue(written.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "write failed");
            written.Value.ShouldBe(2);
            var bytes = await File.ReadAllBytesAsync(Path.Combine(root, "people.xlsx"), TestContext.Current.CancellationToken);
            bytes.Take(2).ShouldBe(new[] { (byte)'P', (byte)'K' });
            using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            archive.GetEntry("xl/worksheets/sheet1.xml").ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task AnXlsxContainerWrittenAsAWorkbookIsReadBackFromItsNamedWorksheet()
    {
        var root = Directory.CreateTempSubdirectory("fdw-fs-xlsx-read-").FullName;
        try
        {
            var connection = new FileSystemConnection(new FileSystemConnectionConfiguration { Root = root }, logger: null);
            // The writer always emits a header row, so a container that reads what it wrote says HasHeader.
            var metadata = new Dictionary<string, object> { ["HasHeader"] = true, ["SheetName"] = "Staff" };
            var container = ContainerStub.Build("people.xlsx", "Xlsx", Fields, metadata, null);
            var rows = new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = 1, ["Name"] = "Ada" },
                new() { ["Id"] = 2, ["Name"] = "=1+1" },
            };

            var written = await connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                container,
                TestContext.Current.CancellationToken);
            written.IsSuccess.ShouldBeTrue(written.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "write failed");

            var read = await connection.Execute<IEnumerable<IReadOnlyDictionary<string, object?>>>(
                CommandStub.Query(), container, TestContext.Current.CancellationToken);

            read.IsSuccess.ShouldBeTrue(read.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "read failed");
            var back = read.Value!.ToList();
            back.Count.ShouldBe(2);
            Convert.ToInt32(back[0]["Id"], System.Globalization.CultureInfo.InvariantCulture).ShouldBe(1);
            back[0]["Name"].ShouldBe("Ada");
            back[1]["Name"].ShouldBe("=1+1");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
