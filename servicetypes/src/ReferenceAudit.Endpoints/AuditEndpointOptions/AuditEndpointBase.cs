using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>Base for an endpoint over the audit surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class AuditEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="AuditEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected AuditEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "AuditEndpoint")
    {
    }
}

/// <summary>Base for a audit endpoint that takes its identity from the endpoint class.</summary>
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
