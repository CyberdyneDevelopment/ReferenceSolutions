using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Hosting;

namespace SampleDomain.Clients.Registration;

/// <summary>Registers the SampleDomain API client as a service type option.</summary>
/// <remarks>
/// Registration is what makes the client resolvable — referencing the package is the intent, and
/// this option is the act. AddApiHttpClient names the HttpClient after this option, which is where
/// the BaseAddress carrying the api/v1 prefix is set, so the client itself never repeats it.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ApiClientTypes), "SampleDomainClient")]
public sealed class SampleDomainClientType : ApiClientTypeBase<SampleDomainApiClient>
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainClientType"/> class.</summary>
    public SampleDomainClientType()
        : base("SampleDomainClient", "SampleDomain API Client")
    {
        Configuration(builder =>
        {
            builder.Services.AddApiHttpClient(Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
