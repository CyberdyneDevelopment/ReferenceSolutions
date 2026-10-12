using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql.Logging;

/// <summary>
/// MessageLogging for reading geography/geometry UDT columns into <see cref="Fdw.Data.DataContainers.Abstractions.SpatialValue"/> (FDW-816).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlSpatialLogger
{
    /// <summary>Traces a successful geography/geometry column read.</summary>
    [MessageLogging(EventId = 12010, Level = LogLevel.Debug, Message = "Parsed {kind} column value (SRID {srid})")]
    public static partial IGenericMessage SpatialValueParsed(ILogger logger, string kind, int srid);

    /// <summary>Logs when a geography/geometry column's serialized bytes are too short to hold the declared structure.</summary>
    [MessageLogging(EventId = 22010, Level = LogLevel.Error, Message = "{kind} column value is {byteLength} byte(s), too short to be a valid serialized value")]
    public static partial IGenericMessage SpatialValueTooShort(ILogger logger, string kind, int byteLength);

    /// <summary>Logs when a geography/geometry column's serialization version byte is not one this reader understands.</summary>
    [MessageLogging(EventId = 22011, Level = LogLevel.Error, Message = "{kind} column value uses serialization version {version}, which this reader does not understand (supports 1, 2)")]
    public static partial IGenericMessage SpatialVersionUnsupported(ILogger logger, string kind, byte version);

    /// <summary>Logs when a geography/geometry column carries the null-geography SRID sentinel despite the reader value not being NULL.</summary>
    [MessageLogging(EventId = 22012, Level = LogLevel.Error, Message = "{kind} column value has SRID {srid}, the null-geography sentinel, on a non-NULL reader value")]
    public static partial IGenericMessage SpatialSridInvalid(ILogger logger, string kind, int srid);

    /// <summary>Logs when a geography/geometry column is not the single-point shape this reader currently parses.</summary>
    [MessageLogging(EventId = 22013, Level = LogLevel.Error, Message = "{kind} column value (SRID {srid}) is not a single point (serialization properties 0x{properties:X2}) -- only single-point {kind} values are currently readable; use STAsText()/STSrid in the query for other shapes")]
    public static partial IGenericMessage SpatialShapeUnsupported(ILogger logger, string kind, int srid, byte properties);
}
