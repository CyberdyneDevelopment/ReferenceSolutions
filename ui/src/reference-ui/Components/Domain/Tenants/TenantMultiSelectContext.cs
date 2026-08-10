using System;
using System.Collections.Generic;
using Fdw.Services.Multitenancy.Clients.Models;

namespace Reference.Ui.Components.Domain.Tenants;

/// <summary>
/// Immutable context provided by <see cref="TenantMultiSelectProvider"/> to its render template.
/// Exposes the list of available tenants and the current selection for presentation filtering
/// in cross-tenant data-viewing pages.
/// </summary>
/// <remarks>
/// This is a PRESENTATION filter only. Row-Level Security (RLS) enforces the real security
/// boundary in the database. This control narrows what the user SEES, not what they can access.
/// </remarks>
public sealed class TenantMultiSelectContext
{
    /// <summary>Gets all tenants available to the user.</summary>
    public IReadOnlyList<TenantSummaryPayload> AvailableTenants { get; init; } = [];

    /// <summary>Gets the currently selected tenant IDs (empty = all).</summary>
    public IReadOnlySet<Guid> SelectedTenantIds { get; init; } = new HashSet<Guid>();

    /// <summary>Gets whether the multi-select should be shown (only in cross-tenant mode).</summary>
    public bool IsVisible { get; init; }

    /// <summary>Gets whether a data load is in progress.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Gets the most recent error message, or null if no error.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Toggles selection of a single tenant. Selected → deselected, deselected → selected.</summary>
    public Action<Guid> OnToggleTenant { get; init; } = _ => { };

    /// <summary>Selects all tenants (clears the filter).</summary>
    public Action OnSelectAll { get; init; } = () => { };

    /// <summary>Clears all selections (deselects all).</summary>
    public Action OnClearAll { get; init; } = () => { };
}
