using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Console palette for the wizard's prompts and chrome. Hex constants are kept alongside the
/// <see cref="Color"/> values because markup strings take a literal hex/name token, not a
/// <see cref="Color"/> instance.
/// </summary>
/// <remarks>
/// The raydar original this is ported from applies the Rayburn brand palette, which is a
/// Rayburn-workspace fact and not a reference-stack one. This repo has no brand of its own, so the
/// values below are picked for a terminal rather than lifted from a product: a blue that stays
/// legible on both light and dark backgrounds, and a red louder than any UI "destructive" token
/// because a plain black terminal background eats the softer ones.
/// </remarks>
internal static class ReferenceTheme
{
    public const string BrandHex = "#3b8eea";
    public const string AccentHex = "#2aa198";
    public const string BorderHex = "#444444";
    public const string DangerHex = "#ff5c5c";

    public static readonly Color Brand = Color.FromHex(BrandHex);
    public static readonly Color Accent = Color.FromHex(AccentHex);
    public static readonly Color Border = Color.FromHex(BorderHex);
    public static readonly Color Muted = Color.Grey;
    public static readonly Color Danger = Color.FromHex(DangerHex);

    public static readonly Style Heading = new(Brand, decoration: Decoration.Bold);
    public static readonly Style Selected = new(Accent, decoration: Decoration.Bold);
    public static readonly Style Done = new(Accent);
    public static readonly Style Pending = new(Muted);

    public static string Markup(string text) => $"[{BrandHex}]{text}[/]";

    public static string AccentMarkup(string text) => $"[{AccentHex}]{text}[/]";

    public static string DangerMarkup(string text) => $"[bold {DangerHex}]{text}[/]";

    public static Rule Rule(string title) => new(Markup(title))
    {
        Justification = Justify.Left,
        Style = new Style(Border),
    };
}
