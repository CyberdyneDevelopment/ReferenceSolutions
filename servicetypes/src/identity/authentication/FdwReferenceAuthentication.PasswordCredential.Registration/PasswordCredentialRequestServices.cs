using Fdw.Services.Users.Abstractions;

namespace FdwReferenceAuthentication.PasswordCredential.Registration;

/// <summary>
/// What the password step uses that lives for one request: the credential service, whose own dependencies are scoped (FDW-4640).
/// </summary>
/// <remarks>
/// Registered scoped by the step's registration and bound to the step by the runner for the execution, so the step, which is one instance for the process, never keeps one
/// request's credential service for the next login.
/// </remarks>
/// <param name="credentials">Hands over the credential service of the request's own scope.</param>
public sealed class PasswordCredentialRequestServices(Func<IUserCredentialService> credentials)
{
    /// <summary>The credential service of the request this was resolved for.</summary>
    public IUserCredentialService Credentials => credentials();
}
