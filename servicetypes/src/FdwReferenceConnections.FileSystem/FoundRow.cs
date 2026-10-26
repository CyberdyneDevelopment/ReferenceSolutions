using System.Collections.Generic;

namespace FdwReferenceConnections.FileSystem;

/// <summary>A row a Find kept, with the fields in which the search term was found.</summary>
/// <param name="Row">The row as the file holds it.</param>
/// <param name="MatchedFields">The names of the fields that contain the search term.</param>
internal sealed record FoundRow(IReadOnlyDictionary<string, object?> Row, IReadOnlyList<string> MatchedFields);
