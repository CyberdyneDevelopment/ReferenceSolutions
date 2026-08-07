using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

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
        command.TableName.ShouldBe("EnvironmentVariableSecretManager");
        command.ContainerName.ShouldBe("EnvironmentVariableSecretManager");
        command.ConfigType.ShouldBe(typeof(EnvironmentVariableConfiguration));
    }
}
