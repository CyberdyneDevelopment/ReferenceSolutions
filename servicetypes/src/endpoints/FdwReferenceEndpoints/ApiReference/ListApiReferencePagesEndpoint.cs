using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEndpoints.ApiReference;

/// <summary>Concrete endpoint for the list of API reference pages the caller may open.</summary>
/// <remarks>Behaviour lives in the base; this gives its <c>[TypeOption]</c> a declared type to name.</remarks>
[ExcludeFromCodeCoverage]
public sealed class ListApiReferencePagesEndpoint : ListApiReferencePagesEndpointBase
{
    /// <inheritdoc />
    public ListApiReferencePagesEndpoint(
        ILogger<ListApiReferencePagesEndpointBase> logger,
        ISystemRoleConfiguration systemRoles)
        : base(logger, systemRoles)
    {
    }
}
