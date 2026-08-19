using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling.Endpoints;

namespace ReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to list the available schedule (trigger) types.
/// Sealed closure of the generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListScheduleTypesEndpoint : ListScheduleTypesEndpointBase
{
}
