using System;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>Reads the variables of the current user account.</summary>
[TypeOption(typeof(EnvironmentVariableTargets), "User")]
public sealed class UserTarget : EnvironmentVariableTargetBase
{
    /// <summary>Initializes a new instance of the <see cref="UserTarget"/> class.</summary>
    public UserTarget()
        : base(2, "User", EnvironmentVariableTarget.User)
    {
    }
}
