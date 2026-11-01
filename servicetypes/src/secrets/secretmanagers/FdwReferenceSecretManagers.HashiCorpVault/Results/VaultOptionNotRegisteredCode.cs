using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// The configuration names an engine or auth method no option provides.
/// </summary>
[TypeOption(typeof(VaultResultCodes), "VaultOptionNotRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VaultOptionNotRegisteredCode : VaultResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="VaultOptionNotRegisteredCode"/> class.</summary>
    public VaultOptionNotRegisteredCode()
        : base(
            61001,
            "VaultOptionNotRegistered",
            ResultSeverities.ByName("Critical"),
            "Vault configuration names {kind} '{requested}', which is not registered.",
            isRetryable: false)
    { }
}
