using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality.Endpoints.Validators;

namespace FdwReferenceQuality.Endpoints.Validators;

/// <summary>
/// The create-quality-rule rules of this host: FDW's (<see cref="CreateQualityRuleRequestValidator"/>), with nothing the host adds. Before this
/// class the host ran no validator on the request, so an empty data set name or an unknown severity reached the provider (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceCreateQualityRuleRequestValidator : CreateQualityRuleRequestValidator;
