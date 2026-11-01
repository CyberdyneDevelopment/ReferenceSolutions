using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Tests a connection configuration of any type without saving it. Used by the connection wizard to check
/// connectivity before saving.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestConnectionConfigEndpoint : TestConnectionConfigEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestConnectionConfigEndpoint"/> class.
    /// </summary>
    public TestConnectionConfigEndpoint(IConnectionProvider connectionProvider, ILogger<TestConnectionConfigEndpoint> logger)
        : base(logger, connectionProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Test a connection configuration";
            s.Description = "Tests connectivity for a connection of any type using an in-memory configuration, without saving it. Used by the connection wizard.";
        });
        Tags("Connections");
    }
}
