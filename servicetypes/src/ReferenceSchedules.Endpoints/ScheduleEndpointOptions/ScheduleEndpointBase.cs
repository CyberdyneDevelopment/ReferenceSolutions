using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>Base for an endpoint over the schedule resource.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class ScheduleEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ScheduleEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ScheduleEndpoint")
    {
    }
}

/// <summary>Base for a schedule endpoint that takes its identity from the endpoint class itself.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class ScheduleEndpointBase<TEndpoint> : ScheduleEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleEndpointBase{TEndpoint}"/> class.</summary>
    protected ScheduleEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
