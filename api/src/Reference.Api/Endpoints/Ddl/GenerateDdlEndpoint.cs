using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Ddl;

/// <summary>
/// Request to generate DDL for a connection's schema.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GenerateDdlRequest
{
    /// <summary>Gets or sets the connection name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets optional schema filter.</summary>
    public string? SchemaFilter { get; set; }
}

/// <summary>
/// Response containing generated DDL.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GenerateDdlResponse
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated DDL script.</summary>
    public string Ddl { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of statements.</summary>
    public int StatementCount { get; set; }
}

/// <summary>
/// Endpoint to generate DDL from a connection's discovered schema.
/// Route: POST /connections/{name}/generate-ddl
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GenerateDdlEndpoint : Endpoint<GenerateDdlRequest, GenerateDdlResponse>
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly ILogger<GenerateDdlEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateDdlEndpoint"/> class.
    /// </summary>
    public GenerateDdlEndpoint(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        ILogger<GenerateDdlEndpoint>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<GenerateDdlEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/connections/{Name}/generate-ddl");
        Policies("datastores:write");
        Tags("Schema");
        Summary(s =>
        {
            s.Summary = "Generate DDL from schema";
            s.Description = "Discovers the schema of a connection and generates DDL (CREATE TABLE) statements.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GenerateDdlRequest req, CancellationToken ct)
    {
        DdlLog.GeneratingDdl(_logger, req.Name);

        var connectionResult = await _connectionProvider.Get<IDataConnection>(req.Name, ct);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var allConfigsResult = await _configProvider.Get(ct).ConfigureAwait(false);
        var config = allConfigsResult.IsSuccess
            ? (allConfigsResult.Value ?? []).FirstOrDefault(c => string.Equals(c.Name, req.Name, StringComparison.OrdinalIgnoreCase))
            : null;

        var connectionType = config?.ConnectionType ?? config?.ServiceOptionType;
        if (string.IsNullOrEmpty(connectionType))
        {
            AddError("Unable to determine connection type");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var connType = ConnectionTypes.ByName(connectionType);
        if (connType == ConnectionTypes.NotFound || connType is not ISchemaDiscovery schemaDiscovery)
        {
            AddError($"Schema discovery not supported for connection type '{connectionType}'");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var discoveryResult = await schemaDiscovery.DiscoverSchema(
            connectionResult.Value, DataStoreDiscoveryOptions.Default, ct).ConfigureAwait(false);

        if (!discoveryResult.IsSuccess || discoveryResult.Value == null)
        {
            DdlLog.DdlGenerationFailed(_logger, req.Name, discoveryResult.CurrentMessage ?? "Schema discovery failed");
            AddError(discoveryResult.CurrentMessage ?? "Schema discovery failed");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        try
        {
            var containers = discoveryResult.Value;
            var ddl = GenerateCreateTableStatements(containers, connectionType, req.SchemaFilter);
            var statementCount = ddl.Split("CREATE TABLE", StringSplitOptions.None).Length - 1;

            DdlLog.DdlGenerated(_logger, req.Name);

            await Send.OkAsync(new GenerateDdlResponse
            {
                ConnectionName = req.Name,
                Ddl = ddl,
                StatementCount = statementCount
            }, ct);
        }
        catch (Exception ex)
        {
            DdlLog.DdlGenerationFailed(_logger, req.Name, ex.Message);
            AddError("DDL generation failed");
            await Send.ErrorsAsync(500, ct);
        }
    }

    private static string GenerateCreateTableStatements(
        System.Collections.Generic.IReadOnlyList<IStorageContainer> containers,
        string connectionType,
        string? schemaFilter)
    {
        var sb = new System.Text.StringBuilder();
        var quoteOpen = string.Equals(connectionType, "PostgreSql", StringComparison.OrdinalIgnoreCase) ? "\"" : "[";
        var quoteClose = string.Equals(connectionType, "PostgreSql", StringComparison.OrdinalIgnoreCase) ? "\"" : "]";

        var groups = containers
            .Where(c => !string.Equals(c.ContainerType.Name, "View", StringComparison.Ordinal))
            .GroupBy(c => c.Path.PathValue, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(schemaFilter) &&
                !string.Equals(group.Key, schemaFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sb.AppendLine($"-- Schema: {group.Key}");

            foreach (var container in group)
            {
                sb.AppendLine($"CREATE TABLE {quoteOpen}{group.Key}{quoteClose}.{quoteOpen}{container.Name}{quoteClose} (");

                var fields = container.Schema.Fields;
                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var nullable = field.IsNullable ? "NULL" : "NOT NULL";
                    var identity = field.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
                    var separator = i < fields.Count - 1 ? "," : string.Empty;

                    sb.AppendLine($"    {quoteOpen}{field.Name}{quoteClose} {field.FieldType.TypeName}{identity} {nullable}{separator}");
                }

                var pkFields = container.Schema.GetIdentityFields().ToList();
                if (pkFields.Count > 0)
                {
                    var pkCols = string.Join(", ", pkFields.Select(f => $"{quoteOpen}{f.Name}{quoteClose}"));
                    sb.AppendLine($"    ,CONSTRAINT {quoteOpen}PK_{container.Name}{quoteClose} PRIMARY KEY ({pkCols})");
                }

                sb.AppendLine(");");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
