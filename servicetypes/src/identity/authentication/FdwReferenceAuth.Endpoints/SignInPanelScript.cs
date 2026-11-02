using System;
using System.Diagnostics.CodeAnalysis;
using FdwReferenceEndpoints.ApiReference;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The sign-in panel's script.</summary>
/// <remarks>
/// <para>
/// Everything the server returns is placed with <c>textContent</c> and element properties, never as
/// HTML, so a flow or page name cannot inject markup.
/// </para>
/// <para>
/// The page's URL is sent as-is as the return URL: the reference page is served with a trailing slash
/// (<c>/api/v1/scalar/</c>), and a flow's <c>ReturnUrls</c> may list it with or without it
/// (<c>AuthenticationFlow.AllowsReturnUrl</c>); anything else about the URL must match exactly.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class SignInPanelScript
{
    private const string ApiBasePlaceholder = "__FDW_API_BASE__";
    private const string PagesRenderPlaceholder = "__FDW_PAGES_RENDER__";

    private const string Template = """
        __FDW_PAGES_RENDER__
        (() => {
          const api = '__FDW_API_BASE__';
          const page = location.origin + location.pathname;
          const title = document.getElementById('fdw-signin-title');
          const message = document.getElementById('fdw-signin-message');
          const body = document.getElementById('fdw-signin-body');
          const dialog = document.getElementById('fdw-signin');
          const opener = document.getElementById('fdw-signin-open');
          const signOutTop = document.getElementById('fdw-signout');
          const open = () => { if (!dialog.open) dialog.showModal(); };
          opener.addEventListener('click', open);
          signOutTop.addEventListener('click', () => signOut());
          document.getElementById('fdw-signin-close').addEventListener('click', () => dialog.close());

          const say = (text, tone) => {
            message.textContent = text;
            if (tone) message.dataset.tone = tone; else delete message.dataset.tone;
          };
          const clear = () => { while (body.firstChild) body.removeChild(body.firstChild); };
          const make = (tag, props, ...children) => {
            const node = Object.assign(document.createElement(tag), props);
            for (const child of children) node.append(child);
            return node;
          };

          async function csrfToken() {
            try {
              const response = await fetch(api + '/auth/session/csrf', { credentials: 'same-origin', headers: { Accept: 'application/json' } });
              return response.ok ? ((await response.json()).token || null) : null;
            } catch { return null; }
          }

          async function call(method, path, payload) {
            const headers = payload ? { 'Content-Type': 'application/json' } : {};
            if (method !== 'GET') {
              const token = await csrfToken();
              if (token) headers['X-CSRF-Token'] = token;
            }
            const response = await fetch(api + path, {
              method,
              credentials: 'same-origin',
              headers,
              body: payload ? JSON.stringify(payload) : undefined,
            });
            let data = null;
            try { data = await response.json(); } catch { data = null; }
            return { status: response.status, data };
          }

          // The callback's error codes, as the console reads them: only access_denied is a refusal.
          const callbackErrors = {
            access_denied: 'The identity provider refused the sign-in.',
            server_error: 'The sign-in was accepted, but the server could not finish it. Your credentials were not refused. Try again, and tell an administrator if it keeps happening.',
            login_required: 'The sign-in took too long. Start again.',
          };

          const signInErrors = {
            401: 'Those credentials were rejected.',
            403: 'That sign-in is switched off.',
            404: 'That sign-in is not set up.',
            500: 'The server could not complete the sign-in. Try again, and tell an administrator if it keeps happening.',
          };
          const signInError = status => signInErrors[status] || ('The sign-in failed (' + status + ').');

          const stripQuery = () => history.replaceState(null, '', location.pathname);

          async function start() {
            const query = new URLSearchParams(location.search);
            const error = query.get('error');
            const authCode = query.get('auth_code');
            if (error || authCode) { stripQuery(); open(); }

            if (error) {
              const known = callbackErrors[error] || ('The sign-in ended with an error this page does not recognise (' + error + ').');
              const detail = query.get('error_description');
              say(detail ? known + ' ' + detail : known, 'error');
            }

            if (authCode) {
              const redeemed = await call('POST', '/auth/redeem', { authCode });
              if (redeemed.status === 200) { signedInOrOut(); return; }
              say(signInError(redeemed.status), 'error');
            }

            await refresh();
          }

          async function refresh() {
            const pages = await call('GET', '/reference/pages');
            if (pages.status !== 200 || !pages.data) {
              say('The reference pages could not be listed (' + pages.status + ').', 'error');
              return;
            }

            fdwRenderPages(pages.data);
            opener.textContent = pages.data.isSignedIn ? 'Signed in' : 'Sign in';
            signOutTop.hidden = !pages.data.isSignedIn;
            if (pages.data.isSignedIn) showSignedIn(pages.data.items);
            else await showSignIn();
          }

          function showSignedIn(links) {
            clear();
            title.textContent = 'Signed in';
            if (!message.dataset.tone) say('Open a reference below. "Try it" on each one is already signed in as you.');

            const list = make('ul', {});
            for (const link of links) list.append(make('li', {}, make('a', { href: link.url, textContent: link.title })));

            const signOutButton = make('button', { type: 'button', textContent: 'Sign out', onclick: signOut });
            signOutButton.dataset.kind = 'secondary';
            body.append(list, make('div', { className: 'fdw-row' }, signOutButton));
          }

          async function signOut() {
            signOutTop.disabled = true;
            const result = await call('POST', '/auth/logout');
            signOutTop.disabled = false;
            if (result.status >= 400) {
              say('Signing out failed (' + result.status + ').', 'error');
              open();
              return;
            }

            signedInOrOut();
          }

          // The page is rendered with this session's "Try it" token (FDW-1100), so signing in or out renders it again
          // rather than redrawing the panel over a page whose Authentication box still holds the old state.
          function signedInOrOut() {
            location.reload();
          }

          async function showSignIn() {
            clear();
            title.textContent = 'Sign in';
            const flows = await call('GET', '/auth/sign-in-flows?returnUrl=' + encodeURIComponent(page));
            if (flows.status !== 200 || !flows.data) {
              say('The sign-in options could not be loaded (' + flows.status + ').', 'error');
              return;
            }

            if (flows.data.items.length === 0) {
              if (!message.dataset.tone) say('No sign-in is set up for this page. An API key still works in the Authentication box of each reference.');
              return;
            }

            for (const flow of flows.data.items) body.append(flow.kind === 'external' ? externalButton(flow) : passwordForm(flow));
          }

          function externalButton(flow) {
            const button = make('button', { type: 'button', textContent: 'Sign in with ' + flow.displayName, onclick: () => redirect(flow) });
            return make('div', { className: 'fdw-row' }, button);
          }

          async function redirect(flow) {
            const result = await call('POST', '/auth/oidc/redirect', { flowName: flow.name, returnUrl: page });
            const target = result.data && result.data.redirectUrl;
            if (result.status !== 200 || !target) {
              say(signInError(result.status), 'error');
              return;
            }

            location.assign(target);
          }

          function passwordForm(flow) {
            const username = make('input', { name: 'username', autocomplete: 'username', placeholder: 'Username', required: true });
            const password = make('input', { name: 'password', type: 'password', autocomplete: 'current-password', placeholder: 'Password', required: true });
            const form = make('form', {}, username, password, make('button', { type: 'submit', textContent: 'Sign in' }));
            form.addEventListener('submit', async event => {
              event.preventDefault();
              const result = await call('POST', '/auth/login', { username: username.value, password: password.value, flowName: flow.name });
              if (result.status === 200) {
                signedInOrOut();
                return;
              }

              if (result.data && result.data.passwordChangeRequired) {
                showChangePassword(flow, username.value, password.value);
                return;
              }

              say(signInError(result.status), 'error');
            });
            return form;
          }

          function showChangePassword(flow, username, current) {
            clear();
            title.textContent = 'Change your password';
            say('Your password has to be changed before you can sign in.');

            const next = make('input', { type: 'password', autocomplete: 'new-password', placeholder: 'New password', required: true });
            const form = make('form', {}, next, make('button', { type: 'submit', textContent: 'Change password' }));
            form.addEventListener('submit', async event => {
              event.preventDefault();
              const changed = await call('POST', '/auth/password', { username, currentPassword: current, newPassword: next.value });
              if (changed.status >= 400) {
                say('The password could not be changed (' + changed.status + ').', 'error');
                return;
              }

              const result = await call('POST', '/auth/login', { username, password: next.value, flowName: flow.name });
              if (result.status === 200) {
                signedInOrOut();
                return;
              }

              say(signInError(result.status), 'error');
            });
            body.append(form);
          }

          start();
        })();
        """;

    /// <summary>Gets the script for a host whose API is served under <paramref name="apiBase"/>.</summary>
    /// <param name="apiBase">The API's base path, for example <c>/api/v1</c>.</param>
    /// <returns>The script.</returns>
    public static string For(string apiBase) => Template
        .Replace(PagesRenderPlaceholder, ReferencePagesNav.RenderScript, StringComparison.Ordinal)
        .Replace(ApiBasePlaceholder, apiBase, StringComparison.Ordinal);
}
