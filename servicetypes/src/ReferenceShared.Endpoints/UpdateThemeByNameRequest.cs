using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fdw.UI.Themes;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceShared.Endpoints.Logging;

namespace ReferenceShared.Endpoints;

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
