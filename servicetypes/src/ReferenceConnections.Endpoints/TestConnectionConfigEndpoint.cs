using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceConnections.Endpoints.Logging;
using ReferenceConnections.MsSql;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// Tests a connection configuration in-memory without persisting.
/// Used by the connection wizard to validate connectivity before saving.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestConnectionConfigEndpoint : Endpoint<CreateConnectionRequest, TestConnectionResponse>
{
    private readonly IMsSqlConnectionFactory _connectionFactory;
    private readonly ILogger<TestConnectionConfigEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConnectionConfigEndpoint"/> class.
    /// </summary>
    public TestConnectionConfigEndpoint(
        IMsSqlConnectionFactory connectionFactory,
        ILogger<TestConnectionConfigEndpoint> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger ?? NullLogger<TestConnectionConfigEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/connections/test-config");
#if DEVELOP
        AllowAnonymous();
#else
        // Why: test-config builds a live connection from caller-supplied credentials. That's a
        // privileged operation (probes a network endpoint with user-controlled inputs) and is
        // gated by the same write permission used to create/update connections. Viewer is denied.
        Policies("connections:write");
#endif
        Summary(s =>
        {
            s.Summary = "Test a connection configuration";
            s.Description = "Tests connectivity using an in-memory configuration without persisting. Used by the connection wizard.";
        });
        Tags("Connections");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateConnectionRequest req, CancellationToken ct)
    {
        var name = req.Name ?? "Untitled";
        ConnectionLog.TestingConnectionConfig(_logger, name);

        // Why: transient in-memory config for factory test only — not persisted. Id is minted via
        // CreateVersion7() for consistency with the write path. Name is on ConnectionConfiguration
        // (the parent) after the config split; only typed-body fields are needed here.
        var config = new MsSqlConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Server = req.Server,
            Port = req.Port,
            Database = req.Database,
            AuthenticationType = req.AuthenticationType,
            AdditionalProperties = new Dictionary<string, string?>(req.Authentication, StringComparer.OrdinalIgnoreCase),
            TrustServerCertificate = req.TrustServerCertificate,
            Encrypt = req.Encrypt
        };

        var connectionResult = _connectionFactory.Create(config);

        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            var buildMessage = connectionResult.CurrentMessage ?? "Failed to build connection from configuration";
            ConnectionLog.ConnectionConfigBuildFailed(_logger, name, buildMessage);

            await Send.OkAsync(new TestConnectionResponse
            {
                Name = name,
                Success = false,
                Message = buildMessage
            }, ct).ConfigureAwait(false);
            return;
        }

        var connection = connectionResult.Value;
        var testResult = await connection.TestConnection(ct).ConfigureAwait(false);
        var success = testResult.IsSuccess;
        var message = success
            ? "Connection successful"
            : testResult.CurrentMessage ?? "Connection test failed";

        if (success)
        {
            ConnectionLog.ConnectionConfigTestSucceeded(_logger, name);
        }
        else
        {
            ConnectionLog.ConnectionConfigTestFailed(_logger, name, message);
        }

        await Send.OkAsync(new TestConnectionResponse
        {
            Name = name,
            Success = success,
            Message = message
        }, ct).ConfigureAwait(false);
    }
}
