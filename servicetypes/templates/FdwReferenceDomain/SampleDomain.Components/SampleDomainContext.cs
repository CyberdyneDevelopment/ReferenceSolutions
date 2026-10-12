using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using SampleDomain.Clients.Models;

namespace SampleDomain.Components;

/// <summary>What the provider hands its child content.</summary>
/// <remarks>
/// A record rather than loose parameters so adding a field later does not change every caller's
/// lambda signature. Loading and Error are separate: an empty list and a failed call are not the
/// same state, and a caller that cannot tell them apart shows "nothing here" for an outage.
/// </remarks>
/// <param name="Items">The loaded resources.</param>
/// <param name="Loading">Whether a load is in flight.</param>
/// <param name="Error">The failure message, when the last load failed.</param>
/// <param name="Reload">Runs the load again.</param>
[ExcludeFromCodeCoverage]
public sealed record SampleDomainContext(
    IReadOnlyList<SampleDomainPayload> Items,
    bool Loading,
    string? Error,
    Func<Task> Reload);
