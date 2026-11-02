using Fdw.Services.Identity;
﻿using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary><c>GET identities</c> — lists the configured managed identities, one page at a time.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListIdentitiesEndpoint : ListIdentitiesEndpointBase
{
    private readonly IdentityServiceConfigurationProvider _identities;

    /// <summary>Initializes a new instance of the <see cref="ListIdentitiesEndpoint"/> class.</summary>
    /// <param name="logger">The logger for this endpoint.</param>
    /// <param name="identities">Reads the configured identities.</param>
    public ListIdentitiesEndpoint(ILogger<ListIdentitiesEndpoint> logger, IdentityServiceConfigurationProvider identities)
        : base(logger) => _identities = identities;

    /// <inheritdoc />
    protected override IdentityServiceConfigurationProvider Identities => _identities;
}
