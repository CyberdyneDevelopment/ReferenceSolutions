using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// A record source that owns the reader and command it streams from.
/// </summary>
/// <remarks>
/// It does NOT dispose the connection: PostgreSqlConnection holds one shared NpgsqlConnection for
/// its lifetime, so closing it here would break every later command on the same instance. MsSql
/// pools a connection per call and therefore disposes its own.
/// </remarks>
internal sealed class PostgreSqlRecordSource : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _inner;
    private readonly NpgsqlCommand _command;
    private readonly NpgsqlDataReader _reader;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="PostgreSqlRecordSource"/> class.</summary>
    public PostgreSqlRecordSource(IRecordSource<DataRecord> inner, NpgsqlCommand command, NpgsqlDataReader reader)
    {
        _inner = inner;
        _command = command;
        _reader = reader;
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
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _inner.DisposeAsync().ConfigureAwait(false);
        await _reader.DisposeAsync().ConfigureAwait(false);
        await _command.DisposeAsync().ConfigureAwait(false);
    }
}
