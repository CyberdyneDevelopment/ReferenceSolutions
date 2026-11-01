using System;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Sqlite.Tests;

/// <summary>
/// The command timeout a SQLite connection gives its commands and scripts (FDW-3279, FDW-3284). Microsoft.Data.Sqlite gives a command the
/// default timeout of the connection string it is attached to, so the resolved number has to be in that string whatever it is, 30
/// included, and not left to the driver's own default. The number is the connection's own or the instance's setting; a connection with
/// neither is not built.
/// </summary>
public sealed class SqliteConnectionStringTests
{
    private static SqliteConnectionConfiguration Configuration(int? defaultTimeout)
        => new() { Name = "Work", DataSource = "work.db", DefaultTimeout = defaultTimeout };

    private static SqliteConnectionFactory Factory(Lazy<IEffectiveSettingsProvider>? settings)
        => new(
            NullLogger<SqliteConnectionFactory>.Instance,
            NullLogger<SqliteConnection>.Instance,
            new Mock<ISecretManagerProvider>().Object,
            settings);

    private static Lazy<IEffectiveSettingsProvider> SettingHolds(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = seconds;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(true);
        return new Lazy<IEffectiveSettingsProvider>(() => settings.Object);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(120)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheResolvedTimeoutIsInTheConnectionStringWhateverItIs(int seconds)
    {
        var connectionString = SqliteConnectionFactory.BuildConnectionString(Configuration(seconds), seconds, password: null);

        new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DefaultTimeout.ShouldBe(seconds);
        connectionString.ShouldContain($"Default Timeout={seconds}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandOnAConnectionTakesTheConnectionStringsTimeout()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(SqliteConnectionFactory.BuildConnectionString(Configuration(17), 17, password: null));
        using var command = new Microsoft.Data.Sqlite.SqliteCommand("SELECT 1");

        command.Connection = connection;

        command.CommandTimeout.ShouldBe(17);
    }

    [Theory]
    [InlineData(1000, 1)]
    [InlineData(1500, 2)]
    [InlineData(2000, 2)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandGivenItsOwnTimeoutTakesItInWholeSecondsRoundedUp(int milliseconds, int seconds)
    {
        using var command = new Microsoft.Data.Sqlite.SqliteCommand("SELECT 1");

        SqliteConnection.Limit(command, TimeSpan.FromMilliseconds(milliseconds));

        command.CommandTimeout.ShouldBe(seconds);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesATimeoutIsBuiltWithoutReadingTheSetting()
    {
        var settings = SettingHolds(99);

        var created = Factory(settings).Create(Configuration(45), resolvedSecret: null);

        created.IsSuccess.ShouldBeTrue();
        settings.IsValueCreated.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoTimeoutIsBuiltFromTheInstancesSetting()
    {
        var created = Factory(SettingHolds(60)).Create(Configuration(null), resolvedSecret: null);

        created.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoTimeoutInAHostWithNoSettingsIsNotBuiltAndTheFailureNamesIt()
    {
        var created = Factory(null).Create(Configuration(null), resolvedSecret: null);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("SETTINGS-62031");
        created.Messages[^1].Message.ShouldContain("connection 'Work'");
        created.Messages[^1].Message.ShouldContain(SettingDefinitions.DefaultCommandTimeoutSeconds);
    }
}
