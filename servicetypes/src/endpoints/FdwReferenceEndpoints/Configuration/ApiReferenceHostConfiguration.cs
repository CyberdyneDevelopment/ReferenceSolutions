using Fdw.Services.Hosts.Abstractions;
﻿using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceEndpoints.Configuration;

/// <summary>
/// Where this host's root redirects to, and the theme fallbacks its API reference pages use.
/// </summary>
/// <remarks>
/// Every value here was a literal in a host's Program.cs — where the root sends a browser, the font
/// and corner radius the pages fall back to when a theme names none, the theme they use when the
/// caller asks for none. Which of those is right differs per deployment, so they are a row rather
/// than constants compiled into a host. Each page's route and document are the page's own, declared
/// with it in code (FDW-919), so the OpenAPI route this row used to carry is gone.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ApiReference", ServiceType = "Host")]
public sealed partial class ApiReferenceHostConfiguration : IApiReferenceImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path the host's root redirects to: the reference page a browser should land on.</summary>
    public string RoutePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the monospace font used when a theme names none.</summary>
    public string FallbackFontFamilyMono { get; set; } = string.Empty;

    /// <summary>Gets or sets the corner radius used when a theme names none, in pixels.</summary>
    public int FallbackBorderRadius { get; set; }

    /// <summary>Gets or sets the theme used when the caller asks for none.</summary>
    public string FallbackThemeName { get; set; } = string.Empty;
}
