using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data.Ddl;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Ddl;

/// <summary>
/// Column definition for table creation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DdlColumnRequest
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the SQL data type name.</summary>
    public string SqlType { get; set; } = "NVARCHAR";

    /// <summary>Gets or sets the max length for string/binary types.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets whether the column is required (NOT NULL).</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the column is a primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Gets or sets whether the column is an identity column.</summary>
    public bool IsIdentity { get; set; }
}

/// <summary>
/// Request to execute DDL (create a table) on a connection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteDdlRequest
{
    /// <summary>Gets or sets the connection name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Gets or sets the table name to create.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column definitions.</summary>
    public System.Collections.Generic.List<DdlColumnRequest> Columns { get; set; } = [];
}

/// <summary>
/// Response from DDL execution.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteDdlResponse
{
    /// <summary>Gets or sets whether the execution was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the result or error message.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Endpoint to execute DDL statements (create table) on a connection.
/// Uses the FDW CreateTableCommand for safe, parameterized DDL execution.
/// Route: POST /connections/{name}/execute-ddl
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteDdlEndpoint : Endpoint<ExecuteDdlRequest, ExecuteDdlResponse>
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<ExecuteDdlEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteDdlEndpoint"/> class.
    /// </summary>
    public ExecuteDdlEndpoint(
        IConnectionProvider connectionProvider,
        IDataGateway dataGateway,
        ILogger<ExecuteDdlEndpoint>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _dataGateway = dataGateway;
        _logger = logger ?? NullLogger<ExecuteDdlEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/connections/{Name}/execute-ddl");
        Policies("datastores:write");
        Tags("Schema");
        Summary(s =>
        {
            s.Summary = "Execute DDL";
            s.Description = "Creates a table on the specified connection using the FDW CreateTableCommand pattern.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecuteDdlRequest req, CancellationToken ct)
    {
        DdlLog.ExecutingDdl(_logger, req.Name);

        if (string.IsNullOrWhiteSpace(req.TableName))
        {
            AddError("TableName is required");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (req.Columns.Count == 0)
        {
            AddError("At least one column is required");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var connectionResult = await _connectionProvider.Get<IDataConnection>(req.Name, ct);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            var command = new CreateTableCommand(req.TableName)
            {
                SchemaName = req.SchemaName,
                IfNotExists = true
            };

            foreach (var col in req.Columns)
            {
                var sqlDbType = ParseSqlDbType(col.SqlType);
                command.WithColumn(
                    col.Name,
                    sqlDbType,
                    maxLength: col.MaxLength,
                    isRequired: col.IsRequired,
                    isPrimaryKey: col.IsPrimaryKey,
                    isIdentity: col.IsIdentity);
            }

            var result = await _dataGateway.Execute<bool>(command, new DataStoreTarget(req.Name, req.SchemaName, req.TableName), ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                DdlLog.DdlExecuted(_logger, req.Name);
                await Send.OkAsync(new ExecuteDdlResponse
                {
                    Success = true,
                    Message = $"Table '{req.TableName}' created successfully"
                }, ct);
            }
            else
            {
                DdlLog.DdlExecutionFailed(_logger, req.Name, result.CurrentMessage ?? "Unknown error");
                await Send.ResponseAsync(new ExecuteDdlResponse
                {
                    Success = false,
                    Message = result.CurrentMessage ?? "DDL execution failed"
                }, 500, ct);
            }
        }
        catch (Exception ex)
        {
            DdlLog.DdlExecutionFailed(_logger, req.Name, ex.Message);
            await Send.ResponseAsync(new ExecuteDdlResponse
            {
                Success = false,
                Message = "An internal error occurred during DDL execution."
            }, 500, ct);
        }
    }

    private static SqlDbType ParseSqlDbType(string typeName)
    {
        if (Enum.TryParse<SqlDbType>(typeName, ignoreCase: true, out var result))
        {
            return result;
        }

        return typeName.ToUpperInvariant() switch
        {
            "INT" or "INTEGER" => SqlDbType.Int,
            "BIGINT" or "LONG" => SqlDbType.BigInt,
            "SMALLINT" or "SHORT" => SqlDbType.SmallInt,
            "TINYINT" or "BYTE" => SqlDbType.TinyInt,
            "BIT" or "BOOLEAN" or "BOOL" => SqlDbType.Bit,
            "NVARCHAR" or "STRING" or "TEXT" => SqlDbType.NVarChar,
            "VARCHAR" => SqlDbType.VarChar,
            "NCHAR" => SqlDbType.NChar,
            "CHAR" => SqlDbType.Char,
            "DATETIME" => SqlDbType.DateTime,
            "DATETIME2" => SqlDbType.DateTime2,
            "DATE" => SqlDbType.Date,
            "TIME" => SqlDbType.Time,
            "DATETIMEOFFSET" => SqlDbType.DateTimeOffset,
            "DECIMAL" or "NUMERIC" => SqlDbType.Decimal,
            "FLOAT" or "DOUBLE" => SqlDbType.Float,
            "REAL" => SqlDbType.Real,
            "UNIQUEIDENTIFIER" or "GUID" => SqlDbType.UniqueIdentifier,
            "VARBINARY" or "BINARY" => SqlDbType.VarBinary,
            "XML" => SqlDbType.Xml,
            _ => SqlDbType.NVarChar
        };
    }
}
