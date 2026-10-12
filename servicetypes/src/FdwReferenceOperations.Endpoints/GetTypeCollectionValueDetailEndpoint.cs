using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.TypeCollections;
using Microsoft.Extensions.Logging;

namespace FdwReferenceOperations.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class GetTypeCollectionValueDetailEndpoint : GetTypeCollectionValueDetailEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetTypeCollectionValueDetailEndpoint"/> class.</summary>
    public GetTypeCollectionValueDetailEndpoint(ILogger<GetTypeCollectionValueDetailEndpoint> logger) : base(logger)
    {
    }

}
