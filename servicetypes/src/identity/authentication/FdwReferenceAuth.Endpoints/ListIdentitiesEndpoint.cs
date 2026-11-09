using Fdw.Services.Identity;
using Fdw.Services.Identity.Abstractions;
﻿using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary><c>GET identities</c> — lists the configured managed identities, one page at a time.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListIdentitiesEndpoint : ListIdentitiesEndpointBase
{
    private readonly IIdentityServiceConfigurationProvider _identities;

    /// <summary>Initializes a new instance of the <see cref="ListIdentitiesEndpoint"/> class.</summary>
    /// <param name="logger">The logger for this endpoint.</param>
    /// <param name="identities">Reads the configured identities.</param>
    public ListIdentitiesEndpoint(ILogger<ListIdentitiesEndpoint> logger, IIdentityServiceConfigurationProvider identities)
        : base(logger) => _identities = identities;

    /// <inheritdoc />
    protected override IIdentityServiceConfigurationProvider Identities => _identities;
}
