using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;
using Fdw.UI.Themes;
using Fdw.Services.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Services.Settings;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The ThemeEndpoints endpoint group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ThemeEndpoints")]
[TypeCollection(typeof(ThemeEndpointBase), typeof(IEndpointTypeOption), typeof(ThemeEndpoints))]
public partial class ThemeEndpoints : EndpointTypeCollectionBase<ThemeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

    /// <summary>Initializes a new instance of the <see cref="ThemeEndpoints"/> class.</summary>
    public ThemeEndpoints()
        => Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IThemeImplementationConfigurationProvider, ThemeImplementationConfigurationProvider>(sp => new ThemeImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ThemeImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SettingsServiceTypes.ConfigurationConnection, "settings", "ThemeConfiguration"));

            builder.Services.TryAddSingleton<ThemeConfigurationProvider>(sp =>
            {
                var domain = new ThemeConfigurationProvider(
                    sp.GetRequiredService<ILogger<ThemeConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    SettingsServiceTypes.ConfigurationConnection, "settings", "Theme");
                domain.Register("Theme", sp.GetRequiredService<IThemeImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IThemeConfigurationProvider>(sp => sp.GetRequiredService<ThemeConfigurationProvider>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
}
