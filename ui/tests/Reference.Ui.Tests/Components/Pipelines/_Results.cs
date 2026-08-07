using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// Small helpers for building <see cref="IGenericResult{T}"/> success/failure values
/// in the Pipelines/Scheduling/Etl.Projects bUnit suite. Keeps each test focused on the
/// branch it exercises rather than result plumbing.
/// </summary>
internal static class Res
{
    public static IGenericResult<T> Ok<T>(T value) => GenericResult<T>.Success(value);

    // Why: GenericResult.Failure(string) was removed in rc.2-reorg; failures require IGenericMessage.
    public static IGenericResult<T> Fail<T>(string message = "boom") =>
        GenericResult<T>.Failure(new GenericMessage { Message = message });

    public static IGenericResult<IReadOnlyList<T>> OkList<T>(params T[] items) =>
        GenericResult<IReadOnlyList<T>>.Success(items);
}
