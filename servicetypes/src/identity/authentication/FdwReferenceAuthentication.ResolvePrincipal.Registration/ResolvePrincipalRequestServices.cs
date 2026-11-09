using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration;

/// <summary>
/// What the resolve step uses that lives for one request: the provisioner provider, a domain service provider that is scoped (FDW-4640).
/// </summary>
/// <remarks>
/// Registered scoped by the step's registration and bound to the step by the runner for the execution, so the step, which is one instance for the process, never keeps one
/// request's provisioner provider for the next login.
/// </remarks>
/// <param name="provisioners">Hands over the provisioner provider of the request's own scope.</param>
public sealed class ResolvePrincipalRequestServices(Func<IExternalIdentityProvisionerServiceProvider> provisioners)
{
    /// <summary>The provisioner provider of the request this was resolved for.</summary>
    public IExternalIdentityProvisionerServiceProvider Provisioners => provisioners();
}
