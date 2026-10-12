using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to list the available schedule (trigger) types.
/// Sealed closure of the generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListScheduleTypesEndpoint : ListScheduleTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListScheduleTypesEndpoint"/> class.</summary>
    public ListScheduleTypesEndpoint(ILogger<ListScheduleTypesEndpoint> logger) : base(logger)
    {
    }

}
