using System.IO;
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
public sealed class FileSystemContainerPath : IFilePath
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

    /// <inheritdoc />
    public string FileName => PathValue[(PathValue.LastIndexOf('/') + 1)..];

    /// <inheritdoc />
    public string Extension => Path.GetExtension(FileName);

    /// <inheritdoc />
    /// <remarks>
    /// The path part replaces the directory of the relative file path and the name part replaces its file name, which carries its own extension
    /// (the format's canonical extension is added only when a container is declared, never to a name a run supplies). A blank part keeps what this
    /// path has. The result is resolved against the connection's Root like any other, so a part that climbs out of Root is refused when the file is read.
    /// </remarks>
    public IPath Readdress(string? pathPart, string? namePart)
    {
        var nameAt = PathValue.LastIndexOf('/') + 1;
        return new FileSystemContainerPath(
            (string.IsNullOrWhiteSpace(pathPart) ? PathValue[..nameAt] : pathPart.TrimEnd('/') + "/")
            + (string.IsNullOrWhiteSpace(namePart) ? PathValue[nameAt..] : namePart));
    }
}
