using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FastEndpoints;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using FdwReferenceDataSets.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// FDW-1209: <c>POST /data/{name}</c> answers a caller no page limit applies to by streaming every row, and a stream that
/// fails must not look like a successful one.
/// </summary>
/// <remarks>
/// A real pipeline (authentication, authorization, FastEndpoints) over an in-memory server, because what matters is what the
/// client sees: a status on a failure before anything is written, a cut-off response on a failure after, and a body that ends in
/// <c>rowCount</c> and <c>complete</c> when it finished. The batch size is the fixture's default page of 100, so a data set
/// answered with 101 rows by the first read needs a second.
/// </remarks>
public sealed class QueryDataSetDataEndpointStreamTests : IAsyncLifetime
{
    private readonly Mock<IDataGateway> _gateway = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(HeaderScheme.Name)
            .AddScheme<AuthenticationSchemeOptions, HeaderScheme>(HeaderScheme.Name, _ => { });
        builder.Services.AddAuthorization(o =>
            o.AddPolicy("datasets:read", p => p.RequireClaim("perm", "datasets:read")));
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(QueryDataSetDataEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(QueryDataSetDataEndpoint);
        });

        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get("KeyedTeams", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(Teams("KeyedTeams", keyed: true)));
        dataSets.Setup(d => d.Get("KeylessTeams", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(Teams("KeylessTeams", keyed: false)));
        builder.Services.AddSingleton(dataSets.Object);

        Reads(Rows(0, 1));
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(g => g.ByName("Main")).Returns(_gateway.Object);
        builder.Services.AddSingleton(gateways.Object);

        var noLimit = 0;
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculatedFilterRows, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(10000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(100);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(1000);
        settings.Setup(s => s.TryGetEffectiveValue<int>(SettingDefinitions.ApiKeyDefaultPaginationSize, out noLimit, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(true);
        settings.Setup(s => s.TryGetEffectiveValue<int>(SettingDefinitions.ApiKeyMaxPaginationSize, out noLimit, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(true);
        builder.Services.AddSingleton(settings.Object);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFailureOnTheFirstBatchAnswersWithAnErrorStatusAndNoRows()
    {
        Reads(StoreFailure());

        var answer = await Post("KeyedTeams", apiKey: true);

        answer.Thrown.ShouldBeNull();
        ((int)answer.Status).ShouldBeGreaterThanOrEqualTo(400);
        answer.Body.ShouldNotContain("\"rows\"");
        answer.Body.ShouldNotContain("\"complete\":true");
    }

    // Why the assertion allows either an exception or an error status: whether the client sees the abort as a thrown read or as
    // the status line is the server's to decide (a response nothing has flushed yet has no status to cut). What it must never be
    // is a 200 whose body parses and says it is complete.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFailureOnALaterBatchLeavesTheClientAnIncompleteResponseNotAWellFormedOne()
    {
        Reads(Rows(0, 101), StoreFailure());

        var answer = await Post("KeyedTeams", apiKey: true);

        if (answer.Thrown is null)
        {
            (answer.Status == HttpStatusCode.OK && IsWholeJson(answer.Body)).ShouldBeFalse();
            answer.Body.ShouldNotContain("\"complete\":true");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACompletedStreamCarriesARowCountEqualToTheRowsSentAndIsComplete()
    {
        Reads(Rows(0, 101), Rows(100, 1));

        var answer = await Post("KeyedTeams", apiKey: true);

        answer.Thrown.ShouldBeNull();
        answer.Status.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(answer.Body);
        var root = document.RootElement;
        root.GetProperty("rows").GetArrayLength().ShouldBe(101);
        root.GetProperty("rowCount").GetInt32().ShouldBe(101);
        root.GetProperty("complete").GetBoolean().ShouldBeTrue();
        root.GetProperty("hasMoreRows").GetBoolean().ShouldBeFalse();
        root.TryGetProperty("take", out _).ShouldBeFalse();
        root.TryGetProperty("error", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnUnlimitedKeyReadingADataSetWithNoKeyColumnMakesOneReadAndCarriesRowCountAndComplete()
    {
        Reads(Rows(0, 3));

        var answer = await Post("KeylessTeams", apiKey: true);

        answer.Thrown.ShouldBeNull();
        answer.Status.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(answer.Body);
        var root = document.RootElement;
        root.GetProperty("rows").GetArrayLength().ShouldBe(3);
        root.GetProperty("rowCount").GetInt32().ShouldBe(3);
        root.GetProperty("complete").GetBoolean().ShouldBeTrue();
        _gateway.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ASignedInCallerIsStillAnsweredWithAPageAndNoStreamMarkers()
    {
        var answer = await Post("KeyedTeams", apiKey: false);

        answer.Status.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(answer.Body);
        var root = document.RootElement;
        root.GetProperty("take").GetInt32().ShouldBe(100);
        root.TryGetProperty("complete", out _).ShouldBeFalse();
        root.TryGetProperty("rowCount", out _).ShouldBeFalse();
    }

    private static SimpleDataSetConfiguration Teams(string name, bool keyed) => new()
    {
        Name = name,
        Implementation = "Simple",
        Fields =
        [
            new DataSetFieldConfiguration { Name = "Name", Ordinal = 0, TypeName = "String", IsKey = keyed },
            new DataSetFieldConfiguration { Name = "Abbreviation", Ordinal = 1, TypeName = "String", IsFilterable = false },
        ],
    };

    // The gateway's answers to data set reads: each test declares what each read returns, in order.
    private void Reads(params IGenericResult<IEnumerable<Dictionary<string, object?>>>[] results)
    {
        _gateway.Reset();
        var sequence = _gateway.SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()));
        foreach (var result in results)
            sequence = sequence.ReturnsAsync(result);
    }

    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> StoreFailure()
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("the store went away"));

    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> Rows(int first, int count)
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            Enumerable.Range(first, count)
                .Select(i => new Dictionary<string, object?> { ["Name"] = $"Team{i}", ["Abbreviation"] = "X", ["OwnerSsn"] = "123-45-6789" })
                .ToList());

    private static bool IsWholeJson(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<(HttpStatusCode Status, string Body, Exception? Thrown)> Post(string dataSet, bool apiKey)
    {
        var status = default(HttpStatusCode);
        var body = string.Empty;
        var thrown = await Record.ExceptionAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/data/{dataSet}", UriKind.Relative))
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Add(HeaderScheme.Header, "datasets:read");
            if (apiKey)
                request.Headers.Add(HeaderScheme.ApiKeyHeader, "true");

            using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            status = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        });

        return (status, body, thrown);
    }

    /// <summary>Authenticates a caller carrying the permissions named in one header; no header, no identity.</summary>
    private sealed class HeaderScheme(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "Test";
        public const string Header = "X-Test-Permissions";

        // Present, the identity is named after an ApiKey scheme, as the ApiKey handler names the one it builds.
        public const string ApiKeyHeader = "X-Test-ApiKey";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Header, out var permissions))
                return Task.FromResult(AuthenticateResult.NoResult());

            var authenticationType = Request.Headers.ContainsKey(ApiKeyHeader) ? ApiKeyAuthenticationSchemes.Prefix + "Test" : Name;
            var identity = new ClaimsIdentity(
                permissions.ToString().Split(',').Select(p => new Claim("perm", p.Trim())), authenticationType);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Name)));
        }
    }
}
