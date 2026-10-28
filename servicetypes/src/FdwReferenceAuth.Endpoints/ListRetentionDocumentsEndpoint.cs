using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the documents of one type that belong to an organization (FDW-2906).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListRetentionDocumentsEndpoint : ListRetentionDocumentsEndpointBase
{
    /// <inheritdoc />
    public ListRetentionDocumentsEndpoint(
        IRetentionCatalog catalog,
        ILogger<ListRetentionDocumentsEndpoint> logger)
        : base(logger, catalog)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:read");
        Summary(s =>
        {
            s.Summary = "List the documents of one type that belong to an organization";
            s.Description = "Lists the organization's documents of the type named by the documentType query value, with the dates the application knows, so a legal hold can be placed on one of them.";
        });
        Tags("Retention");
    }
}
