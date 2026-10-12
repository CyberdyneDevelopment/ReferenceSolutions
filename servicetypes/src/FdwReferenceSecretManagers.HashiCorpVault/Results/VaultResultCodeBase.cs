using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace FdwReferenceSecretManagers.HashiCorpVault.Results;

/// <summary>
/// Base class for HashiCorp Vault result codes.
/// </summary>
/// <remarks>
/// Numbers are categorized (<c>Category = Id / 10000</c>) and sit in this package's open band
/// (<c>x1000+</c>), so category — and therefore HTTP status and retryability — is derived from the
/// number rather than hand-set per code. <c>EventId == Id</c>, so a code here and the MessageLogging
/// method reporting the same condition carry the same number.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class VaultResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes a new instance for the Empty sentinel.</summary>
    protected VaultResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="VaultResultCodeBase"/> class with a categorized number.</summary>
    /// <param name="number">The categorized result number.</param>
    /// <param name="name">The code name.</param>
    /// <param name="severity">The severity this condition carries.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="isRetryable">Whether retrying could succeed.</param>
    protected VaultResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "VAULT", isRetryable)
    {
    }
}
