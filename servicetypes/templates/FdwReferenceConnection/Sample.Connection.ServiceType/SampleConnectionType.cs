using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.ServiceTypes;
using Sample.Connection;
using Fdw.Services.Connections.Abstractions.Authentication;
using System.Collections.Generic;
using Fdw.Services.Connections.Results;

namespace Sample.Connection.ServiceType;

/// <summary>Registers SAMPLE_CONNECTION as a connection type.</summary>
/// <remarks>
/// Referencing the package is the intent; this option is the act. A connection type that is never
/// registered fails at resolution with "no registered service type", which reads like a
/// configuration problem and is not one.
///
/// The parameterless constructor is required — the module initializer that collects options
/// constructs each one with no arguments and silently skips any that lacks one, so an option that
/// never appears in its collection usually failed here rather than in the attribute.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "SAMPLE_CONNECTION")]
public sealed class SampleConnectionType
    : ConnectionTypeBase<IGenericConnection, ISampleConnectionFactory, SampleConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SampleConnectionType"/> class.</summary>
    public SampleConnectionType()
        : base(
            name: "SAMPLE_CONNECTION",
            sectionName: "Connections",
            displayName: "SAMPLE_CONNECTION connection",
            description: "A SAMPLE_CONNECTION connection.",
            category: "SAMPLE_CATEGORY",
            defaultContainerName: "SAMPLE_CONNECTIONConnection")
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// State the fact for this kind. A connection that signs in as the host (Windows or Kerberos, a managed
    /// identity, the host's file system) answers true, and only a platform administrator may then create or change
    /// one. A kind with authentication methods asks the method its configuration names instead; see
    /// MsSqlConnectionType. Replace this answer with the fact for the kind you are building.
    /// </remarks>
    protected override IGenericResult<bool> DecideHostIdentity(SampleConnectionConfiguration configuration)
        => GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    /// <remarks>The sample carries no stored credential; a kind that authenticates reads its method's credential fields here.</remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(SampleConnectionConfiguration configuration)
        => GenericResult<ConnectionCredentialSlots>.Success(ConnectionCredentialSlots.None);

    /// <inheritdoc/>
    protected override IGenericResult BindCredentialReferences(
        SampleConnectionConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
        => GenericResult.Failure(
            ConnectionsResultCodes.ByName("CredentialsUndetermined"),
            ResultDetails.Create("name", configuration.Name, "reason", "this kind of connection carries no stored credential"));
}
