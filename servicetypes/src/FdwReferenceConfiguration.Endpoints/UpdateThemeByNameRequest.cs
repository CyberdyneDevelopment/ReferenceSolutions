using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Themes.Clients.Models;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Request to update a theme, including the route-bound name.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateThemeByNameRequest : UpdateThemeRequest
{
    /// <summary>
    /// Gets or sets the theme name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
