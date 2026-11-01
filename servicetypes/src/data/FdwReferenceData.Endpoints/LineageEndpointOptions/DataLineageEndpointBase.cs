using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>Base for the DataLineageEndpoints options.</summary>
public abstract class DataLineageEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataLineageEndpointBase"/> class.</summary>
    protected DataLineageEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "DataLineageEndpoint")
    {
    }
}

/// <summary>Base for the DataLineageEndpoints options, naming its endpoint by type.</summary>
public abstract class DataLineageEndpointBase<TEndpoint> : DataLineageEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="DataLineageEndpointBase{TEndpoint}"/> class.</summary>
    protected DataLineageEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
