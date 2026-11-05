using System;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Extension methods for mapping request DTOs to <see cref="ThemeImplementationConfiguration"/>.
/// Framework-level mappings (ToDto, ToManaged, ToSummary) are in
/// <see cref="Fdw.UI.Themes.Configuration.ThemeConfigurationMapper"/>.
/// </summary>
public static class ThemeRequestMapper
{
    /// <summary>
    /// Builds the theme a create request makes. A value the request leaves out takes the default theme's value: the default theme is
    /// configuration (the theme row marked default, seeded with every value), not code and not a setting per field (FDW-3823, Mike: "theme
    /// configuration you just set defaults in configuration").
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="defaultTheme">The theme marked default, whose values an omitted one takes.</param>
    /// <returns>The theme to save.</returns>
    public static ThemeImplementationConfiguration ToManaged(this CreateThemeRequest request, IThemeImplementationConfiguration defaultTheme)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(defaultTheme);

        return new ThemeImplementationConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            // The implementation the Theme domain provider is registered under
            // (ThemeServiceType.Register("Theme", ...)). Save looks the name up before writing;
            // "" is registered for nothing. (FDW-790)
            Implementation = "Theme",
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = System.DateTimeOffset.UtcNow,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            PrimaryColor = request.PrimaryColor ?? defaultTheme.PrimaryColor,
            SecondaryColor = request.SecondaryColor ?? defaultTheme.SecondaryColor,
            TertiaryColor = request.TertiaryColor ?? defaultTheme.TertiaryColor,
            BackgroundColor = request.BackgroundColor ?? defaultTheme.BackgroundColor,
            SurfaceColor = request.SurfaceColor ?? defaultTheme.SurfaceColor,
            ErrorColor = request.ErrorColor ?? defaultTheme.ErrorColor,
            WarningColor = request.WarningColor ?? defaultTheme.WarningColor,
            SuccessColor = request.SuccessColor ?? defaultTheme.SuccessColor,
            InfoColor = request.InfoColor ?? defaultTheme.InfoColor,
            TextPrimary = request.TextPrimary ?? defaultTheme.TextPrimary,
            TextSecondary = request.TextSecondary ?? defaultTheme.TextSecondary,
            TextDisabled = request.TextDisabled ?? defaultTheme.TextDisabled,
            TextOnPrimary = request.TextOnPrimary ?? defaultTheme.TextOnPrimary,
            TextOnSecondary = request.TextOnSecondary ?? defaultTheme.TextOnSecondary,
            FontFamily = request.FontFamily ?? defaultTheme.FontFamily,
            FontFamilyMono = request.FontFamilyMono ?? defaultTheme.FontFamilyMono,
            FontSizeBase = request.FontSizeBase ?? defaultTheme.FontSizeBase,
            BorderRadius = request.BorderRadius ?? defaultTheme.BorderRadius,
            IsDarkMode = request.IsDarkMode ?? defaultTheme.IsDarkMode,
            LogoUrl = request.LogoUrl ?? defaultTheme.LogoUrl,
            AppName = request.AppName ?? defaultTheme.AppName,
            FaviconUrl = request.FaviconUrl ?? defaultTheme.FaviconUrl
        };
    }

    /// <summary>
    /// Applies non-null update values to an existing <see cref="ThemeImplementationConfiguration"/>.
    /// </summary>
    public static void ApplyUpdate(this ThemeImplementationConfiguration config, UpdateThemeRequest request)
    {
        if (request.DisplayName != null) config.DisplayName = request.DisplayName;
        if (request.Description != null) config.Description = request.Description;
        if (request.PrimaryColor != null) config.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor != null) config.SecondaryColor = request.SecondaryColor;
        if (request.TertiaryColor != null) config.TertiaryColor = request.TertiaryColor;
        if (request.BackgroundColor != null) config.BackgroundColor = request.BackgroundColor;
        if (request.SurfaceColor != null) config.SurfaceColor = request.SurfaceColor;
        if (request.ErrorColor != null) config.ErrorColor = request.ErrorColor;
        if (request.WarningColor != null) config.WarningColor = request.WarningColor;
        if (request.SuccessColor != null) config.SuccessColor = request.SuccessColor;
        if (request.InfoColor != null) config.InfoColor = request.InfoColor;
        if (request.TextPrimary != null) config.TextPrimary = request.TextPrimary;
        if (request.TextSecondary != null) config.TextSecondary = request.TextSecondary;
        if (request.TextDisabled != null) config.TextDisabled = request.TextDisabled;
        if (request.TextOnPrimary != null) config.TextOnPrimary = request.TextOnPrimary;
        if (request.TextOnSecondary != null) config.TextOnSecondary = request.TextOnSecondary;
        if (request.FontFamily != null) config.FontFamily = request.FontFamily;
        if (request.FontFamilyMono != null) config.FontFamilyMono = request.FontFamilyMono;
        if (request.FontSizeBase.HasValue) config.FontSizeBase = request.FontSizeBase.Value;
        if (request.BorderRadius.HasValue) config.BorderRadius = request.BorderRadius.Value;
        if (request.IsDarkMode.HasValue) config.IsDarkMode = request.IsDarkMode.Value;
        if (request.LogoUrl != null) config.LogoUrl = request.LogoUrl;
        if (request.AppName != null) config.AppName = request.AppName;
        if (request.FaviconUrl != null) config.FaviconUrl = request.FaviconUrl;
    }
}
