using Fdw.Collections;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Results;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceSecretManagers.TestDouble;

/// <summary>
/// The <c>Synthetic</c> secret-manager <c>[Implementation]</c> this suite supplies to
/// <see cref="SecretManagerTypes"/>.
/// </summary>
/// <remarks>
/// <para>
/// Why the test assembly owns this: FDW ships the SecretManager collection, abstractions and
/// per-backend configuration packages, but deliberately no concrete backend — secret custody belongs
/// to the consuming application. A consumer therefore declares its own option in its OWN assembly,
/// where the module initializer registers it at load; this suite is that consumer.
/// </para>
/// <para>
/// The option self-wires: <c>Registration</c> puts the factory in DI while the container is still
/// being built, and <c>Initialization</c> hands that factory to the domain provider once the
/// container is live. Nothing in Aegis names this backend — it becomes reachable purely by this
/// assembly being loaded, which is the whole point of the mechanism.
/// </para>
/// </remarks>
[Implementation(typeof(SecretManagerTypes), OptionName)]
public sealed class SyntheticSecretManagerType
    : SecretManagerTypeBase<ISecretManager, ISyntheticSecretManagerFactory, SyntheticSecretManagerConfiguration>
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "Synthetic";

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticSecretManagerType"/> class.
    /// </summary>
    public SyntheticSecretManagerType()
        : base(
            name: OptionName,
            sectionName: OptionName,
            displayName: "Synthetic (test)",
            description: "Test-owned secret manager that reads secrets from environment variables.",
            supportedSecretStores: [OptionName],
            supportedSecretTypes: ["ApiKey", "Token"],
            supportsRotation: false,
            supportsVersioning: false,
            supportsSoftDelete: false,
            supportsAccessPolicies: false,
            maxSecretSizeBytes: 32767,
            defaultContainerName: "SyntheticSecretManager")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<ISyntheticSecretManagerFactory, SyntheticSecretManagerFactory>();

            // Why: this registers the shared header provider for the whole SecretManager domain — and
            // with it ISecretManagerProvider, which AegisInjector takes as a constructor dependency.
            // TryAddSingleton inside makes it idempotent; every option calls it, first registration wins.
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Initialization and not Registration: handing the factory to the provider needs a LIVE
        // container, and Registration runs while the container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services
                .GetRequiredService<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>>()
                .Register(Name, () => new SyntheticSecretManagerProvider(services.GetRequiredService<ISyntheticSecretManagerFactory>()));

            return GenericResult<IHost>.Success(host);
        });
    }
}
