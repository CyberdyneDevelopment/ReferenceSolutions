using Fdw.Results;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using FdwReferencePipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// The one limit on updating a saved pipeline here: BatchCopy is the only engine wired, so only a BatchCopy pipeline can
/// be updated, and checking an edit of a saved pipeline must refuse exactly what updating it refuses (FDW-1105).
/// </summary>
internal static class WiredEngine
{
    /// <summary>Reads a saved pipeline as the one engine record this host writes.</summary>
    /// <param name="original">The saved pipeline.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The BatchCopy record, or the refusal naming the engine that is not wired.</returns>
    internal static IGenericResult<BatchCopyPipelineConfiguration> Of(IPipelineImplementationConfiguration original, ILogger logger)
    {
        // Why the concrete record: IEtlPipelineImplementationConfiguration is read-only and every writer builds the
        // engine's own record -- BatchCopy is the only engine wired, the same limit CreatePipelineEndpoint.BuildEngineBody states.
        if (original is BatchCopyPipelineConfiguration engine)
        {
            return GenericResult<BatchCopyPipelineConfiguration>.Success(engine);
        }

        return GenericResult<BatchCopyPipelineConfiguration>.Failure(PipelineLog.EngineNotWired(logger, original.Implementation));
    }
}
