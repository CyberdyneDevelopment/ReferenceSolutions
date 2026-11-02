using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEndpoints.ApiReference;

/// <summary>Concrete endpoint for the whole-surface API reference document.</summary>
/// <remarks>
/// Behaviour lives in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off — and to name the document it cuts.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ServiceReferenceDocumentEndpoint : ServiceReferenceDocumentEndpointBase
{
    /// <inheritdoc />
    public ServiceReferenceDocumentEndpoint(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles)
        : base(logger, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => FdwReferenceEndpoints.Endpoints.OpenApiDocumentName;
}
