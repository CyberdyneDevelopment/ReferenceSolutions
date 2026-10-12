using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataStores.SqlServer;
using Microsoft.Extensions.Logging;

namespace Fdw.SchemaDump;

/// <summary>
/// Connects to a database and writes out the schema it finds.
/// </summary>
/// <remarks>
/// The output is a DataStoreConfiguration — the same shape configurationSchema.json declares and
/// the same shape the importer hands the rest of the stack. That is the point of the tool: it does
/// not invent a reporting format, so what it prints can be pasted into a schema file or diffed
/// against one.
/// </remarks>
public static class Program
{
    // Why a static instance: JsonSerializerOptions caches its own reflection metadata, so building
    // one per call re-does that work — which is what CA1869 is pointing at. One dump, one writer.
    private static readonly JsonSerializerOptions SchemaJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Entry point.</summary>
    /// <param name="args">Connection string, then optionally an output path.</param>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.Error.WriteLine("""
                fdw-schema — read a live database's schema and print it as a DataStoreConfiguration.

                  fdw-schema "<connection-string>" [--out <file>] [--quiet]

                  --out <file>   write to a file instead of stdout
                  --quiet        suppress the importer's own log lines on stderr

                The connection string is passed through untouched, so anything SqlConnection accepts
                works. Nothing is written to the database; the importer only reads catalogue views.
                """);
            return args.Length == 0 ? 2 : 0;
        }

        string connectionString = args[0];
        string? outPath = null;
        bool quiet = false;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outPath = args[++i]; continue; }
            if (args[i] == "--quiet") { quiet = true; continue; }
            Console.Error.WriteLine($"unrecognised argument: {args[i]}");
            return 2;
        }

        // Why a real logger rather than NullLogger: the importer reports what it skipped and why,
        // and a schema dump that silently omitted a table would be worse than one that failed.
        using var loggerFactory = LoggerFactory.Create(b => b
            .AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)
            .SetMinimumLevel(quiet ? LogLevel.Warning : LogLevel.Information));

        var importer = new SqlServerSchemaImporter(loggerFactory.CreateLogger<SqlServerSchemaImporter>());

        // Validate first so an unreachable server is reported as that, rather than as an empty schema.
        var reachable = await importer.Validate(connectionString, CancellationToken.None).ConfigureAwait(false);
        if (!reachable.IsSuccess)
        {
            Console.Error.WriteLine($"cannot reach the database: {reachable.CurrentMessage}");
            return 1;
        }

        var imported = await importer.Import(connectionString, options: null, CancellationToken.None).ConfigureAwait(false);
        if (!imported.IsSuccess)
        {
            Console.Error.WriteLine($"schema import failed: {imported.CurrentMessage}");
            return 1;
        }

        string json = JsonSerializer.Serialize(imported.Value, SchemaJson);

        if (outPath is null)
        {
            Console.Out.WriteLine(json);
        }
        else
        {
            await File.WriteAllTextAsync(outPath, json, CancellationToken.None).ConfigureAwait(false);
            Console.Error.WriteLine($"wrote {outPath}");
        }

        return 0;
    }
}
