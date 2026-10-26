using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.TypeCollections;
using Microsoft.Extensions.Logging;

namespace FdwReferenceOperations.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class ListTypeCollectionsEndpoint : ListTypeCollectionsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListTypeCollectionsEndpoint"/> class.</summary>
    public ListTypeCollectionsEndpoint(ILogger<ListTypeCollectionsEndpoint> logger) : base(logger)
    {
    }
}
