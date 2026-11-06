using System;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>Reads the variables of the machine (setting one needs elevated rights).</summary>
[TypeOption(typeof(EnvironmentVariableTargets), "Machine")]
public sealed class MachineTarget : EnvironmentVariableTargetBase
{
    /// <summary>Initializes a new instance of the <see cref="MachineTarget"/> class.</summary>
    public MachineTarget()
        : base(3, "Machine", EnvironmentVariableTarget.Machine)
    {
    }
}
