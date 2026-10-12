using Fdw.Services.Connections.Abstractions;

namespace Sample.Connection;

/// <summary>Builds a SAMPLE_CONNECTION connection from its configuration.</summary>
/// <remarks>
/// The factory closes over this kind's implementation configuration, because that is the only
/// shape carrying what opening this kind of connection actually requires. The name it reports the
/// connection by comes from the domain record and is copied across before construction.
/// </remarks>
public interface ISampleConnectionFactory : IConnectionFactory<IGenericConnection, SampleConnectionConfiguration>
{
}
