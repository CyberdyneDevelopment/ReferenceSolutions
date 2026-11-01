using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>Base for the CatalogEndpoints options.</summary>
public abstract class CatalogEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="CatalogEndpointBase"/> class.</summary>
    protected CatalogEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "CatalogEndpoint")
    {
    }
}

/// <summary>Base for the CatalogEndpoints options, naming its endpoint by type.</summary>
public abstract class CatalogEndpointBase<TEndpoint> : CatalogEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="CatalogEndpointBase{TEndpoint}"/> class.</summary>
    protected CatalogEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
