using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceEndpoints.ApiReference;

/// <summary>
/// The list of reference pages the signed-in session may open, kept open in the top right corner of every
/// reference page, under the sign-in buttons where the page has them.
/// </summary>
/// <remarks>
/// <para>
/// Read from <c>/reference/pages</c>, the same list the sign-in dialog offers, so it shows only what the
/// session may read. It stays hidden while signed out. Whether it is collapsed is the reader's choice and is
/// remembered in the browser; it opens by default. Everything the server returns is placed with
/// <c>textContent</c> and element properties, never as HTML.
/// </para>
/// <para>
/// The sign-in page draws the list from <see cref="Css"/>, <see cref="ListMarkup"/> and
/// <see cref="RenderScript"/> itself, refreshing it whenever the session changes. The other reference
/// pages carry <see cref="Markup"/>, which reads the list once and offers a Sign in link while signed out.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class ReferencePagesNav
{
    /// <summary>Gets the list's CSS, drawn from the reference page's own theme variables.</summary>
    public const string Css = """
        #fdw-pages{position:fixed;top:.75rem;right:1rem;z-index:999;box-sizing:border-box;width:min(18rem,80vw);
          max-height:calc(100vh - 5rem);overflow:auto;padding:.5rem .75rem;
          background:var(--scalar-background-2);color:var(--scalar-color-1);
          border:1px solid var(--scalar-border-color);border-radius:var(--scalar-border-radius,6px);
          font-family:var(--scalar-font,system-ui,sans-serif);font-size:.85rem}
        #fdw-session ~ #fdw-pages{top:3.5rem}
        #fdw-pages[hidden]{display:none}
        #fdw-pages summary{cursor:pointer;font-weight:600}
        #fdw-pages ul{margin:.5rem 0 .25rem;padding-left:1.1rem}
        #fdw-pages li{margin:.2rem 0}
        #fdw-pages a{color:var(--scalar-color-accent);text-decoration:none}
        #fdw-pages a:hover{text-decoration:underline}
        #fdw-pages a[aria-current=page]{color:var(--scalar-color-1);font-weight:600}
        #fdw-login-link{position:fixed;top:.75rem;right:1rem;z-index:1000;padding:.45rem .9rem;border-radius:4px;
          background:var(--scalar-button-1,var(--scalar-color-accent));color:var(--scalar-button-1-color,#fff);
          font-family:var(--scalar-font,system-ui,sans-serif);text-decoration:none}
        #fdw-login-link[hidden]{display:none}
        """;

    /// <summary>Gets the list's element, hidden until a signed-in session fills it.</summary>
    public const string ListMarkup =
        "<details id=\"fdw-pages\" hidden><summary>Reference pages</summary><ul></ul></details>";

    /// <summary>
    /// Gets the script that draws the list: <c>fdwRenderPages(data)</c> with the answer of
    /// <c>/reference/pages</c>, and the open state remembered between pages.
    /// </summary>
    public const string RenderScript = """
        function fdwRenderPages(data) {
          const box = document.getElementById('fdw-pages');
          if (!box) return;
          const list = box.querySelector('ul');
          while (list.firstChild) list.removeChild(list.firstChild);
          box.hidden = !(data && data.isSignedIn);
          if (box.hidden) return;
          const items = data.items || [];
          const here = location.pathname.replace(/\/$/, '');
          for (const link of items) {
            const anchor = document.createElement('a');
            anchor.href = link.url;
            anchor.textContent = link.title;
            try { if (new URL(link.url, location.origin).pathname.replace(/\/$/, '') === here) anchor.setAttribute('aria-current', 'page'); } catch { }
            const item = document.createElement('li');
            item.append(anchor);
            list.append(item);
          }
          box.querySelector('summary').textContent = 'Reference pages (' + items.length + ')';
        }
        (() => {
          const box = document.getElementById('fdw-pages');
          if (!box) return;
          try { box.open = localStorage.getItem('fdw-pages-open') !== '0'; } catch { box.open = true; }
          box.addEventListener('toggle', () => { try { localStorage.setItem('fdw-pages-open', box.open ? '1' : '0'); } catch { } });
        })();
        """;

    /// <summary>Gets the whole list for a reference page that has no sign-in panel of its own.</summary>
    public static string Markup { get; } =
        "<style>" + Css + "</style>"
        + "<a id=\"fdw-login-link\" href=\"/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/scalar/\" hidden>Sign in</a>"
        + ListMarkup
        + "<script>" + RenderScript
        + "(async () => { try { const response = await fetch('/" + FdwReferenceEndpoints.Endpoints.RoutePrefix
        + "/reference/pages', { credentials: 'same-origin', headers: { Accept: 'application/json' } });"
        + " const data = response.ok ? await response.json() : null; fdwRenderPages(data);"
        + " document.getElementById('fdw-login-link').hidden = !!(data && data.isSignedIn); } catch { } })();</script>";
}
