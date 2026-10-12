using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// <see cref="IPath"/> for a FileSystem container's physical address: the FULL relative file path
/// (the owning DataPath's folder + the container's own name + the format's canonical file extension,
/// e.g. <c>"sec/SecretManager.json"</c>). Unlike <c>GenericContainerPath</c>
/// — which carries only the DataPath name (correct for HTTP, where the DataPath IS the URL path) — this
/// carries the whole file path so a config header and its typed body under one DataPath resolve to
/// DISTINCT files.
/// </summary>
/// <remarks>
/// Why a file-specific IPath (not <c>GenericContainerPath</c>): its <see cref="Domain"/> is <c>"File"</c>,
/// marking the value a literal file path rather than a generic request path — mirroring how the MsSql
/// transport's <c>DatabasePath</c> is its structured schema-qualified address.
/// </remarks>
public sealed class FileSystemContainerPath : IPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemContainerPath"/> class.
    /// </summary>
    /// <param name="pathValue">The relative file path this container addresses.</param>
    public FileSystemContainerPath(string pathValue) => PathValue = pathValue;

    /// <inheritdoc />
    public string PathValue { get; }

    /// <inheritdoc />
    public string Domain => "File";
}
