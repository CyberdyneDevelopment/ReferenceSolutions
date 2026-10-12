using System.Reflection;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// Builds a real <see cref="SqlException"/>, which SqlClient gives no public way to make: the exception,
/// its errors and their collection all have internal constructors. A test that needs the failure a server
/// answers with (an error number, the procedure it came from) has to make one, and a plain exception
/// would not carry the fields the code under test reads.
/// </summary>
/// <remarks>
/// Uses reflection over SqlClient's internals, written against Microsoft.Data.SqlClient 7.0.1
/// (<c>SqlError(int, byte, byte, string, string, string, int, Exception)</c>, the internal
/// <c>SqlErrorCollection.Add</c> and <c>SqlException.CreateException(SqlErrorCollection, string)</c>). A
/// SqlClient upgrade that changes those signatures fails here, by name, before it fails anywhere else.
/// </remarks>
internal static class TestSqlException
{
    private const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    /// <summary>Makes an exception carrying one error.</summary>
    /// <param name="number">The SQL error number.</param>
    /// <param name="message">The error's message.</param>
    /// <param name="procedure">The procedure the error came from; empty for an ad-hoc batch, as SQL Server reports it.</param>
    /// <returns>A <see cref="SqlException"/> whose <c>Number</c> and <c>Errors</c> read back as given.</returns>
    public static SqlException Create(int number, string message, string procedure = "")
        => Create((number, message, procedure));

    /// <summary>Makes an exception carrying several errors, in order.</summary>
    /// <param name="errors">The errors: number, message and the procedure each came from.</param>
    /// <returns>A <see cref="SqlException"/> whose <c>Errors</c> read back as given.</returns>
    public static SqlException Create(params (int Number, string Message, string Procedure)[] errors)
    {
        var collection = Required(
            typeof(SqlErrorCollection).GetConstructor(Instance, null, Type.EmptyTypes, null),
            "the SqlErrorCollection constructor").Invoke([]);

        var add = Required(
            typeof(SqlErrorCollection).GetMethod("Add", Instance, null, [typeof(SqlError)], null),
            "SqlErrorCollection.Add(SqlError)");
        var errorConstructor = Required(
            typeof(SqlError).GetConstructor(
                Instance,
                null,
                [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)],
                null),
            "the SqlError constructor (number, state, class, server, message, procedure, line, exception)");

        foreach (var (number, message, procedure) in errors)
        {
            add.Invoke(collection, [errorConstructor.Invoke([number, (byte)1, (byte)16, "test-server", message, procedure, 1, null])]);
        }

        var create = Required(
            typeof(SqlException).GetMethod("CreateException", Static, null, [typeof(SqlErrorCollection), typeof(string)], null),
            "SqlException.CreateException(SqlErrorCollection, string)");

        return (SqlException)Required(create.Invoke(null, [collection, "16.0"]), "the SqlException CreateException returned");
    }

    private static T Required<T>(T? value, string what)
        where T : class
        => value ?? throw new InvalidOperationException(
            $"SqlClient no longer has {what}; TestSqlException was written against Microsoft.Data.SqlClient 7.0.1 and needs updating for the version in use.");
}
