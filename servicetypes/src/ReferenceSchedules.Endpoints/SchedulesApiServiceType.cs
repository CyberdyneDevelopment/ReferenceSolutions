using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

namespace ReferenceSchedules.Endpoints;

/// <summary>The schedules domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Schedules")]
public class SchedulesApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SchedulesApiServiceType"/> class.</summary>
    public SchedulesApiServiceType()
        : base("Schedules", "Schedules", "Schedules API", "HTTP endpoints for the schedules domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            new ScheduleEndpoints(),
        };
}
