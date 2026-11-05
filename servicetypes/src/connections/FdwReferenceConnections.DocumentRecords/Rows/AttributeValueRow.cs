using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.AttributeValue (FDW-3731).</summary>
internal sealed class AttributeValueRow : IAttributeValue
{
    /// <summary>Initializes a new instance of the <see cref="AttributeValueRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="ownerId">The OwnerId.</param>
    /// <param name="attributeCode">The AttributeCode.</param>
    /// <param name="text">The Text.</param>
    /// <param name="number">The Number.</param>
    /// <param name="date">The Date.</param>
    /// <param name="flag">The Flag.</param>
    internal AttributeValueRow(Guid id, Guid ownerId, string attributeCode, string? text, decimal? number, DateTimeOffset? date, bool? flag)
    {
        Id = id;
        OwnerId = ownerId;
        AttributeCode = attributeCode;
        Text = text;
        Number = number;
        Date = date;
        Flag = flag;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid OwnerId { get; }

    /// <inheritdoc/>
    public string AttributeCode { get; }

    /// <inheritdoc/>
    public string? Text { get; }

    /// <inheritdoc/>
    public decimal? Number { get; }

    /// <inheritdoc/>
    public DateTimeOffset? Date { get; }

    /// <inheritdoc/>
    public bool? Flag { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IAttributeValue> From(SqlDataReader reader) =>
        GenericResult<IAttributeValue>.Success(new AttributeValueRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("OwnerId")),
            reader.GetString(reader.GetOrdinal("AttributeCode")),
            reader.IsDBNull(reader.GetOrdinal("Text")) ? null : reader.GetString(reader.GetOrdinal("Text")),
            reader.IsDBNull(reader.GetOrdinal("Number")) ? null : reader.GetDecimal(reader.GetOrdinal("Number")),
            reader.IsDBNull(reader.GetOrdinal("Date")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("Date")),
            reader.IsDBNull(reader.GetOrdinal("Flag")) ? null : reader.GetBoolean(reader.GetOrdinal("Flag"))));
}
