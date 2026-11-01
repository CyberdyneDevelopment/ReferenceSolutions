using System;
using System.IO;
using System.Threading.Tasks;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSecretManagers.Sqlite.Tests;

/// <summary>
/// The command timeout of a SQLite secret manager is its own or the instance's setting DefaultCommandTimeoutSeconds, and a secret manager that
/// has neither is not built (FDW-3284).
/// </summary>
public sealed class SqliteSecretManagerCommandTimeoutTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"secret-timeout-{Guid.NewGuid():N}.db");

    private SqliteSecretManagerConfiguration Configuration(int? commandTimeoutSeconds)
        => new() { Id = Guid.NewGuid(), DataSource = _dbPath, TableName = "Secret", CommandTimeoutSeconds = commandTimeoutSeconds };

    private static Lazy<IEffectiveSettingsProvider> SettingHolds(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = seconds;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(true);
        return new Lazy<IEffectiveSettingsProvider>(() => settings.Object);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecretManagerThatStatesATimeoutIsBuiltWithoutReadingTheSetting()
    {
        var settings = SettingHolds(99);

        var created = await new SqliteSecretManagerFactory(NullLoggerFactory.Instance, settings).CreateSecretManager(Configuration(45));

        created.IsSuccess.ShouldBeTrue();
        settings.IsValueCreated.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecretManagerThatStatesNoTimeoutIsBuiltFromTheInstancesSetting()
    {
        var created = await new SqliteSecretManagerFactory(NullLoggerFactory.Instance, SettingHolds(60)).CreateSecretManager(Configuration(null));

        created.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecretManagerThatStatesNoTimeoutInAHostWithNoSettingsIsNotBuiltAndTheFailureNamesTheSetting()
    {
        var created = await new SqliteSecretManagerFactory(NullLoggerFactory.Instance, settings: null).CreateSecretManager(Configuration(null));

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("SETTINGS-62031");
        created.Messages[^1].Message.ShouldContain("secret manager");
        created.Messages[^1].Message.ShouldContain(SettingDefinitions.DefaultCommandTimeoutSeconds);
        File.Exists(_dbPath).ShouldBeFalse();
    }
}
