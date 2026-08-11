using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Etl.Projects.Providers;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceProjects.Endpoints.ProjectsEndpointOptions;

namespace ReferenceProjects.Endpoints;

/// <summary>The projects domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Projects")]
public class ProjectsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ProjectsApiServiceType"/> class.</summary>
    public ProjectsApiServiceType()
        : base("Projects", "Projects", "Projects API", "HTTP endpoints for the projects domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            // The config provider, for the CRUD these endpoints do over orchestration-node
            // configuration. Execution belongs to the ETL server.
            OrchestrationNodeConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            return RegisterEndpoints(builder, loggerFactory);
        });

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new ProjectsEndpoints() };
}
