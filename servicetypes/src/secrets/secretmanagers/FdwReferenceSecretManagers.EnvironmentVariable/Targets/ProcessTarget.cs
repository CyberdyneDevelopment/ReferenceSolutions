using System;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>Reads the variables of the current process.</summary>
[TypeOption(typeof(EnvironmentVariableTargets), "Process")]
public sealed class ProcessTarget : EnvironmentVariableTargetBase
{
    /// <summary>Initializes a new instance of the <see cref="ProcessTarget"/> class.</summary>
    public ProcessTarget()
        : base(1, "Process", EnvironmentVariableTarget.Process)
    {
    }
}
