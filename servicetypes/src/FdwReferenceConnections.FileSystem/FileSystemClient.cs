using FdwReferenceData.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using FdwReferenceConnections.FileSystem.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FileSystemLog = FdwReferenceConnections.FileSystem.Abstractions.Logging.FileSystemConnectionLog;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Default implementation of <see cref="IFileSystemClient"/>.
/// All methods validate the resolved path is within Root before performing I/O.
/// </summary>
internal sealed class FileSystemClient : IFileSystemClient
{
    private readonly string _canonicalRoot;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    internal FileSystemClient(string canonicalRoot, string connectionName, ILogger? logger)
    {
        _canonicalRoot = canonicalRoot;
        _connectionName = connectionName;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> ReadText(string relativePath, CancellationToken ct = default)
    {
        FileSystemLog.ReadingText(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<string>();

        var path = resolveResult.Value!;
        if (!File.Exists(path))
            return GenericResult<string>.Failure(
                FileSystemLog.FileNotFound(_logger, _connectionName, path));

        try
        {
            var text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            FileSystemLog.ReadTextCompleted(_logger, _connectionName, relativePath, text.Length);
            return GenericResult<string>.Success(text);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> ReadText(string relativePath, RawTextLineRange lines, CancellationToken ct = default)
    {
        FileSystemLog.ReadingText(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<string>();

        var path = resolveResult.Value!;
        if (!File.Exists(path))
            return GenericResult<string>.Failure(
                FileSystemLog.FileNotFound(_logger, _connectionName, path));

        try
        {
            // Why: File.ReadLinesAsync yields lazily; LINQ Skip/Take materialises only the
            // requested slice without loading the entire file into memory.
            var allLines = File.ReadLinesAsync(path, ct);
            var slice = allLines
                .Skip(lines.StartLine - 1)
                .Take(lines.EndLine - lines.StartLine + 1);
            var text = string.Join(Environment.NewLine, await slice.ToArrayAsync(ct).ConfigureAwait(false));
            FileSystemLog.ReadTextCompleted(_logger, _connectionName, relativePath, text.Length);
            return GenericResult<string>.Success(text);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<byte[]>> ReadBytes(string relativePath, CancellationToken ct = default)
    {
        FileSystemLog.ReadingBytes(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<byte[]>();

        var path = resolveResult.Value!;
        if (!File.Exists(path))
            return GenericResult<byte[]>.Failure(
                FileSystemLog.FileNotFound(_logger, _connectionName, path));

        try
        {
            var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            FileSystemLog.ReadBytesCompleted(_logger, _connectionName, relativePath, bytes.Length);
            return GenericResult<byte[]>.Success(bytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<byte[]>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<byte[]>> ReadBytes(string relativePath, long offset, int length, CancellationToken ct = default)
    {
        FileSystemLog.ReadingBytes(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<byte[]>();

        var path = resolveResult.Value!;
        if (!File.Exists(path))
            return GenericResult<byte[]>.Failure(
                FileSystemLog.FileNotFound(_logger, _connectionName, path));

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[length];
            var read = await fs.ReadAsync(buffer.AsMemory(0, length), ct).ConfigureAwait(false);
            // Why: Trim the buffer if fewer bytes were available at the offset+length position.
            var bytes = read < length ? buffer[..read] : buffer;
            FileSystemLog.ReadBytesCompleted(_logger, _connectionName, relativePath, bytes.Length);
            return GenericResult<byte[]>.Success(bytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<byte[]>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<bool>> Exists(string relativePath, CancellationToken ct = default)
    {
        FileSystemLog.CheckingExists(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return Task.FromResult(resolveResult.ToNewResult<bool>());

        return Task.FromResult(GenericResult<bool>.Success(File.Exists(resolveResult.Value!)));
    }

    // Why the client holds this rather than the transaction wrapping the connection: only the client
    // knows which file a command resolves to, and an undo log that cannot name the files it must put
    // back is not an undo log.
    private IFileSystemUndoLog? _undoLog;

    /// <summary>Attaches an undo log that captures prior content before each write.</summary>
    /// <param name="undoLog">The log to attach.</param>
    /// <returns>A failure when a unit of work is already open on this client.</returns>
    /// <remarks>
    /// One at a time, and refusing rather than replacing: a second unit would interleave its writes
    /// with the first one's and neither could be undone without undoing part of the other.
    /// </remarks>
    internal IGenericResult AttachUndoLog(IFileSystemUndoLog undoLog)
    {
        if (_undoLog is not null)
        {
            return GenericResult.Failure(
                FileSystemLog.UnitOfWorkAlreadyOpen(_logger, _connectionName));
        }

        _undoLog = undoLog;
        return GenericResult.Success();
    }

    /// <summary>Detaches the current undo log, ending the unit of work.</summary>
    internal void DetachUndoLog() => _undoLog = null;

    /// <inheritdoc />
    public async Task<IGenericResult<int>> WriteText(string relativePath, string text, CancellationToken ct = default)
    {
        FileSystemLog.WritingText(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<int>();

        var path = resolveResult.Value!;
        if (_undoLog is not null)
        {
            await _undoLog.Capture(relativePath, ct).ConfigureAwait(false);
        }

        string? tempPath = null;
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Why: write-then-atomic-rename, NOT a direct truncate-then-stream write. The whole file is
            // rewritten on every config write (Save/Update/ConfigurationDelete) — a process kill or power
            // loss mid-write between truncate and flush would leave the file empty/partial, destroying
            // every version of every record in the container. File.Move(..., overwrite: true) onto the
            // same directory is atomic on both NTFS (Windows) and ext4/xfs (Linux) — a concurrent reader
            // always observes either the complete old file or the complete new file, never a torn write.
            tempPath = string.Concat(path, ".", Guid.NewGuid().ToString("N"), ".tmp");
            await File.WriteAllTextAsync(tempPath, text, ct).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
            tempPath = null; // moved successfully — nothing left to clean up

            FileSystemLog.WriteTextCompleted(_logger, _connectionName, relativePath);
            return GenericResult<int>.Success(text.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
        finally
        {
            // Why: best-effort cleanup of an orphaned temp file left by a failed write/move — the actual
            // failure result was already produced above; a leftover .tmp file is cosmetic, not fatal.
            CleanupOrphanedTempFile(tempPath);
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> WriteBytes(string relativePath, byte[] bytes, CancellationToken ct = default)
    {
        FileSystemLog.WritingBytes(_logger, _connectionName, relativePath);
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePath, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return resolveResult.ToNewResult<int>();

        var path = resolveResult.Value!;
        if (_undoLog is not null)
        {
            await _undoLog.Capture(relativePath, ct).ConfigureAwait(false);
        }

        string? tempPath = null;
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Why: same write-then-atomic-rename rationale as WriteText — see there.
            tempPath = string.Concat(path, ".", Guid.NewGuid().ToString("N"), ".tmp");
            await File.WriteAllBytesAsync(tempPath, bytes, ct).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
            tempPath = null;

            FileSystemLog.WriteBytesCompleted(_logger, _connectionName, relativePath, bytes.Length);
            return GenericResult<int>.Success(bytes.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(
                FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePath, ex.Message));
        }
        finally
        {
            // Why: same best-effort, logged cleanup as WriteText — see there.
            CleanupOrphanedTempFile(tempPath);
        }
    }

    // Why: a dedicated void-returning helper (not inlined in WriteText/WriteBytes' finally blocks) for the
    // best-effort temp-file cleanup catch. FDW014 requires every catch block inside an IGenericResult-
    // returning method to either rethrow or return a propagating result — but a SECONDARY cleanup failure
    // must NOT override the PRIMARY write/move failure result already produced above it. Extracting the
    // catch into its own void method (outside the IGenericResult-returning method) is the correct shape for
    // this genuinely best-effort concern, not a suppression of the analyzer's real intent.
    private void CleanupOrphanedTempFile(string? tempPath)
    {
        if (tempPath is null || !File.Exists(tempPath))
            return;

        try
        {
            File.Delete(tempPath);
        }
        catch (Exception cleanupEx)
        {
            FileSystemLog.TempFileCleanupFailed(_logger, cleanupEx, _connectionName, tempPath, cleanupEx.Message);
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<string>>> List(string relativePrefix, CancellationToken ct = default)
    {
        var resolveResult = PathCanonicalizer.Resolve(_canonicalRoot, relativePrefix ?? string.Empty, _connectionName, _logger);
        if (!resolveResult.IsSuccess)
            return Task.FromResult(resolveResult.ToNewResult<IReadOnlyList<string>>());

        try
        {
            var root = resolveResult.Value!;
            if (!Directory.Exists(root))
                return Task.FromResult<IGenericResult<IReadOnlyList<string>>>(
                    GenericResult<IReadOnlyList<string>>.Success(Array.Empty<string>()));

            // Why: enumerate recursively and return paths relative to the canonical root so
            // callers cannot infer the absolute root from a List response.
            var rootWithSep = _canonicalRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _canonicalRoot
                : _canonicalRoot + Path.DirectorySeparatorChar;
            var entries = Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(absolute => absolute.StartsWith(rootWithSep, StringComparison.Ordinal)
                    ? absolute.Substring(rootWithSep.Length)
                    : absolute)
                .ToList();
            return Task.FromResult<IGenericResult<IReadOnlyList<string>>>(
                GenericResult<IReadOnlyList<string>>.Success(entries));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<IReadOnlyList<string>>>(
                GenericResult<IReadOnlyList<string>>.Failure(
                    FileSystemLog.IoFailed(_logger, ex, _connectionName, relativePrefix ?? string.Empty, ex.Message)));
        }
    }
}
