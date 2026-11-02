using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceOperations.Endpoints.TypeCollectionEndpointOptions;

/// <summary>Base for the TypeCollectionEndpoints options.</summary>
public abstract class TypeCollectionEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TypeCollectionEndpointBase"/> class.</summary>
    protected TypeCollectionEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "TypeCollectionEndpoint")
    {
    }
}

/// <summary>Base for the TypeCollectionEndpoints options, naming its endpoint by type.</summary>
public abstract class TypeCollectionEndpointBase<TEndpoint> : TypeCollectionEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="TypeCollectionEndpointBase{TEndpoint}"/> class.</summary>
    protected TypeCollectionEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
