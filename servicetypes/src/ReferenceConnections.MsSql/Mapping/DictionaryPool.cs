using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Fdw.Services.Connections.MsSql;

namespace ReferenceConnections.MsSql.Mapping;

/// <summary>
/// Object pool for dictionaries to eliminate per-row allocations.
/// Thread-safe implementation using ConcurrentBag.
/// </summary>
internal static class DictionaryPool
{
    private static readonly ConcurrentBag<Dictionary<string, object?>> Pool = new();
    private const int MaxPoolSize = 1000;
    private const int MaxDictionarySize = 100;

    /// <summary>
    /// Rents a dictionary from the pool, or creates a new one if pool is empty.
    /// </summary>
    /// <param name="capacity">The expected capacity for the dictionary.</param>
    /// <returns>A cleared dictionary ready for use.</returns>
    public static Dictionary<string, object?> Rent(int capacity)
    {
        if (Pool.TryTake(out var dict))
        {
            dict.Clear();
            return dict;
        }

        return new Dictionary<string, object?>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a dictionary to the pool for reuse.
    /// Dictionaries that are too large or when the pool is full are discarded.
    /// </summary>
    /// <param name="dict">The dictionary to return.</param>
    public static void Return(Dictionary<string, object?> dict)
    {
        if (dict == null)
        {
            return;
        }

        // Don't pool oversized dictionaries (indicates unusual usage patterns)
        if (dict.Count > MaxDictionarySize)
        {
            return;
        }

        // Don't exceed pool size
        if (Pool.Count >= MaxPoolSize)
        {
            return;
        }

        dict.Clear();
        Pool.Add(dict);
    }

    /// <summary>
    /// Gets the current pool size (for diagnostics/testing).
    /// </summary>
    public static int CurrentPoolSize => Pool.Count;

    /// <summary>
    /// Clears the pool (for testing or memory pressure scenarios).
    /// </summary>
    public static void Clear()
    {
        while (Pool.TryTake(out _))
        {
            // Drain the pool
        }
    }
}
