using System.Threading;

namespace FdwReferenceEndpoints;

/// <summary>Whether endpoint mapping finished, and how many endpoints it mapped.</summary>
/// <remarks>
/// Exists because a route whose mapping died and a route that was never built are the same
/// zero-byte 404 from outside the host. Four distinct causes of a 404 were seen in one evening —
/// no <c>[TypeOption]</c> declaring the endpoint, an undeclared data container, a deleted row, and
/// mapping throwing at boot — and only the last is an incident rather than a thing to build.
///
/// Static rather than a registered service on purpose: the reader has to work when the container
/// is in whatever state a failed startup left it. Written exactly once, from the startup thread,
/// before any request is served.
/// </remarks>
public static class EndpointMappingStatus
{
    private static int _completed;
    private static int _endpointCount;

    /// <summary>Gets whether endpoint mapping ran to completion.</summary>
    /// <remarks>
    /// False until mapping returns normally, and stays false if it throws — so it reports what
    /// happened rather than what was attempted. Never set from a catch: a flag written by the
    /// failure path would be reporting its own guess.
    /// </remarks>
    public static bool MappingCompleted => Volatile.Read(ref _completed) == 1;

    /// <summary>Gets how many endpoints were mapped, or zero when mapping did not complete.</summary>
    public static int EndpointCount => Volatile.Read(ref _endpointCount);

    /// <summary>Records that mapping completed.</summary>
    /// <param name="endpointCount">How many endpoints were mapped.</param>
    internal static void MarkCompleted(int endpointCount)
    {
        Volatile.Write(ref _endpointCount, endpointCount);
        Volatile.Write(ref _completed, 1);
    }
}
