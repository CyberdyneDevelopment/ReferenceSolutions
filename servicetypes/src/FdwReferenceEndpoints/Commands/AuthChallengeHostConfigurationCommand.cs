using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceEndpoints.Configuration;

namespace FdwReferenceEndpoints.Commands;

/// <summary>ConfigurationCommands TypeOption for what this surface says when it refuses a caller.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostAuthChallengeConfiguration")]
public sealed class AuthChallengeHostConfigurationCommand : ConfigurationCommandBase<AuthChallengeHostConfiguration>
{
    /// <inheritdoc/>
    public AuthChallengeHostConfigurationCommand() : base("HostAuthChallengeConfiguration") { }
}
