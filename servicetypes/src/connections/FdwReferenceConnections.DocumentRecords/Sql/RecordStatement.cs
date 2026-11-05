using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Sql;

/// <summary>
/// One statement the SQL Server DocumentRecord connection runs (FDW-3731): its text and values, and, for a read that lists what belongs to
/// one entity, the entity that must be the organization's. Such a read's text starts with a <c>SELECT</c> of 1 or 0 for that entity.
/// </summary>
internal sealed class RecordStatement
{
    private RecordStatement(string name, string text, IReadOnlyList<SqlParameter> parameters, string? mustFind, Guid mustFindId)
    {
        Name = name;
        Text = text;
        Parameters = parameters;
        MustFind = mustFind;
        MustFindId = mustFindId;
    }

    /// <summary>Gets the statement's name, for the log; never its text.</summary>
    public string Name { get; }

    /// <summary>Gets the SQL.</summary>
    public string Text { get; }

    /// <summary>Gets the values, as parameters; never part of the text.</summary>
    public IReadOnlyList<SqlParameter> Parameters { get; }

    /// <summary>Gets the entity that must be the organization's for the statement to answer, or null when none must.</summary>
    public string? MustFind { get; }

    /// <summary>Gets the public id of the entity that must be found.</summary>
    public Guid MustFindId { get; }

    /// <summary>A statement that answers rows, or runs a write.</summary>
    /// <param name="name">The statement's name, for the log.</param>
    /// <param name="text">The SQL.</param>
    /// <param name="parameters">The values.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Of(string name, string text, params SqlParameter[] parameters) =>
        new(name, text, parameters, null, Guid.Empty);

    /// <summary>A statement whose first result is 1 when the organization has the entity and 0 when it has not.</summary>
    /// <param name="name">The statement's name, for the log.</param>
    /// <param name="entity">The entity that must be found, as a refusal names it.</param>
    /// <param name="id">The entity's public id.</param>
    /// <param name="text">The SQL, starting with the <c>SELECT</c> of 1 or 0.</param>
    /// <param name="parameters">The values.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Finding(string name, string entity, Guid id, string text, params SqlParameter[] parameters) =>
        new(name, text, parameters, entity, id);

    /// <summary>A parameter, with SQL's null for a null value.</summary>
    /// <param name="name">The parameter's name, with its <c>@</c>.</param>
    /// <param name="value">The value.</param>
    /// <returns>The parameter.</returns>
    public static SqlParameter Value(string name, object? value) => new(name, value is null ? DBNull.Value : value);

    /// <summary>A public id parameter, typed as SQL Server's uniqueidentifier so a null still compares as one.</summary>
    /// <param name="name">The parameter's name, with its <c>@</c>.</param>
    /// <param name="value">The id, or null.</param>
    /// <returns>The parameter.</returns>
    public static SqlParameter Id(string name, Guid? value) =>
        new(name, SqlDbType.UniqueIdentifier) { Value = value is { } id ? id : DBNull.Value };

    /// <summary>Builds the command the statement is.</summary>
    /// <returns>The command, with its parameters and no connection.</returns>
    public SqlCommand Command()
    {
        var command = new SqlCommand(Text);
        foreach (var parameter in Parameters)
        {
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
