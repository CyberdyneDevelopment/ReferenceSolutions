namespace FdwReferenceServiceNamePlural.ImplName;

/// <summary>
/// Factory contract for the ImplName ServiceName.
/// </summary>
// Why: the service-type option names this INTERFACE, never the concrete factory, so the registration
// surface binds to a contract and the aggregation stays replaceable.
//
// TODO: derive this from your domain's factory contract. FDW does not use one universal name --
// SecretManagers, Connections and Notifications each declare their own, and several are generic in
// the service, factory and configuration types. Check the real signature in
// Fdw.Services.ServiceNamePlural before wiring the option below.
public interface IImplNameServiceNameFactory
{
    /// <summary>
    /// Creates the ImplName ServiceName.
    /// </summary>
    /// <returns>A configured service instance.</returns>
    ImplNameServiceName Create(string name);
}
