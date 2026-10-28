using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The base of every option in the audit endpoint group.</summary>
public abstract class AuditEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="AuditEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class.</param>
    /// <param name="description">What the endpoint does.</param>
    protected AuditEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "AuditEndpoint")
    {
    }
}

/// <summary>An option in the audit endpoint group, named after the endpoint it holds.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class AuditEndpointBase<TEndpoint> : AuditEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="AuditEndpointBase{TEndpoint}"/> class.</summary>
    protected AuditEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
