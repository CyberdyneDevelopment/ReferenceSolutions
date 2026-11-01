using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Evaluates a Built transformation with values for its inputs: <c>POST /transformations/{Name}/evaluate</c>.
/// </summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name; auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class EvaluateTransformationEndpoint : EvaluateTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="EvaluateTransformationEndpoint"/>.
    /// </summary>
    public EvaluateTransformationEndpoint(ILogger<EvaluateTransformationEndpointBase> logger, IFilterCalculations calculations)
        : base(logger, calculations)
    {
    }
}
