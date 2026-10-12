using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// End-to-end round-trip tests for the FileSystem config-driven record read/write seam: write N records
/// to a temp file purely from a configured container (format + field schema, NO compile-time DTO), then
/// read them back through the same <see cref="FileSystemConnection.Execute{T}(IDataCommand, IDataContainer, System.Threading.CancellationToken)"/>
/// path and assert equality. Covers delimited, json, fixedwidth, and xml.
/// </summary>
/// <remarks>
/// The <c>RecordSourceTypes</c> / <c>RecordWriterTypes</c> TypeOptions use
/// <c>RestrictToCurrentCompilation = true</c>, so they are NOT auto-registered into the frozen collection
/// from a referencing assembly. These tests register the four format type instances once (the same
/// composition an entry-point app performs) so the connector's <c>ByName(format)</c> dispatch resolves.
/// </remarks>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemRecordRoundTripTests
{
    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task DelimitedWriteThenReadEqualsOriginal()
    {
        await RoundTrip(
            format: "Delimited",
            fileName: "people.csv",
            fields: ["Id", "Name", "City"],
            metadata: new Dictionary<string, object> { ["HasHeader"] = true, ["Separator"] = "," },
            rows:
            [
                new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
                new() { ["Id"] = "2", ["Name"] = "Bob, Jr", ["City"] = "LA" },
                new() { ["Id"] = "3", ["Name"] = "Carol \"C\"", ["City"] = "SF" },
            ]);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task JsonWriteThenReadEqualsOriginal()
    {
        await RoundTrip(
            format: "Json",
            fileName: "people.json",
            fields: ["Id", "Name", "City"],
            metadata: new Dictionary<string, object>(),
            rows:
            [
                new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
                new() { ["Id"] = "2", ["Name"] = "Bob", ["City"] = "LA" },
            ]);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task XmlWriteThenReadEqualsOriginal()
    {
        await RoundTrip(
            format: "Xml",
            fileName: "people.xml",
            fields: ["Id", "Name", "City"],
            metadata: new Dictionary<string, object> { ["RowElementName"] = "Row" },
            rows:
            [
                new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
                new() { ["Id"] = "2", ["Name"] = "Bob", ["City"] = "LA" },
            ]);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task FixedWidthWriteThenReadEqualsOriginal()
    {
        // Why: fixed-width offsets/widths come from per-field metadata (StartIndex/Length); the options
        // builder reads them off each field's Metadata bag — the field schema IS the layout.
        await RoundTrip(
            format: "FixedWidth",
            fileName: "people.txt",
            fields: ["Id", "Name", "City"],
            metadata: new Dictionary<string, object>(),
            rows:
            [
                new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
                new() { ["Id"] = "2", ["Name"] = "Bob", ["City"] = "LA" },
            ],
            fieldMetadata: new Dictionary<string, IReadOnlyDictionary<string, object>>
            {
                ["Id"] = new Dictionary<string, object> { ["StartIndex"] = 0, ["Length"] = 4 },
                ["Name"] = new Dictionary<string, object> { ["StartIndex"] = 4, ["Length"] = 10 },
                ["City"] = new Dictionary<string, object> { ["StartIndex"] = 14, ["Length"] = 6 },
            });
    }

    // ── Round-trip harness ────────────────────────────────────────────────────────

    private static async Task RoundTrip(
        string format,
        string fileName,
        string[] fields,
        IReadOnlyDictionary<string, object> metadata,
        List<Dictionary<string, object?>> rows,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>? fieldMetadata = null)
    {
        var root = Directory.CreateTempSubdirectory("fdw-fs-roundtrip-").FullName;
        try
        {
            var connection = new FileSystemConnection(
                new FileSystemConnectionConfiguration { Root = root },
                logger: null);

            var container = ContainerStub.Build(fileName, format, fields, metadata, fieldMetadata);

            // Act — write through the unified Execute seam (Insert command carries the rows)
            var writeResult = await connection.Execute<int>(
                CommandStub.Insert(rows.Cast<IReadOnlyDictionary<string, object?>>().ToList()),
                container,
                TestContext.Current.CancellationToken);

            writeResult.IsSuccess.ShouldBeTrue(
                writeResult.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "write failed");
            writeResult.Value.ShouldBe(rows.Count);
            File.Exists(Path.Combine(root, fileName)).ShouldBeTrue();

            // Act — read back through the unified Execute seam (Query command)
            var readResult = await connection.Execute<IEnumerable<IReadOnlyDictionary<string, object?>>>(
                CommandStub.Query(),
                container,
                TestContext.Current.CancellationToken);

            // Assert
            readResult.IsSuccess.ShouldBeTrue(
                readResult.Messages.Select(m => m.ToString()).FirstOrDefault() ?? "read failed");
            var readBack = readResult.Value!.ToList();
            readBack.Count.ShouldBe(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                foreach (var field in fields)
                {
                    // Why: every value round-trips through text serialization, so compare the string form
                    // (a delimited "1" reads back as "1"; a fixed-width value is trimmed back to its text).
                    var expected = rows[i][field];
                    var actual = readBack[i][field];
                    Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture)
                        .ShouldBe(Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
                            $"format '{format}' row {i} field '{field}'");
                }
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
