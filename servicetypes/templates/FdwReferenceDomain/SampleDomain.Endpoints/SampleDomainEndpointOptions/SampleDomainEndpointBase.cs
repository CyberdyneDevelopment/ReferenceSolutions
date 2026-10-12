using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace SampleDomain.Endpoints.SampleDomainEndpointOptions;

/// <summary>Base for the options that declare this group's endpoints.</summary>
public abstract class SampleDomainEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainEndpointBase"/> class.</summary>
    protected SampleDomainEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "SampleDomainEndpoint")
    {
    }
}

/// <summary>Base that names itself from the endpoint it declares.</summary>
/// <typeparam name="TEndpoint">The endpoint this option mounts.</typeparam>
public abstract class SampleDomainEndpointBase<TEndpoint> : SampleDomainEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainEndpointBase{TEndpoint}"/> class.</summary>
    protected SampleDomainEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
