using System;
using Fdw.Collections;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>
/// Where an environment-variable secret manager reads its variables: the current process, the current user or the machine
/// (FDW-4144). A configuration states the option's <c>Name</c>; nothing parses an enum to find it.
/// </summary>
public interface IEnvironmentVariableTarget : ITypeOption<int, IEnvironmentVariableTarget>
{
    /// <summary>Gets the <see cref="EnvironmentVariableTarget"/> this option is read with, used only where System.Environment is called.</summary>
    EnvironmentVariableTarget SystemTarget { get; }
}
