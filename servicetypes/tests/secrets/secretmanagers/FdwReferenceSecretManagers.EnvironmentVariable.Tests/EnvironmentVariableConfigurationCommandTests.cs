using FdwReferenceSecretManagers.EnvironmentVariable.Commands;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableConfigurationCommand"/>: the TypeOption identity used by
/// <see cref="Fdw.Services.Configuration.ConfigurationCommands"/> to address the
/// EnvironmentVariableSecretManager typed-body table.
/// </summary>
public class EnvironmentVariableConfigurationCommandTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void TableNameAndConfigTypeMatchEnvironmentVariableConfiguration()
    {
        // Arrange
        var command = new EnvironmentVariableConfigurationCommand();

        // Assert
        command.TableName.ShouldBe("EnvironmentVariableSecretManagerConfiguration");
        command.ContainerName.ShouldBe("EnvironmentVariableSecretManagerConfiguration");
        command.ConfigType.ShouldBe(typeof(EnvironmentVariableConfiguration));
    }
}
