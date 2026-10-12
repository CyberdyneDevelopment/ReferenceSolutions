using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>Base for the SampleEndpoints options.</summary>
public abstract class SampleEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleEndpointBase"/> class.</summary>
    protected SampleEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "SampleEndpoint")
    {
    }
}

/// <summary>Base for the SampleEndpoints options, naming its endpoint by type.</summary>
public abstract class SampleEndpointBase<TEndpoint> : SampleEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="SampleEndpointBase{TEndpoint}"/> class.</summary>
    protected SampleEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
