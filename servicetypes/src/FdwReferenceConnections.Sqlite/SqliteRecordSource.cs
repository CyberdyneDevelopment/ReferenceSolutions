using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using SqliteAdoConnection = Microsoft.Data.Sqlite.SqliteConnection;
using SqliteAdoCommand = Microsoft.Data.Sqlite.SqliteCommand;
using SqliteAdoReader = Microsoft.Data.Sqlite.SqliteDataReader;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// A record source that owns the connection, command and reader it streams from.
/// </summary>
/// <remarks>
/// SqliteConnection opens a new ADO connection per operation, so the cursor keeps that one open for
/// as long as it is being read and disposes it at the end. The materialising Execute path can use
/// `await using` because it is finished with the reader before it returns; streaming cannot.
/// </remarks>
internal sealed class SqliteRecordSource : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _inner;
    private readonly SqliteAdoCommand _command;
    private readonly SqliteAdoReader _reader;
    private readonly SqliteAdoConnection _connection;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="SqliteRecordSource"/> class.</summary>
    public SqliteRecordSource(
        IRecordSource<DataRecord> inner,
        SqliteAdoCommand command,
        SqliteAdoReader reader,
        SqliteAdoConnection connection)
    {
        _inner = inner;
        _command = command;
        _reader = reader;
        _connection = connection;
    }

    /// <inheritdoc />
    public RecordSchema Schema => _inner.Schema;

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => _inner.Read();

    /// <inheritdoc />
    public IAsyncEnumerable<IGenericResult<DataRecord>> Read(CancellationToken cancellationToken = default)
        => _inner.Read(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _inner.Dispose();
        _reader.Dispose();
        _command.Dispose();
        _connection.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _inner.DisposeAsync().ConfigureAwait(false);
        await _reader.DisposeAsync().ConfigureAwait(false);
        await _command.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
