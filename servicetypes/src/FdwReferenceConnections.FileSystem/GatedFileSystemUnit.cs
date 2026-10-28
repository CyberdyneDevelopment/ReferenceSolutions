using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// A unit of work opened for a command that assembles its own result, which gives up its turn on the connection when the unit
/// ends: committed, rolled back, or disposed. Everything else is the unit it wraps.
/// </summary>
internal sealed class GatedFileSystemUnit : IDataConnectionTransaction
{
    private readonly IDataConnectionTransaction _unit;
    private readonly SemaphoreSlim _gate;
    private int _released;

    internal GatedFileSystemUnit(IDataConnectionTransaction unit, SemaphoreSlim gate)
    {
        _unit = unit;
        _gate = gate;
    }

    /// <inheritdoc />
    public bool IsActive => _unit.IsActive;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => _unit.Execute<T>(command, container, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult> Execute(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => _unit.Execute(command, container, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unit.Commit(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unit.Rollback(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _unit.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    private void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
            _gate.Release();
    }
}
