using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: read what an organization's documents offer when a retention policy is set (FDW-2906).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetRetentionChoicesEndpoint : GetRetentionChoicesEndpointBase
{
    /// <inheritdoc />
    public GetRetentionChoicesEndpoint(
        IRetentionCatalog catalog,
        ILogger<GetRetentionChoicesEndpoint> logger)
        : base(logger, catalog)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:read");
        Summary(s =>
        {
            s.Summary = "Read what an organization can choose for a retention policy";
            s.Description = "Lists the start fields a period can count from, the end actions a document can be given and the document types the organization has, as the application's documents offer them. A policy that names a start field or end action not listed here is refused.";
        });
        Tags("Retention");
    }
}
