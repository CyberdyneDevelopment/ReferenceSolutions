using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Client for executing calculations via the API server.
/// </summary>
public interface ICalculationApiClient
{
    /// <summary>
    /// Executes a calculation by type and hash via the API.
    /// </summary>
    /// <param name="calculationType">The calculation type name.</param>
    /// <param name="calculationHash">The calculation hash identifying specific parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with duration in milliseconds.</returns>
    Task<IGenericResult<long>> Execute(string calculationType, string calculationHash, CancellationToken cancellationToken);
}
