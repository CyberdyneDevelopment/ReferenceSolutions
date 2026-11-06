using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Targets;

/// <summary>
/// The targets an environment-variable secret manager can read from (FDW-4144). A configuration's Target is one of these
/// names; <c>ByName</c> answers the option, or <c>NotFound</c> for a name that is none of them.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EnvironmentVariableTargetBase), typeof(IEnvironmentVariableTarget), typeof(EnvironmentVariableTargets))]
public abstract partial class EnvironmentVariableTargets : TypeCollectionBase<EnvironmentVariableTargetBase, IEnvironmentVariableTarget>
{
}
