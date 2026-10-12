using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FdwReferenceConfigurationSchema.Tools;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Patches the configurationSchema.json dotnet new just generated: adds a Connections + empty
/// DataStores entry for every "other connection" from the wizard (the template itself has no
/// mechanism for a variable-length connection list), and drops in real introspected Paths for
/// any DataStore a live connection was reached for.
/// </summary>
internal static class ConfigurationSchemaPatcher
{
    /// <summary>
    /// The prefix FdwReferenceSecretManagers.EnvironmentVariable reads keys under. Fixed, not a
    /// symbol: fdw-ref-api-host has no SecretPrefix parameter, unlike the raydar template this is
    /// ported from, and the generated schema names the manager "EnvSecrets" literally.
    /// </summary>
    public const string EnvSecretPrefix = "FDW_SECRET_";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// Where fdw-ref-api-host leaves configurationSchema.json: <c>&lt;output&gt;/&lt;name&gt;/</c>.
    /// The template's sourceName is the project folder itself (not a src/ subtree as in raydar),
    /// and preferNameDirectory makes the no -o case create the same &lt;name&gt; level, so one
    /// join covers both.
    /// </summary>
    public static string ResolvePath(WizardAnswers answers)
    {
        var topLevel = string.IsNullOrWhiteSpace(answers.OutputDirectory) ? answers.Name : answers.OutputDirectory;
        return Path.Combine(topLevel, answers.Name, "configurationSchema.json");
    }

    public static bool AddOtherConnections(string path, IReadOnlyList<ConnectionAnswer> otherConnections)
    {
        if (otherConnections.Count == 0)
        {
            return false;
        }

        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidOperationException($"{path} did not parse as a JSON object.");
        var schema = root["ConfigurationSchema"] as JsonObject
            ?? throw new InvalidOperationException($"{path} has no ConfigurationSchema object.");
        var connections = schema["Connections"] as JsonArray
            ?? throw new InvalidOperationException($"{path} has no ConfigurationSchema.Connections array.");
        var dataStores = schema["DataStores"] as JsonArray
            ?? throw new InvalidOperationException($"{path} has no ConfigurationSchema.DataStores array.");

        // Why no SecretManagers block is synthesized here, unlike the raydar original: this
        // template's schema never declares one. The manager is registered by referencing
        // FdwReferenceSecretManagers.<choice>.Registration and resolved through
        // PlatformServices.SecretManager, and the connections that need it name "EnvSecrets"
        // directly. Writing a SecretManagers array into this schema would add a section the
        // reference gateway does not read.
        foreach (var connection in otherConnections)
        {
            connections.Add(BuildConnectionNode(connection));
            dataStores.Add(BuildDataStoreNode(connection.Name, connection.ConnectionType, paths: []));
        }

        File.WriteAllText(path, root.ToJsonString(WriteOptions));
        return true;
    }

    public static void ApplyIntrospectedPaths(string path, string dataStoreName, List<SchemaPath> paths)
    {
        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidOperationException($"{path} did not parse as a JSON object.");
        var dataStores = root["ConfigurationSchema"]?["DataStores"] as JsonArray
            ?? throw new InvalidOperationException($"{path} has no ConfigurationSchema.DataStores array.");

        foreach (var node in dataStores)
        {
            if (node is JsonObject dataStore && string.Equals((string?)dataStore["Name"], dataStoreName, StringComparison.Ordinal))
            {
                dataStore["Paths"] = JsonSerializer.SerializeToNode(paths, WriteOptions);
                break;
            }
        }

        File.WriteAllText(path, root.ToJsonString(WriteOptions));
    }

    private static JsonObject BuildConnectionNode(ConnectionAnswer connection)
    {
        var configuration = new JsonObject();

        if (connection.IsServerBacked)
        {
            configuration["Server"] = connection.DbServer;
            configuration["Database"] = connection.DbName;
            configuration["Port"] = int.TryParse(connection.DbPort, out var port) ? JsonValue.Create(port) : JsonValue.Create(connection.DbPort);
            configuration["AuthenticationType"] = connection.AuthenticationType;
            configuration["TrustServerCertificate"] = true;

            if (connection.NeedsSecret)
            {
                configuration["AdditionalProperties"] = new JsonObject
                {
                    ["Username"] = connection.DbUser,
                    ["SecretManagerName"] = "EnvSecrets",
                    ["SecretKeyName"] = connection.SecretKeyName,
                };
            }
        }
        else if (connection.ConnectionType == "Sqlite")
        {
            configuration["DataSource"] = connection.ConfigPath;
        }
        else
        {
            configuration["Root"] = connection.ConfigPath;
        }

        return new JsonObject
        {
            ["Name"] = connection.Name,
            ["Implementation"] = connection.ConnectionType,
            ["Configuration"] = configuration,
        };
    }

    private static JsonObject BuildDataStoreNode(string name, string implementation, List<SchemaPath> paths) => new()
    {
        ["Name"] = name,
        ["Implementation"] = implementation,
        ["Paths"] = JsonSerializer.SerializeToNode(paths, WriteOptions),
    };
}
