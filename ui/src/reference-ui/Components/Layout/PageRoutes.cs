using System;
using System.Linq;
using Fdw.Results;
using Fdw.UI.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Reference.Ui.Logging;

namespace Reference.Ui.Components.Layout;

/// <summary>
/// Resolves the sidebar link for a declared page from its component's routes.
/// </summary>
// Why: this lives in the RENDERER, not in Fdw.UI. Fdw.UI is deliberately free of any Blazor reference so
// a non-Blazor renderer can consume the same page declarations, and [Route] is a Blazor type. Mapping a
// declared page to a Blazor href is therefore the Blazor renderer's job — the same way a terminal renderer
// would resolve its own addressing from the same IPage.
//
// Why a result and not a throw: a mis-declared page must not tear down the sidebar. The caller renders the
// failure in place, so a bad declaration is visible rather than fatal and rather than silently skipped.
internal static class PageRoutes
{
    /// <summary>
    /// Returns the route a sidebar entry should link to.
    /// </summary>
    /// <param name="page">The declared page.</param>
    /// <param name="logger">Receives the structured failure when the declaration is unusable.</param>
    /// <returns>
    /// The first parameterless route template on the page's component, or a failure when it declares none.
    /// </returns>
    internal static IGenericResult<string> NavRoute(IPage page, ILogger logger)
    {
        // Why: [Route] allows multiple templates (that is how a component gets several @page directives)
        // and some are parameterised — "/datasets/calculated/{Name}/edit" cannot be a nav target. The link
        // is the first template with no parameter.
        var template = page.Component
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(a => a.Template)
            .FirstOrDefault(t => !t.Contains('{', StringComparison.Ordinal));

        return template is null
            ? GenericResult<string>.Failure(
                NavLog.NoParameterlessRoute(logger, page.Name, page.Component.FullName ?? page.Component.Name))
            : GenericResult<string>.Success(template);
    }
}
