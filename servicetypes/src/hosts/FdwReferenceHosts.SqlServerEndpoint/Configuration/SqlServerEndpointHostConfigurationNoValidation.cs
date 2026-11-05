using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.SqlServerEndpoint.Configuration;

/// <summary>States that <see cref="SqlServerEndpointHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqlServerEndpointHostConfiguration")]
public sealed class SqlServerEndpointHostConfigurationNoValidation : UnvalidatedConfiguration<SqlServerEndpointHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointHostConfigurationNoValidation"/> class.</summary>
    public SqlServerEndpointHostConfigurationNoValidation()
        : base("SqlServerEndpointHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
