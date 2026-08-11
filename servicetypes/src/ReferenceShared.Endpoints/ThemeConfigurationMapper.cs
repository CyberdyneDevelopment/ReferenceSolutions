using System;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Extension methods for mapping request DTOs to <see cref="ThemeManagedConfiguration"/>.
/// Framework-level mappings (ToDto, ToManaged, ToSummary) are in
/// <see cref="Fdw.UI.Themes.Configuration.ThemeConfigurationMapper"/>.
/// </summary>
public static class ThemeRequestMapper
{
    /// <summary>
    /// Creates a <see cref="ThemeManagedConfiguration"/> from a <see cref="CreateThemeRequest"/>.
    /// </summary>
    public static ThemeManagedConfiguration ToManaged(this CreateThemeRequest request)
    {
        return new ThemeManagedConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            PrimaryColor = request.PrimaryColor ?? "#7209B7",
            SecondaryColor = request.SecondaryColor ?? "#F72585",
            TertiaryColor = request.TertiaryColor,
            BackgroundColor = request.BackgroundColor ?? "#0F1115",
            SurfaceColor = request.SurfaceColor ?? "#16191F",
            ErrorColor = request.ErrorColor ?? "#DC2626",
            WarningColor = request.WarningColor ?? "#F59E0B",
            SuccessColor = request.SuccessColor ?? "#10B981",
            InfoColor = request.InfoColor ?? "#00B4D8",
            TextPrimary = request.TextPrimary ?? "#E2E8F0",
            TextSecondary = request.TextSecondary ?? "#94A3B8",
            TextDisabled = request.TextDisabled,
            TextOnPrimary = request.TextOnPrimary,
            TextOnSecondary = request.TextOnSecondary,
            FontFamily = request.FontFamily ?? "Space Grotesk, system-ui, sans-serif",
            FontFamilyMono = request.FontFamilyMono ?? "JetBrains Mono, monospace",
            FontSizeBase = request.FontSizeBase ?? 14,
            BorderRadius = request.BorderRadius ?? 6,
            IsDarkMode = request.IsDarkMode ?? true,
            LogoUrl = request.LogoUrl,
            AppName = request.AppName ?? "Fdw",
            FaviconUrl = request.FaviconUrl
        };
    }

    /// <summary>
    /// Applies non-null update values to an existing <see cref="ThemeManagedConfiguration"/>.
    /// </summary>
    public static void ApplyUpdate(this ThemeManagedConfiguration config, UpdateThemeRequest request)
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
