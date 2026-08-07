using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Multitenancy.Clients.Models;

namespace Reference.Management.UI.Tailwind.Components.Domain.Tenants;

/// <summary>
/// Immutable context provided by <see cref="ProfileDropdownProvider"/> to its render template.
/// Carries user identity info, tenant list, and callbacks for tenant switching/default-setting.
/// </summary>
public sealed class ProfileDropdownContext
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Gets the authenticated user's display name.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Gets the authenticated user's email address.</summary>
    public string UserEmail { get; init; } = string.Empty;

    /// <summary>Gets the user's initials for avatar display (up to two characters).</summary>
    public string Initials { get; init; } = "?";

    // ── Active Tenant ──────────────────────────────────────────────────────────

    /// <summary>Gets the active tenant ID from the current JWT claim, or null when in cross-tenant mode.</summary>
    public Guid? ActiveTenantId { get; init; }

    /// <summary>Gets the active tenant name, or null when in cross-tenant mode.</summary>
    public string? ActiveTenantName { get; init; }

    /// <summary>Gets the user's role claim from the current JWT.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets whether the current session is in cross-tenant mode (no single active tenant).</summary>
    public bool IsCrossTenantMode { get; init; }

    // ── Tenant List ────────────────────────────────────────────────────────────

    /// <summary>Gets the list of tenants the user belongs to.</summary>
    public IReadOnlyList<TenantSummaryPayload> Tenants { get; init; } = [];

    /// <summary>Gets the default tenant ID for the user, if any.</summary>
    public Guid? DefaultTenantId { get; init; }

    /// <summary>
    /// Gets whether the tenant switcher should be shown.
    /// Hidden for single-tenant users; they still see profile/user info.
    /// </summary>
    public bool ShowTenantSwitcher { get; init; }

    /// <summary>Gets whether the "View all tenants" cross-tenant option should be visible.
    /// Requires the <c>tenants:view-all</c> permission in the <c>perm</c> claim.</summary>
    public bool ShowCrossTenantOption { get; init; }

    // ── Dropdown State ────────────────────────────────────────────────────────

    /// <summary>Gets whether the dropdown panel is open.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Gets whether an async operation is in progress.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Gets the most recent error message, or null when no error.</summary>
    public string? ErrorMessage { get; init; }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    /// <summary>Toggles the dropdown open/closed.</summary>
    public Func<Task> OnToggle { get; init; } = () => Task.CompletedTask;

    /// <summary>Closes the dropdown (e.g., on click-away).</summary>
    public Action OnClose { get; init; } = () => { };

    /// <summary>Switches the active tenant; re-mints the JWT via /connect/token.</summary>
    public Func<Guid, Task> OnSwitchTenant { get; init; } = _ => Task.CompletedTask;

    /// <summary>Sets the specified tenant as the user's default.</summary>
    public Func<Guid, Task> OnSetDefaultTenant { get; init; } = _ => Task.CompletedTask;

    /// <summary>Switches to cross-tenant mode (scope=cross_tenant, no single tenant).</summary>
    public Func<Task> OnEnterCrossTenantMode { get; init; } = () => Task.CompletedTask;
}
