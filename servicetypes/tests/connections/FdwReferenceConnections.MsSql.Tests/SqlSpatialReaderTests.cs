using System.Data.SqlTypes;
using Fdw.Data.DataContainers.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// FDW-816: <see cref="SqlSpatialReader.ParseSinglePoint"/> against hand-built MS-SSCLRT byte
/// arrays — no live SQL Server connection required. Byte layout per the "Basic GEOGRAPHY
/// Structure" spec: SRID (4 bytes) + Version (1 byte) + SerializationProperties (1 byte), then
/// (when the single-point flag is set) two doubles with no NumPoints/Figures/Shapes fields.
/// </summary>
public sealed class SqlSpatialReaderTests
{
    private const byte FlagSinglePoint = 0x08;

    private static SqlBytes BuildSinglePoint(int srid, byte version, byte properties, double first, double second)
    {
        var bytes = new byte[6 + 16];
        BitConverter.GetBytes(srid).CopyTo(bytes, 0);
        bytes[4] = version;
        bytes[5] = properties;
        BitConverter.GetBytes(first).CopyTo(bytes, 6);
        BitConverter.GetBytes(second).CopyTo(bytes, 14);
        return new SqlBytes(bytes);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointGeographySwapsLatitudeLongitudeForWkt()
    {
        // GEOGRAPHY POINT stores (Latitude, Longitude); WKT is always "POINT (x y)" == longitude first.
        var bytes = BuildSinglePoint(4326, version: 1, FlagSinglePoint, first: 29.76, second: -95.37);

        var value = SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geography, NullLogger.Instance);

        value.Srid.ShouldBe(4326);
        value.Kind.ShouldBe(SpatialValue.Geography);
        value.Wkt.ShouldBe("POINT (-95.37 29.76)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointGeometryKeepsXyOrderForWkt()
    {
        // GEOMETRY POINT stores (X, Y) directly -- no swap.
        var bytes = BuildSinglePoint(2277, version: 2, FlagSinglePoint, first: 123.45, second: 678.9);

        var value = SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geometry, NullLogger.Instance);

        value.Srid.ShouldBe(2277);
        value.Kind.ShouldBe(SpatialValue.Geometry);
        value.Wkt.ShouldBe("POINT (123.45 678.9)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointFailsLoudWhenSinglePointFlagNotSet()
    {
        var bytes = BuildSinglePoint(4326, version: 1, properties: 0x00, first: 1, second: 2);

        Should.Throw<InvalidOperationException>(
            () => SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geography, NullLogger.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointFailsLoudOnUnsupportedVersion()
    {
        var bytes = BuildSinglePoint(4326, version: 3, FlagSinglePoint, first: 1, second: 2);

        Should.Throw<InvalidOperationException>(
            () => SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geography, NullLogger.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointFailsLoudOnNegativeSrid()
    {
        var bytes = BuildSinglePoint(-1, version: 1, FlagSinglePoint, first: 1, second: 2);

        Should.Throw<InvalidOperationException>(
            () => SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geography, NullLogger.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointFailsLoudOnTruncatedHeader()
    {
        var bytes = new SqlBytes(new byte[] { 1, 2, 3 });

        Should.Throw<InvalidOperationException>(
            () => SqlSpatialReader.ParseSinglePoint(bytes, SpatialValue.Geography, NullLogger.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseSinglePointFailsLoudOnTruncatedPointData()
    {
        // Valid 6-byte header, single-point flag set, but the two doubles are cut short.
        var bytes = new byte[6 + 8];
        BitConverter.GetBytes(4326).CopyTo(bytes, 0);
        bytes[4] = 1;
        bytes[5] = FlagSinglePoint;
        BitConverter.GetBytes(29.76).CopyTo(bytes, 6);

        Should.Throw<InvalidOperationException>(
            () => SqlSpatialReader.ParseSinglePoint(new SqlBytes(bytes), SpatialValue.Geography, NullLogger.Instance));
    }
}
