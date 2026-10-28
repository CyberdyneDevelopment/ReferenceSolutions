using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.TypeCollections;
using Microsoft.Extensions.Logging;

namespace FdwReferenceOperations.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class ListTypeCollectionValuesEndpoint : ListTypeCollectionValuesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListTypeCollectionValuesEndpoint"/> class.</summary>
    public ListTypeCollectionValuesEndpoint(ILogger<ListTypeCollectionValuesEndpoint> logger) : base(logger)
    {
    }

}
