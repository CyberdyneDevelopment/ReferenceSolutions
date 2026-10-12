using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using FdwReferenceConfigurationSchema.Tools;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Attempts a real connection for one DataStore and, if it succeeds, runs the same introspection
/// raydar-schema uses to fill in its Paths array — so a connection given real, resolvable values
/// doesn't have to be introspected by hand as a separate follow-up step.
/// </summary>
internal static class DatabaseIntrospector
{
    public static bool LooksLikePlaceholder(string value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Contains("${", StringComparison.Ordinal)
        || value.Contains("{your-", StringComparison.Ordinal);

    public static async Task<IntrospectionResult> TryIntrospect(
        string connectionType,
        string server,
        string database,
        string port,
        string authenticationType,
        string user,
        string? password)
    {
        if (connectionType != "MsSql")
        {
            return IntrospectionResult.Skipped($"automatic introspection isn't available yet for {connectionType}");
        }

        if (LooksLikePlaceholder(server) || LooksLikePlaceholder(database))
        {
            return IntrospectionResult.Skipped("server/database still look like placeholders");
        }

        if (authenticationType == "SqlAuth" && string.IsNullOrEmpty(password))
        {
            return IntrospectionResult.Skipped("SqlAuth needs a password, and none was resolved from the environment");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = string.IsNullOrWhiteSpace(port) ? server : $"{server},{port}",
            InitialCatalog = database,
            TrustServerCertificate = true,
            ConnectTimeout = 5,
        };

        switch (authenticationType)
        {
            case "SqlAuth":
                builder.UserID = user;
                builder.Password = password;
                break;
            case "ManagedIdentity":
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;
                break;
            case "EntraId":
            case "AzureCli":
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                break;
            case "WindowsAuth":
                builder.IntegratedSecurity = true;
                break;
            default:
                return IntrospectionResult.Skipped($"unrecognized authentication type {authenticationType}");
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(timeout.Token).ConfigureAwait(false);
            var paths = await SchemaGenerator.Generate(connection, timeout.Token).ConfigureAwait(false);
            return IntrospectionResult.Success(paths);
        }
        catch (SqlException ex)
        {
            return IntrospectionResult.Skipped($"connection failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return IntrospectionResult.Skipped("connection timed out");
        }
    }
}
