using System.Diagnostics.CodeAnalysis;
#pragma warning disable CS1591
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace ReferenceShared.Endpoints.Logging;

/// <summary>
/// MessageLogging for Theme operations.
/// EventId range: 1770-1789
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ThemeLog
{
    /// <summary>
    /// Logs when listing all available themes begins.
    /// </summary>
    [MessageLogging(
        EventId = 1770,
        Level = LogLevel.Trace,
        Message = "Listing available themes")]
    public static partial IGenericMessage ListingThemes(ILogger logger);

    /// <summary>
    /// Logs when themes have been retrieved.
    /// </summary>
    [MessageLogging(
        EventId = 1771,
        Level = LogLevel.Information,
        Message = "Found {count} themes")]
    public static partial IGenericMessage ThemesRetrieved(ILogger logger, int count);

    /// <summary>
    /// Logs when retrieving a specific theme by name.
    /// </summary>
    [MessageLogging(
        EventId = 1772,
        Level = LogLevel.Trace,
        Message = "Getting theme: {name}")]
    public static partial IGenericMessage GettingTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when a theme is not found.
    /// </summary>
    [MessageLogging(
        EventId = 1773,
        Level = LogLevel.Warning,
        Message = "Theme not found: {name}")]
    public static partial IGenericMessage ThemeNotFound(ILogger logger, string name);

    /// <summary>
    /// Logs when retrieving the default theme.
    /// </summary>
    [MessageLogging(
        EventId = 1774,
        Level = LogLevel.Trace,
        Message = "Getting default theme")]
    public static partial IGenericMessage GettingDefaultTheme(ILogger logger);

    /// <summary>
    /// Logs when creating a new theme.
    /// </summary>
    [MessageLogging(
        EventId = 1775,
        Level = LogLevel.Trace,
        Message = "Creating theme: {name}")]
    public static partial IGenericMessage CreatingTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when a theme already exists during creation.
    /// </summary>
    [MessageLogging(
        EventId = 1776,
        Level = LogLevel.Warning,
        Message = "Theme already exists: {name}")]
    public static partial IGenericMessage ThemeAlreadyExists(ILogger logger, string name);

    /// <summary>
    /// Logs when a theme has been successfully created.
    /// </summary>
    [MessageLogging(
        EventId = 1777,
        Level = LogLevel.Information,
        Message = "Theme created: {name}")]
    public static partial IGenericMessage ThemeCreated(ILogger logger, string name);

    /// <summary>
    /// Logs when updating an existing theme.
    /// </summary>
    [MessageLogging(
        EventId = 1778,
        Level = LogLevel.Trace,
        Message = "Updating theme: {name}")]
    public static partial IGenericMessage UpdatingTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when a theme has been successfully updated.
    /// </summary>
    [MessageLogging(
        EventId = 1779,
        Level = LogLevel.Information,
        Message = "Theme updated: {name}")]
    public static partial IGenericMessage ThemeUpdated(ILogger logger, string name);

    /// <summary>
    /// Logs when deleting a theme.
    /// </summary>
    [MessageLogging(
        EventId = 1780,
        Level = LogLevel.Information,
        Message = "Deleting theme: {name}")]
    public static partial IGenericMessage DeletingTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when attempting to delete the default theme.
    /// </summary>
    [MessageLogging(
        EventId = 1781,
        Level = LogLevel.Warning,
        Message = "Cannot delete default theme: {name}")]
    public static partial IGenericMessage CannotDeleteDefaultTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when attempting to delete a built-in theme.
    /// </summary>
    [MessageLogging(
        EventId = 1782,
        Level = LogLevel.Warning,
        Message = "Cannot delete built-in theme: {name}")]
    public static partial IGenericMessage CannotDeleteBuiltInTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when a theme has been successfully deleted.
    /// </summary>
    [MessageLogging(
        EventId = 1783,
        Level = LogLevel.Information,
        Message = "Theme deleted: {name}")]
    public static partial IGenericMessage ThemeDeleted(ILogger logger, string name);

    /// <summary>
    /// Logs when setting a theme as default.
    /// </summary>
    [MessageLogging(
        EventId = 1784,
        Level = LogLevel.Trace,
        Message = "Setting default theme: {name}")]
    public static partial IGenericMessage SettingDefaultTheme(ILogger logger, string name);

    /// <summary>
    /// Logs when the default theme has been successfully set.
    /// </summary>
    [MessageLogging(
        EventId = 1785,
        Level = LogLevel.Information,
        Message = "Default theme set to: {name}")]
    public static partial IGenericMessage DefaultThemeSet(ILogger logger, string name);

    /// <summary>
    /// Logs when persisting a theme to the configuration database fails.
    /// </summary>
    [MessageLogging(
        EventId = 1786,
        Level = LogLevel.Error,
        Message = "Failed to persist theme '{name}': {error}")]
    public static partial IGenericMessage ThemePersistenceFailed(ILogger logger, string name, string error);
}
