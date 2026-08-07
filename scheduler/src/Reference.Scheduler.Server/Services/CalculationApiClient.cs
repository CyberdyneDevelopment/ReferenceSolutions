using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// HTTP client for executing calculations via the API server.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationApiClient : ICalculationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CalculationApiClient> _logger;

    public CalculationApiClient(
        HttpClient httpClient,
        ILogger<CalculationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IGenericResult<long>> Execute(
        string calculationType,
        string calculationHash,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var request = new { CalculationType = calculationType, CalculationHash = calculationHash };
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/calculations/execute",
                request,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return GenericResult<long>.Failure(
                    PreComputeLog.PreComputeFailed(
                        _logger,
                        calculationType,
                        $"HTTP {(int)response.StatusCode}: {body}"));
            }

            return GenericResult<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PreComputeLog.PreComputeException(_logger, calculationType);
            return GenericResult<long>.Failure(
                PreComputeLog.PreComputeFailed(_logger, calculationType, ex.Message));
        }
    }
}
