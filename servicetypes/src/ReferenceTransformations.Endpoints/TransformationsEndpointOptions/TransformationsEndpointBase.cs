using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>Base for an endpoint over the transformations surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class TransformationsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TransformationsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected TransformationsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "TransformationsEndpoint")
    {
    }
}

/// <summary>Base for a transformations endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class TransformationsEndpointBase<TEndpoint> : TransformationsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="TransformationsEndpointBase{TEndpoint}"/> class.</summary>
    protected TransformationsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
