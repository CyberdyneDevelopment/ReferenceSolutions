using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Documents.Abstractions.Logging;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// The document operations of the FileSystem connection over its root folder (FDW-1798).
/// </summary>
/// <remarks>
/// A document is a file; its path is the file's path below the root. Folders exist only while they hold a document, as in an object store:
/// uploading creates the folders above the document, and listing a folder that does not exist answers an empty page. No metadata and no
/// content type are kept, because the connection declares neither capability; an upload that carries metadata is refused rather than
/// having it silently dropped. A ranged read buffers the range it returns; a whole read streams the file.
/// </remarks>
internal sealed class FileSystemDocumentStore
{
    private const int BufferSize = 81920;

    private readonly string _root;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    internal FileSystemDocumentStore(string root, string connectionName, ILogger? logger)
    {
        // Why the root is made absolute and loses a trailing separator: a path is inside the root when it starts with the root and one separator,
        // so a root configured as /var/lib/fdw/sources/ would compare against //, and every path would be refused as outside the scope.
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root))));
        _connectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        _logger = logger ?? NullLogger.Instance;
    }

    internal async Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.FirstProblem() is { } problem)
        {
            return GenericResult<T>.Failure(DocumentResultCodes.ByName("DocumentPathInvalid"), ResultDetails.Create("Path", string.Empty, "Reason", problem));
        }

        object result = command switch
        {
            UploadCommand upload => await Upload(upload, cancellationToken).ConfigureAwait(false),
            DownloadCommand download => Download(download),
            GetInfoCommand getInfo => GetInfo(getInfo.Path),
            ListCommand list => List(list),
            DeleteCommand delete => Delete(delete.Path),
            CopyCommand copy => Transfer(copy.Source, copy.Destination, copy.CreateOnly, move: false),
            MoveCommand move => Transfer(move.Source, move.Destination, move.CreateOnly, move: true),
            CreateReadLinkCommand => GenericResult<DocumentReadLink>.Failure(
                DocumentResultCodes.ByName("DocumentReadLinksNotSupported"), ResultDetails.Create("Connection", _connectionName)),
            _ => GenericResult<T>.Failure(
                DocumentResultCodes.ByName("DocumentStorageRefused"),
                ResultDetails.Create("Path", string.Empty, "Reason", $"the file system connection does not answer '{command.GetType().Name}'")),
        };

        return result is IGenericResult<T> typed
            ? typed
            : GenericResult<T>.Failure(
                DocumentResultCodes.ByName("DocumentStorageRefused"),
                ResultDetails.Create("Path", string.Empty, "Reason", $"the answer to '{command.GetType().Name}' is not the result type the command asks for"));
    }

    // The file a document path names. The path was parsed by DocumentPath, which refuses ".." and "."; the full path is checked against the
    // root again so a path can never leave it.
    private IGenericResult<string> Physical(DocumentPath path)
    {
        var full = Path.GetFullPath(Path.Combine(_root, path.Value.Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || string.Equals(full, _root, StringComparison.Ordinal)
            ? GenericResult<string>.Success(full)
            : GenericResult<string>.Failure(DocumentResultCodes.ByName("DocumentOutsideScope"), ResultDetails.Create("Path", path.Value, "Scope", _root));
    }

    private async Task<IGenericResult<DocumentInfo>> Upload(UploadCommand upload, CancellationToken cancellationToken)
    {
        if (upload.Metadata.Count > 0)
        {
            return Refused<DocumentInfo>(upload.Path, "the file system connection keeps no document metadata");
        }

        var physical = Physical(upload.Path);
        if (physical.IsFailure || physical.Value is not { } file)
        {
            return physical.ToNewResult<DocumentInfo>();
        }

        if (upload.CreateOnly && File.Exists(file))
        {
            return GenericResult<DocumentInfo>.Failure(DocumentResultCodes.ByName("DocumentAlreadyExists"), ResultDetails.Create("Path", upload.Path.Value));
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await using var target = new FileStream(
                file, upload.CreateOnly ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            await upload.Content.CopyToAsync(target, BufferSize, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex) when (upload.CreateOnly && File.Exists(file))
        {
            return Refused<DocumentInfo>(upload.Path, ex.Message, alreadyExists: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Refused<DocumentInfo>(upload.Path, ex.Message);
        }

        return GetInfo(upload.Path);
    }

    private IGenericResult<DocumentContent> Download(DownloadCommand download)
    {
        var info = GetInfo(download.Path);
        if (info.IsFailure || info.Value is not { } found)
        {
            return info.ToNewResult<DocumentContent>();
        }

        try
        {
            var stream = new FileStream(Physical(download.Path).Value!, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            return download.Range is not { } range
                ? GenericResult<DocumentContent>.Success(new DocumentContent(found, stream))
                : ReadRange(found, stream, range, download.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Refused<DocumentContent>(download.Path, ex.Message);
        }
    }

    // A range is buffered: it starts at the offset and holds up to the length asked for, or the rest of the file if that is less.
    private IGenericResult<DocumentContent> ReadRange(DocumentInfo info, FileStream stream, DocumentRange range, DocumentPath path)
    {
        using (stream)
        {
            if (range.Offset >= info.Length)
            {
                return Refused<DocumentContent>(path, $"the range starts at {range.Offset}, after the end of the {info.Length}-byte document");
            }

            var count = (int)Math.Min(Math.Min(range.Length, info.Length - range.Offset), int.MaxValue);
            var buffer = new byte[count];
            stream.Seek(range.Offset, SeekOrigin.Begin);
            stream.ReadExactly(buffer, 0, count);
            return GenericResult<DocumentContent>.Success(new DocumentContent(info, new MemoryStream(buffer, writable: false)));
        }
    }

    private IGenericResult<DocumentInfo> GetInfo(DocumentPath path)
    {
        var physical = Physical(path);
        if (physical.IsFailure || physical.Value is not { } file)
        {
            return physical.ToNewResult<DocumentInfo>();
        }

        var info = new FileInfo(file);
        return info.Exists
            ? GenericResult<DocumentInfo>.Success(InfoOf(path, info))
            : GenericResult<DocumentInfo>.Failure(DocumentResultCodes.ByName("DocumentNotFound"), ResultDetails.Create("Path", path.Value));
    }

    private static DocumentInfo InfoOf(DocumentPath path, FileInfo file)
        => new(path, file.Length, null, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), null, null, new Dictionary<string, string>(StringComparer.Ordinal));

    // One page of the documents below a folder, in path order. The continuation is the last path of the page; the next page starts after it.
    private IGenericResult<DocumentPage> List(ListCommand list)
    {
        if (!Directory.Exists(_root))
        {
            return Refused<DocumentPage>(DocumentPath.Root, "the root folder of the connection does not exist");
        }

        var folder = Physical(list.Folder);
        if (folder.IsFailure || folder.Value is not { } directory)
        {
            return folder.ToNewResult<DocumentPage>();
        }

        if (!Directory.Exists(directory))
        {
            return GenericResult<DocumentPage>.Success(new DocumentPage([], null));
        }

        var found = new List<DocumentInfo>();
        foreach (var file in Directory.EnumerateFiles(directory, "*", list.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            var relative = DocumentPath.Parse(Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/'));
            if (relative.IsFailure)
            {
                return relative.ToNewResult<DocumentPage>();
            }

            found.Add(InfoOf(relative.Value, new FileInfo(file)));
        }

        var after = found
            .OrderBy(info => info.Path.Value, StringComparer.Ordinal)
            .Where(info => list.Continuation is null || string.CompareOrdinal(info.Path.Value, list.Continuation) > 0)
            .Take(list.PageSize + 1)
            .ToList();
        var page = after.Take(list.PageSize).ToList();
        return GenericResult<DocumentPage>.Success(new DocumentPage(page, after.Count > list.PageSize ? page[^1].Path.Value : null));
    }

    // Deleting what is not there is not an error: the answer says whether a document was removed.
    private IGenericResult<bool> Delete(DocumentPath path)
    {
        var physical = Physical(path);
        if (physical.IsFailure || physical.Value is not { } file)
        {
            return physical.ToNewResult<bool>();
        }

        if (!File.Exists(file))
        {
            return GenericResult<bool>.Success(false);
        }

        try
        {
            File.Delete(file);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Refused<bool>(path, ex.Message);
        }
    }

    private IGenericResult<DocumentInfo> Transfer(DocumentPath source, DocumentPath destination, bool createOnly, bool move)
    {
        var from = Physical(source);
        var to = Physical(destination);
        if (from.IsFailure || from.Value is not { } sourceFile)
        {
            return from.ToNewResult<DocumentInfo>();
        }

        if (to.IsFailure || to.Value is not { } destinationFile)
        {
            return to.ToNewResult<DocumentInfo>();
        }

        if (!File.Exists(sourceFile))
        {
            return GenericResult<DocumentInfo>.Failure(DocumentResultCodes.ByName("DocumentNotFound"), ResultDetails.Create("Path", source.Value));
        }

        if (createOnly && File.Exists(destinationFile))
        {
            return GenericResult<DocumentInfo>.Failure(DocumentResultCodes.ByName("DocumentAlreadyExists"), ResultDetails.Create("Path", destination.Value));
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            if (move)
            {
                File.Move(sourceFile, destinationFile, overwrite: !createOnly);
            }
            else
            {
                File.Copy(sourceFile, destinationFile, overwrite: !createOnly);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Refused<DocumentInfo>(destination, ex.Message);
        }

        return GetInfo(destination);
    }

    private IGenericResult<T> Refused<T>(DocumentPath path, string reason, bool alreadyExists = false)
    {
        if (alreadyExists)
        {
            return GenericResult<T>.Failure(DocumentResultCodes.ByName("DocumentAlreadyExists"), ResultDetails.Create("Path", path.Value));
        }

        DocumentLog.StorageRefused(_logger, path.Value, reason);
        return GenericResult<T>.Failure(DocumentResultCodes.ByName("DocumentStorageRefused"), ResultDetails.Create("Path", path.Value, "Reason", reason));
    }
}
