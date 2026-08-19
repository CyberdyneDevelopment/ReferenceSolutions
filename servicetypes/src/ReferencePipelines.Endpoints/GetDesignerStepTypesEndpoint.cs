using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Pipelines.Endpoints;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to list the pipeline step types available to the designer.
/// Sealed closure of the generic base class from Fdw.UI.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDesignerStepTypesEndpoint : GetDesignerStepTypesEndpointBase
{
}
