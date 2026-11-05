using System;
using Fdw.Collections;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>
/// Base of the <see cref="EnvironmentVariableTargets"/> options: each names a target and carries the
/// <see cref="EnvironmentVariableTarget"/> System.Environment reads it with.
/// </summary>
public abstract class EnvironmentVariableTargetBase : TypeOptionBase<EnvironmentVariableTargetBase>, IEnvironmentVariableTarget
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentVariableTargetBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The name a configuration states as its Target.</param>
    /// <param name="systemTarget">The System value this option is read with.</param>
    protected EnvironmentVariableTargetBase(int id, string name, EnvironmentVariableTarget systemTarget)
        : base(id, name)
    {
        SystemTarget = systemTarget;
    }

    /// <inheritdoc />
    public EnvironmentVariableTarget SystemTarget { get; }
}
