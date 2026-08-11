using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>Base for an endpoint over the fieldmappings surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class FieldMappingsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldMappingsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected FieldMappingsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "FieldMappingsEndpoint")
    {
    }
}

/// <summary>Base for a fieldmappings endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class FieldMappingsEndpointBase<TEndpoint> : FieldMappingsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="FieldMappingsEndpointBase{TEndpoint}"/> class.</summary>
    protected FieldMappingsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
