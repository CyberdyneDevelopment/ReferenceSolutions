using Fdw.Services.Hosts.Abstractions;
﻿using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceEndpoints.Configuration;

/// <summary>
/// What this surface says when it refuses a caller.
/// </summary>
/// <remarks>
/// A bare 401 or 403 leaves the framework before any handler runs, so it carries no body unless
/// something writes one. These are the four values that body was built from, and they were string
/// literals in a host's Program.cs: the code and the sentence for each of the two refusals. What a
/// surface tells an unauthenticated caller differs per deployment — how much to reveal, which
/// wording, whose voice — so they are a row rather than constants compiled into a host.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthChallenge", ServiceType = "Host")]
public sealed partial class AuthChallengeHostConfiguration : IAuthChallengeImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the error code returned when the caller is not authenticated.</summary>
    public string UnauthorizedCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the message returned when the caller is not authenticated.</summary>
    public string UnauthorizedMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the error code returned when the caller is authenticated but not permitted.</summary>
    public string ForbiddenCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the message returned when the caller is authenticated but not permitted.</summary>
    public string ForbiddenMessage { get; set; } = string.Empty;
}
