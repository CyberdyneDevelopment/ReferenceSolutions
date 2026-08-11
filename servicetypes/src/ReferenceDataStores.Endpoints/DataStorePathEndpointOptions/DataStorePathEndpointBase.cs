using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>Base for an endpoint over the data-store-path resource.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class DataStorePathEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStorePathEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected DataStorePathEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "DataStorePathEndpoint")
    {
    }
}

/// <summary>Base for a data-store-path endpoint that takes its identity from the endpoint class itself.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class DataStorePathEndpointBase<TEndpoint> : DataStorePathEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="DataStorePathEndpointBase{TEndpoint}"/> class.</summary>
    protected DataStorePathEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
