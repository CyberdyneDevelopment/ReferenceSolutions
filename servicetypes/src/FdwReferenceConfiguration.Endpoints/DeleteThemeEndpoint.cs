using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Endpoints;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Deletes a theme from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteThemeEndpoint : DeleteThemeEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteThemeEndpoint"/> class.
    /// </summary>
    public DeleteThemeEndpoint(ThemeConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/themes/{Name}");
        Policies("configurations:write");
        Summary(s =>
        {
            s.Summary = "Delete a theme";
            s.Description = "Deletes a UI theme. Cannot delete the default theme or built-in themes.";
        });
    }
}
