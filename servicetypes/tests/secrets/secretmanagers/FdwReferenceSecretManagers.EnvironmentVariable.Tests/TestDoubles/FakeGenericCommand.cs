using Fdw.Abstractions;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Tests.TestDoubles;

// Why: a minimal IGenericCommand that is deliberately NOT an ISecretManagerCommand, used to exercise
// the "command must be of type ISecretManagerCommand" guard on EnvironmentVariableSecretManager's
// explicit IGenericService.Execute overloads.
internal sealed class FakeGenericCommand : IGenericCommand
{
    public Guid CommandId { get; } = Guid.NewGuid();

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public string CommandType => "SomeOtherCommand";

    public string Category => "Other";
}
