namespace Reference.Api.Endpoints.DataStores;

/// <summary>
/// Request DTO for adding a path to an existing DataStore.
/// POST /datastores/{Name}/paths
/// </summary>
public sealed class AddDataStorePathRequest
{
    // Why: FastEndpoints binds this from the {Name} route segment in /datastores/{Name}/paths.
    /// <summary>Gets or sets the DataStore name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the new path (e.g. a schema name or URL prefix).</summary>
    public string PathName { get; set; } = string.Empty;

    // Why: Path is the per-feed value the transport actually uses to address the path — a DB
    // schema name (e.g. "dbo"/"sales") or, for an HTTP store, the URL suffix appended to the base
    // address (e.g. "all_hour.geojson"). Without it the HTTP transport cannot build the request URL.
    /// <summary>Gets or sets the actual path value (DB schema name, or URL suffix for HTTP stores).</summary>
    // Why: Non-nullable with an empty default mirrors DataPathConfiguration.Path (also a non-null
    // string defaulting to empty); an absent Path round-trips as empty rather than null.
    public string Path { get; set; } = string.Empty;

    // Why: PathType is the DataPath discriminator (e.g. "Schema" for DB schemas, "UrlSuffix" for
    // HTTP feeds). Accepted from the caller rather than defaulted so we never silently assume a type.
    /// <summary>Gets or sets the path type discriminator (e.g. "Schema", "UrlSuffix").</summary>
    public string? PathType { get; set; }

    /// <summary>Gets or sets the optional description for this path.</summary>
    public string? Description { get; set; }
}
