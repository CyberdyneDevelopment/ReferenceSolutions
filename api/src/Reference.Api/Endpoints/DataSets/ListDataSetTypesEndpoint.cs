using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to list the available DataSet strategy types (Simple/Compound/Federated) from the
/// source-generated <c>DataSetTypes</c> TypeCollection. The create-dataset wizard reads this list to
/// offer the <c>serviceOptionType</c> options.
/// </summary>
/// <remarks>
/// Why: closes <see cref="ListDataSetTypesEndpointBase"/> (whose <c>LoadItems</c> enumerates
/// <c>DataSetTypes.All()</c>) so the route <c>datasets/types</c> is reachable. A new strategy added as a
/// <c>[TypeOption(typeof(DataSetTypes), ...)]</c> appears here automatically — no endpoint change needed.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetTypesEndpoint : ListDataSetTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
