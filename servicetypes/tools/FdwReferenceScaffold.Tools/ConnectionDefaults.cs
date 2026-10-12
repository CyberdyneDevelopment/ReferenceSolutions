namespace FdwReferenceScaffold.Tools;

/// <summary>Port prompt shared by the Platform Configuration and Other Connections steps.</summary>
internal static class ConnectionDefaults
{
    private const string MsSqlDefaultPort = "1433";
    private const string PostgreSqlDefaultPort = "5432";

    /// <summary>
    /// Prompts for the port, labeled and defaulted for the given connection type. Only swaps the
    /// suggested default when the current value is still one of the two standard ports -- a value
    /// the user already customized (a nonstandard port) survives switching ConnectionType back
    /// and forth.
    /// </summary>
    public static string PromptPort(string connectionType, string currentPort)
    {
        var isPostgreSql = connectionType == "PostgreSql";
        var suggested = isPostgreSql ? PostgreSqlDefaultPort : MsSqlDefaultPort;
        var untouched = currentPort == MsSqlDefaultPort || currentPort == PostgreSqlDefaultPort;
        var effectiveDefault = untouched ? suggested : currentPort;

        var label = isPostgreSql ? "PostgreSQL port" : "SQL Server port";
        return Prompts.Text(label, effectiveDefault);
    }
}
