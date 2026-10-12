using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Delimited;
using Fdw.Data.RowSources.FixedWidth;
using Fdw.Data.RowSources.Json;
using Fdw.Data.RowSources.Xml;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Registers the Json/Xml/Delimited/FixedWidth record source + writer TypeOptions into the
/// <see cref="RecordSourceTypes"/> / <see cref="RecordWriterTypes"/> collections before any test reads
/// them, so the connector's <c>ByName(format)</c> dispatch resolves.
/// </summary>
/// <remarks>
/// Why manual registration: the format TypeOptions use <c>RestrictToCurrentCompilation = true</c>, so
/// they are NOT auto-registered into a referencing assembly's frozen collection (the same convention the
/// <c>FormatDrivenResolutionTests</c> / <c>RowSourceTypesTests</c> rely on). <see cref="RegisterMember"/>
/// is idempotent and must run before first access; constructing this fixture (a collection fixture) runs
/// before the round-trip tests, and the registration is a no-op if it has already happened.
/// </remarks>
public sealed class RecordFormatRegistrationFixture
{
    public RecordFormatRegistrationFixture()
    {
        RecordSourceTypes.RegisterMember(new JsonRowSourceType());
        RecordSourceTypes.RegisterMember(new XmlRowSourceType());
        RecordSourceTypes.RegisterMember(new DelimitedRowSourceType());
        RecordSourceTypes.RegisterMember(new FixedWidthRowSourceType());

        RecordWriterTypes.RegisterMember(new JsonRowWriterType());
        RecordWriterTypes.RegisterMember(new XmlRowWriterType());
        RecordWriterTypes.RegisterMember(new DelimitedRowWriterType());
        RecordWriterTypes.RegisterMember(new FixedWidthRowWriterType());
    }
}

[CollectionDefinition("FileSystemRecordFormats")]
public sealed class FileSystemRecordFormatsCollection : ICollectionFixture<RecordFormatRegistrationFixture>
{
}
