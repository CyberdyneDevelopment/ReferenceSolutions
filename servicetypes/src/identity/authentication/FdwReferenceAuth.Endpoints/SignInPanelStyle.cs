using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The sign-in panel's style, drawn from the reference page's own theme variables.</summary>
[ExcludeFromCodeCoverage]
public static class SignInPanelStyle
{
    /// <summary>Gets the panel's CSS.</summary>
    public const string Css = """
        #fdw-session{position:fixed;top:.75rem;right:1rem;z-index:1000;display:flex;gap:.5rem}
        #fdw-session button{padding:.45rem .9rem;border-radius:4px;border:0;
          cursor:pointer;background:var(--scalar-button-1,var(--scalar-color-accent));color:var(--scalar-button-1-color,#fff);
          font-family:var(--scalar-font,system-ui,sans-serif)}
        #fdw-session #fdw-signout{background:var(--scalar-background-1);color:var(--scalar-color-1);
          border:1px solid var(--scalar-border-color)}
        #fdw-session button[hidden]{display:none}
        #fdw-signin{box-sizing:border-box;width:min(28rem,92vw);padding:1.25rem 1.5rem;
          background:var(--scalar-background-2);color:var(--scalar-color-1);
          border:1px solid var(--scalar-border-color);border-radius:var(--scalar-border-radius,6px);
          font-family:var(--scalar-font,system-ui,sans-serif)}
        #fdw-signin::backdrop{background:rgba(0,0,0,.45)}
        #fdw-signin h2{margin:0 0 .5rem;font-size:1.15rem}
        #fdw-signin #fdw-signin-close{position:absolute;top:.5rem;right:.6rem;padding:.1rem .45rem;font-size:1.2rem;
          background:transparent;color:var(--scalar-color-2)}
        #fdw-signin p{margin:.25rem 0 .75rem;color:var(--scalar-color-2)}
        #fdw-signin [data-tone=error]{color:var(--scalar-color-red)}
        #fdw-signin form,#fdw-signin .fdw-row{display:flex;flex-wrap:wrap;gap:.5rem;align-items:center;margin:.5rem 0}
        #fdw-signin input{flex:1 1 12rem;padding:.45rem .6rem;border-radius:4px;
          border:1px solid var(--scalar-border-color);background:var(--scalar-background-1);color:var(--scalar-color-1)}
        #fdw-signin button{padding:.45rem .9rem;border-radius:4px;border:0;cursor:pointer;
          background:var(--scalar-button-1,var(--scalar-color-accent));color:var(--scalar-button-1-color,#fff)}
        #fdw-signin button[data-kind=secondary]{background:transparent;color:var(--scalar-color-1);
          border:1px solid var(--scalar-border-color)}
        #fdw-signin ul{margin:.25rem 0 .75rem;padding-left:1.25rem}
        #fdw-signin a{color:var(--scalar-color-accent)}
        """;
}
