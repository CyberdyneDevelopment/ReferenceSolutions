using System.Runtime.CompilerServices;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A cursor that yields one row, can then block until it is cancelled, and records whether it was disposed.
/// </summary>
internal sealed class ScriptedRecordSource : IRecordSource<DataRecord>
{
    public RecordSchema Schema { get; } = new(Array.Empty<Fdw.Data.Abstractions.IDataField>());

    public bool BlockAfterFirstRow { get; init; }

    public bool Disposed { get; private set; }

    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        yield return Row();
    }

    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return Row();

        if (BlockAfterFirstRow)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    public void Dispose() => Disposed = true;

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }

    private IGenericResult<DataRecord> Row()
        => GenericResult<DataRecord>.Success(new DataRecord(Schema, Array.Empty<object?>()));
}
