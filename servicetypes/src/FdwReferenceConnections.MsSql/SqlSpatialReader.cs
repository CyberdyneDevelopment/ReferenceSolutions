using System;
using System.Data.SqlTypes;
using System.Globalization;
using Fdw.Data.DataContainers.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using FdwReferenceConnections.MsSql.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Reads a SqlDataReader column, translating a geography/geometry UDT into a
/// <see cref="SpatialValue"/> (FDW-816) instead of the bytes <see cref="SqlDataReader.GetValue(int)"/>
/// returns for a CLR UDT type nothing above the connection layer references.
/// </summary>
internal static class SqlSpatialReader
{
    private const string GeographyTypeName = "geography";
    private const string GeometryTypeName = "geometry";

    // MS-SSCLRT "Basic GEOGRAPHY Structure" header: SRID (4 bytes, LE int32) + Version (1 byte) +
    // SerializationProperties (1 byte) -- identical layout for GEOGRAPHY and GEOMETRY.
    private const int HeaderLength = 6;
    private const int PointLength = 16; // two 8-byte doubles

    private const byte FlagSinglePoint = 0x08;

    /// <summary>
    /// Reads column <paramref name="ordinal"/>. A geography/geometry column becomes a
    /// <see cref="SpatialValue"/>; every other column type reads exactly as
    /// <c>reader.IsDBNull(i) ? null : reader.GetValue(i)</c> always has.
    /// </summary>
    public static object? ReadValue(SqlDataReader reader, int ordinal, ILogger logger)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var dbTypeName = reader.GetDataTypeName(ordinal);
        if (string.Equals(dbTypeName, GeographyTypeName, StringComparison.OrdinalIgnoreCase))
            return ParseSinglePoint(reader.GetSqlBytes(ordinal), SpatialValue.Geography, logger);
        if (string.Equals(dbTypeName, GeometryTypeName, StringComparison.OrdinalIgnoreCase))
            return ParseSinglePoint(reader.GetSqlBytes(ordinal), SpatialValue.Geometry, logger);

        return reader.GetValue(ordinal);
    }

    /// <summary>
    /// The CLR type <see cref="ReadValue"/> produces for column <paramref name="ordinal"/> --
    /// <see cref="SpatialValue"/> for a geography/geometry column, <c>reader.GetFieldType(ordinal)</c>
    /// otherwise. For ad-hoc result sets (e.g. a data-preview query) whose schema is built from the
    /// reader itself, this keeps the declared field type matching what rows actually contain.
    /// </summary>
    public static Type GetFieldClrType(SqlDataReader reader, int ordinal)
    {
        var dbTypeName = reader.GetDataTypeName(ordinal);
        return string.Equals(dbTypeName, GeographyTypeName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbTypeName, GeometryTypeName, StringComparison.OrdinalIgnoreCase)
                ? typeof(SpatialValue)
                : reader.GetFieldType(ordinal);
    }

    // Parses the MS-SSCLRT "IsSinglePoint" fast path only -- the shape a map-marker data set
    // actually carries. Every other shape (single line segment, or the general multi-point/figure/
    // shape structure) fails loud rather than guessing at a partial WKT: SQL Server's own
    // STAsText()/STSrid handles every shape correctly, and projecting those server-side is the
    // natural way to extend this if a data set ever needs more than points, rather than growing
    // this parser into a client-side reimplementation of SQL Server's spatial engine.
    // Why internal, not private: FdwReferenceConnections.MsSql.Tests (InternalsVisibleTo) constructs
    // raw MS-SSCLRT byte arrays directly to test the parser without a live SQL Server connection.
    internal static SpatialValue ParseSinglePoint(SqlBytes bytes, string kind, ILogger logger)
    {
        var raw = bytes.Value;

        if (raw.Length < HeaderLength)
        {
            var message = MsSqlSpatialLogger.SpatialValueTooShort(logger, kind, raw.Length);
            throw new InvalidOperationException(message.Message);
        }

        var srid = BitConverter.ToInt32(raw, 0);
        var version = raw[4];
        var properties = raw[5];

        if (version is not (1 or 2))
        {
            var message = MsSqlSpatialLogger.SpatialVersionUnsupported(logger, kind, version);
            throw new InvalidOperationException(message.Message);
        }

        // -1 is the MS-SSCLRT null-geography sentinel -- unexpected here since IsDBNull already
        // filtered a genuinely NULL reader value.
        if (srid < 0)
        {
            var message = MsSqlSpatialLogger.SpatialSridInvalid(logger, kind, srid);
            throw new InvalidOperationException(message.Message);
        }

        if ((properties & FlagSinglePoint) == 0)
        {
            var message = MsSqlSpatialLogger.SpatialShapeUnsupported(logger, kind, srid, properties);
            throw new InvalidOperationException(message.Message);
        }

        if (raw.Length < HeaderLength + PointLength)
        {
            var message = MsSqlSpatialLogger.SpatialValueTooShort(logger, kind, raw.Length);
            throw new InvalidOperationException(message.Message);
        }

        var first = BitConverter.ToDouble(raw, HeaderLength);
        var second = BitConverter.ToDouble(raw, HeaderLength + 8);

        // GEOGRAPHY POINT stores (Latitude, Longitude); GEOMETRY POINT stores (X, Y). OGC WKT is
        // always "POINT (x y)" -- for geography that means longitude first, matching STAsText().
        var (x, y) = kind == SpatialValue.Geography ? (second, first) : (first, second);
        var wkt = string.Create(CultureInfo.InvariantCulture, $"POINT ({x} {y})");

        MsSqlSpatialLogger.SpatialValueParsed(logger, kind, srid);
        return new SpatialValue(wkt, srid, kind);
    }
}
