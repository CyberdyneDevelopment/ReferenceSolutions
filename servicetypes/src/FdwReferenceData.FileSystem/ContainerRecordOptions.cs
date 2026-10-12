using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.FixedWidth.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// Builds the format-specific <see cref="RowSourceOptions"/> / <see cref="RowWriterOptions"/> for a
/// configured container DYNAMICALLY from its <see cref="IStorageContainer.Metadata"/> and field schema —
/// the config-driven format surface. The container's <see cref="IStorageContainer.Format"/> name selects
/// which option shape to build; the container's fields supply the column order (delimited) and the
/// per-field offsets/widths (fixed-width). There is no per-format container class and no compile-time DTO.
/// </summary>
/// <remarks>
/// Why this lives here (and mirrors the read-side logic in <c>RestProtocolBase.BuildRowSourceOptions</c>):
/// the only package that builds concrete delimited/fixed-width/json/xml options from a container is the
/// HTTP connection, and that read-only logic is private to it. The FileSystem connection needs the same
/// read-side mapping plus the WRITE-side mapping (which exists nowhere yet). Both are built here from the
/// container so the FileSystem read/write seam stays config-driven. The format name is matched in exactly
/// one place per direction — only to map config onto the matching library options, never to pick a
/// reader/writer type (that is <c>RecordSourceTypes.ByName(format)</c> / <c>RecordWriterTypes.ByName(format)</c>).
/// A later stage should hoist this shared builder once a non-cyclic home (a package that references all
/// four format <c>*.Abstractions</c> packages) exists, and have <c>RestProtocolBase</c> consume it too.
/// </remarks>
public static class ContainerRecordOptions
{
    /// <summary>
    /// Builds the read-side options for the container's configured format.
    /// </summary>
    /// <param name="container">The configured container (format name + metadata + field schema).</param>
    /// <returns>
    /// The format-specific <see cref="RowSourceOptions"/>, or null when the format is registered elsewhere
    /// and should use its own defaults.
    /// </returns>
    public static RowSourceOptions? BuildSourceOptions(IStorageContainer container)
        => container.Format.Name switch
        {
            "Json" => BuildJsonSourceOptions(container.Metadata),
            "Xml" => BuildXmlSourceOptions(container.Metadata),
            "Delimited" => BuildDelimitedSourceOptions(container),
            "FixedWidth" => BuildFixedWidthSourceOptions(container),
            _ => null
        };

    /// <summary>
    /// Builds the write-side options for the container's configured format.
    /// </summary>
    /// <param name="container">The configured container (format name + metadata + field schema).</param>
    /// <returns>
    /// The format-specific <see cref="RowWriterOptions"/>, or null when the format is registered elsewhere
    /// and should use its own defaults.
    /// </returns>
    public static RowWriterOptions? BuildWriterOptions(IStorageContainer container)
        => container.Format.Name switch
        {
            "Json" => BuildJsonWriterOptions(container.Metadata),
            "Xml" => BuildXmlWriterOptions(container.Metadata),
            "Delimited" => BuildDelimitedWriterOptions(container),
            "FixedWidth" => BuildFixedWidthWriterOptions(container),
            _ => null
        };

    // ── Read side ───────────────────────────────────────────────────────────────

    private static JsonRowSourceOptions BuildJsonSourceOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            RowArrayPath = meta.TryGetValue("RecordSelector", out var sel) ? sel as string : null,
            FlattenNestedObjects = meta.TryGetValue("FlattenNestedObjects", out var fn) && fn is bool fb && fb,
            FlattenSeparator = meta.TryGetValue("FlattenSeparator", out var fs) && fs is string s && !string.IsNullOrEmpty(s) ? s : "."
        };

    private static XmlRowSourceOptions BuildXmlSourceOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            RowElementName = meta.TryGetValue("RowElementName", out var rn) ? rn as string : null,
            RowElementPath = meta.TryGetValue("RecordSelector", out var rp) ? rp as string : null
        };

    private static DelimitedRowSourceOptions BuildDelimitedSourceOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new DelimitedRowSourceOptions
        {
            HasHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb,
            Separator = meta.TryGetValue("Separator", out var sep) && sep is string ss && !string.IsNullOrEmpty(ss) ? ss : ","
        };
        options.Columns = new List<string>(FieldNames(container));
        return options;
    }

    private static FixedWidthRowSourceOptions BuildFixedWidthSourceOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new FixedWidthRowSourceOptions
        {
            HasHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb
        };
        // Why: fixed-width offsets/widths come from per-field metadata; when absent the reader fails loud
        // (empty Fields → ArgumentException) rather than guessing widths (NO FALLBACKS).
        options.Fields = new List<FixedWidthField>(FixedWidthFields(container));
        return options;
    }

    // ── Write side ──────────────────────────────────────────────────────────────

    private static JsonRowWriterOptions BuildJsonWriterOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            WriteIndented = meta.TryGetValue("WriteIndented", out var wi) && wi is bool wb && wb
        };

    private static XmlRowWriterOptions BuildXmlWriterOptions(IReadOnlyDictionary<string, object> meta)
    {
        var options = new XmlRowWriterOptions();
        if (meta.TryGetValue("RowElementName", out var rn) && rn is string rns && !string.IsNullOrEmpty(rns))
            options.RowElementName = rns;
        if (meta.TryGetValue("RootElementName", out var root) && root is string roots && !string.IsNullOrEmpty(roots))
            options.RootElementName = roots;
        return options;
    }

    private static DelimitedRowWriterOptions BuildDelimitedWriterOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new DelimitedRowWriterOptions
        {
            WriteHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb,
            Separator = meta.TryGetValue("Separator", out var sep) && sep is string ss && !string.IsNullOrEmpty(ss) ? ss : ","
        };
        // Why: column order is the container field order — a delimited file with a header MUST emit
        // columns in a stable order, supplied from the schema, never inferred.
        options.Columns = new List<string>(FieldNames(container));
        return options;
    }

    private static FixedWidthRowWriterOptions BuildFixedWidthWriterOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new FixedWidthRowWriterOptions
        {
            WriteHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb
        };
        options.Fields = new List<FixedWidthField>(FixedWidthFields(container));
        return options;
    }

    // ── Field projection ─────────────────────────────────────────────────────────

    private static IEnumerable<string> FieldNames(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields) yield return field.Name;
    }

    private static IEnumerable<FixedWidthField> FixedWidthFields(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields)
        {
            var fieldMeta = field.Metadata;
            if (fieldMeta is null
                || !fieldMeta.TryGetValue("StartIndex", out var startObj)
                || !fieldMeta.TryGetValue("Length", out var lenObj))
            {
                continue;
            }

            yield return new FixedWidthField
            {
                Name = field.Name,
                StartIndex = Convert.ToInt32(startObj, CultureInfo.InvariantCulture),
                Length = Convert.ToInt32(lenObj, CultureInfo.InvariantCulture)
            };
        }
    }
}
