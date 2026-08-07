using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to test connectivity to a configured connection.
/// Uses the base implementation which tests via IGenericConnection.TestConnection()
/// and records results to ops.ConnectionHealthCheck.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TestConnectionEndpoint : Fdw.Services.Connections.Endpoints.TestConnectionEndpoint
{
    /// <inheritdoc />
    public TestConnectionEndpoint(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider provider,
        IConnectionHealthService healthService,
        ILogger<TestConnectionEndpoint> logger)
        : base(connectionProvider, provider, healthService, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Connections");
#if DEVELOP
        AllowAnonymous();
#endif
    }
}
