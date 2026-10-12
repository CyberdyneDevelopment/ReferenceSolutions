using System.Diagnostics.CodeAnalysis;

namespace FdwReferencePipelines.Endpoints.Validators;

/// <summary>The rules both pipeline validators share about what a pipeline reads (FDW-1103).</summary>
[ExcludeFromCodeCoverage]
internal static class PipelineSourceRules
{
    /// <summary>Counts the source forms a request names: a data set, a connection, a data store container.</summary>
    /// <param name="dataSet">The source data set, if named.</param>
    /// <param name="connectionName">The source connection, if named.</param>
    /// <param name="dataStoreName">The source data store, if named.</param>
    /// <returns>How many of the three are named.</returns>
    public static int SourceForms(string? dataSet, string? connectionName, string? dataStoreName) =>
        (string.IsNullOrWhiteSpace(dataSet) ? 0 : 1)
        + (string.IsNullOrWhiteSpace(connectionName) ? 0 : 1)
        + (string.IsNullOrWhiteSpace(dataStoreName) ? 0 : 1);

    /// <summary>True when a data store source is either absent or names its store, path and container.</summary>
    /// <param name="dataStoreName">The source data store.</param>
    /// <param name="path">The path within it.</param>
    /// <param name="containerName">The container within the path.</param>
    /// <returns>Whether the three are all absent or all present.</returns>
    public static bool DataStoreSourceIsWhole(string? dataStoreName, string? path, string? containerName) =>
        (string.IsNullOrWhiteSpace(dataStoreName) && string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(containerName))
        || (!string.IsNullOrWhiteSpace(dataStoreName) && !string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(containerName));
}
