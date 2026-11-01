using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FastEndpoints;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Commands.Data.Abstractions;
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
/// GET /datasets/{name}/query reads a data set the way POST /data/{name} does: the kind's read gate (FDW-1054), and the data
/// set path of the gateway, the filterable check and only the declared fields (FDW-1075).
/// </summary>
/// <remarks>
/// The same real pipeline as the authorization tests, with the provider handing back a set of each kind and the gateway handing
/// back a row that carries a column the data set does not declare.
/// </remarks>
public sealed class QueryDataSetEndpointKindGateTests : IAsyncLifetime
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
            o.Assemblies = [typeof(QueryDataSetEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(QueryDataSetEndpoint);
        });

        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get("BreweriesInNflCities", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(
                new FederatedDataSetConfiguration { Name = "BreweriesInNflCities", Implementation = "Federated" }));
        dataSets.Setup(d => d.Get("NflTeams", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(
                new SimpleDataSetConfiguration
                {
                    Name = "NflTeams",
                    Implementation = "Simple",
                    Fields =
                    [
                        new DataSetFieldConfiguration { Name = "Name", Ordinal = 0, TypeName = "String" },
                        new DataSetFieldConfiguration { Name = "Abbreviation", Ordinal = 1, TypeName = "String", IsFilterable = false },
                    ],
                }));
        dataSets.Setup(d => d.Get("KeyedNflTeams", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(
                new SimpleDataSetConfiguration
                {
                    Name = "KeyedNflTeams",
                    Implementation = "Simple",
                    Fields =
                    [
                        new DataSetFieldConfiguration { Name = "Name", Ordinal = 0, TypeName = "String", IsKey = true },
                        new DataSetFieldConfiguration { Name = "Abbreviation", Ordinal = 1, TypeName = "String", IsFilterable = false },
                    ],
                }));
        builder.Services.AddSingleton(dataSets.Object);

        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
                [new Dictionary<string, object?> { ["Name"] = "Patriots", ["Abbreviation"] = "NE", ["OwnerSsn"] = "123-45-6789" }]));
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(g => g.ByName("Main")).Returns(_gateway.Object);
        builder.Services.AddSingleton(gateways.Object);

        // The route sizes its page from the paging settings (FDW-1209), so the fixture declares them like a real server does;
        // the ApiKey pair is 0, no limit, as the seed has it.
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculatedFilterRows, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(10000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(100);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(1000);
        var noLimit = 0;
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
    public async Task AFederatedSetIsRefusedWithTheReasonInsteadOfAnsweredFromItsFirstSource()
    {
        var response = await Get("BreweriesInNflCities");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("kind 'Federated' does not page in the store");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ASimpleSetAnswersWithOnlyItsDeclaredFields()
    {
        var response = await Get("NflTeams");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Patriots");
        body.ShouldNotContain("OwnerSsn");
        body.ShouldNotContain("123-45-6789");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheReadGoesThroughTheDataSetNotTheContainerUnderIt()
    {
        await Get("NflTeams");

        _gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.Is<DataSetTarget>(t => t.DataSet == "NflTeams"), It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFilterOnAFieldThatIsNotFilterableIsRefusedWithTheReason()
    {
        var response = await Get("NflTeams", "?Abbreviation=NE");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("'Abbreviation' is not a field of this data set that can be filtered on");
    }

    // FDW-1209: the limits that protect an interactive caller do not apply to a caller an API key authenticated.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnApiKeyWithNoLimitIsSentEveryRowAndNoTake()
    {
        var response = await Get("KeyedNflTeams", apiKey: true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Patriots");
        body.ShouldContain("\"hasMoreRows\":false");
        body.ShouldNotContain("\"take\"");
        body.ShouldNotContain("OwnerSsn");
        body.ShouldContain("\"rowCount\":1");
        body.ShouldContain("\"complete\":true");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnApiKeyWithNoLimitIsSentEveryRowOfADataSetThatDeclaresNoKeyField()
    {
        var response = await Get("NflTeams", apiKey: true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Patriots");
        body.ShouldContain("\"hasMoreRows\":false");
        body.ShouldNotContain("\"take\"");
        body.ShouldContain("\"rowCount\":1");
        body.ShouldContain("\"complete\":true");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFailureOnTheFirstBatchOfAStreamAnswersWithAnErrorStatusAndNoRows()
    {
        Reads(StoreFailure());

        var answer = await Fetch("KeyedNflTeams");

        answer.Thrown.ShouldBeNull();
        ((int)answer.Status).ShouldBeGreaterThanOrEqualTo(400);
        answer.Body.ShouldNotContain("\"rows\"");
        answer.Body.ShouldNotContain("\"complete\":true");
    }

    // Why either an exception or an error status: whether the client sees the abort as a thrown read or as the status line is the
    // server's to decide. What it must never be is a 200 whose body parses and says it is complete.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFailureOnALaterBatchOfAStreamLeavesAnIncompleteResponseNotAWellFormedOne()
    {
        Reads(Teams(0, 101), StoreFailure());

        var answer = await Fetch("KeyedNflTeams");

        if (answer.Thrown is null)
        {
            (answer.Status == HttpStatusCode.OK && IsWholeJson(answer.Body)).ShouldBeFalse();
            answer.Body.ShouldNotContain("\"complete\":true");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACompletedStreamOfSeveralBatchesCarriesARowCountEqualToTheRowsSent()
    {
        Reads(Teams(0, 101), Teams(100, 1));

        var answer = await Fetch("KeyedNflTeams");

        answer.Thrown.ShouldBeNull();
        answer.Status.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(answer.Body);
        var root = document.RootElement;
        root.GetProperty("rows").GetArrayLength().ShouldBe(101);
        root.GetProperty("rowCount").GetInt32().ShouldBe(101);
        root.GetProperty("complete").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ASignedInCallerIsStillPagedByTheDefaultPaginationSize()
    {
        var response = await Get("NflTeams");

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"take\":100");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnApiKeyThatNamesATakeIsNotCappedAtTheOldThousand()
    {
        var response = await Get("NflTeams", "?take=5000", apiKey: true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("\"take\":5000");
    }

    // The gateway's answers to data set reads: a test declares what each read returns, in order.
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

    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> Teams(int first, int count)
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            Enumerable.Range(first, count)
                .Select(i => new Dictionary<string, object?> { ["Name"] = $"Team{i}", ["Abbreviation"] = "X" })
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

    // What a client sees of an API key's read: the status and body, or what it threw when the response was cut off.
    private async Task<(HttpStatusCode Status, string Body, Exception? Thrown)> Fetch(string dataSet)
    {
        var status = default(HttpStatusCode);
        var body = string.Empty;
        var thrown = await Record.ExceptionAsync(async () =>
        {
            using var response = await Get(dataSet, apiKey: true);
            status = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        });

        return (status, body, thrown);
    }

    private async Task<HttpResponseMessage> Get(string dataSet, string query = "", bool apiKey = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/datasets/{dataSet}/query{query}", UriKind.Relative));
        request.Headers.Add(HeaderScheme.Header, "datasets:read");
        if (apiKey)
            request.Headers.Add(HeaderScheme.ApiKeyHeader, "true");

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
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
