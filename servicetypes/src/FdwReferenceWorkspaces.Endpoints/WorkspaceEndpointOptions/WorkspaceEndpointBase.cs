using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>Base for the options that declare this app's workspace endpoints.</summary>
public abstract class WorkspaceEndpointBase : EndpointTypeOptionBase
{
    /// <inheritdoc />
    protected WorkspaceEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "WorkspaceEndpoint")
    {
    }
}

/// <summary>Base for the options that declare this app's workspace endpoints.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class WorkspaceEndpointBase<TEndpoint> : WorkspaceEndpointBase
    where TEndpoint : class
{
    /// <inheritdoc />
    protected WorkspaceEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
