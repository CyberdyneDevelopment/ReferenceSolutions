using System.Diagnostics.CodeAnalysis;

namespace FdwReferencePipelines.Endpoints.Validators;

/// <summary>The rules both pipeline validators share about what a pipeline writes (FDW-1269), the mirror of <see cref="PipelineSourceRules"/>.</summary>
[ExcludeFromCodeCoverage]
internal static class PipelineDestinationRules
{
    /// <summary>Counts the destination forms a request names: a data set, a data store container.</summary>
    /// <param name="dataSet">The destination data set, if named.</param>
    /// <param name="dataStoreName">The destination data store, if named.</param>
    /// <returns>How many of the two are named.</returns>
    public static int DestinationForms(string? dataSet, string? dataStoreName) =>
        (string.IsNullOrWhiteSpace(dataSet) ? 0 : 1)
        + (string.IsNullOrWhiteSpace(dataStoreName) ? 0 : 1);

    /// <summary>True when a data store destination is either absent or names its store, path and container.</summary>
    /// <param name="dataStoreName">The destination data store.</param>
    /// <param name="path">The path within it.</param>
    /// <param name="containerName">The container within the path.</param>
    /// <returns>Whether the three are all absent or all present.</returns>
    public static bool DataStoreDestinationIsWhole(string? dataStoreName, string? path, string? containerName) =>
        (string.IsNullOrWhiteSpace(dataStoreName) && string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(containerName))
        || (!string.IsNullOrWhiteSpace(dataStoreName) && !string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(containerName));
}
