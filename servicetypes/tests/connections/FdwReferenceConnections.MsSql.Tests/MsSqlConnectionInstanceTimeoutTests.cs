using System;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// The command timeout of a SQL Server connection comes from the connection or from the instance's setting DefaultCommandTimeoutSeconds, and
/// a connection that has neither is not built (FDW-3284).
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionInstanceTimeoutTests
{
    private static MsSqlConnectionConfiguration Configuration(int? commandTimeoutSeconds)
        => new()
        {
            Name = "Warehouse",
            MaxSerialInsert = 1000,
            CommandTimeoutSeconds = commandTimeoutSeconds,
            // The connect timeout is resolved first and is not what these tests read (FDW-3580): it is stated so the command timeout is reached.
            ConnectionTimeoutSeconds = 15,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

    private static MsSqlConnectionFactory Factory(Lazy<IEffectiveSettingsProvider>? settings)
        => new(
            NullLogger<MsSqlConnectionFactory>.Instance,
            NullLogger<MsSqlConnection>.Instance,
            new Mock<ISecretManagerProvider>().Object,
            authenticationContextAccessor: null,
            settings)
        {
            DefaultMaxParametersBeforeTempTable = 50,
        };

    private static Lazy<IEffectiveSettingsProvider> SettingHolds(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = seconds;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(true);
        return new Lazy<IEffectiveSettingsProvider>(() => settings.Object);
    }

    private static Lazy<IEffectiveSettingsProvider> SettingAbsent()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = 0;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(false);
        return new Lazy<IEffectiveSettingsProvider>(() => settings.Object);
    }

    private static int CommandTimeoutOf(MsSqlConnection connection)
    {
        using var command = new SqlCommand("SELECT 1");
        using var sqlConnection = new SqlConnection();
        connection.Attach(command, sqlConnection);
        return command.CommandTimeout;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesATimeoutTakesItAndNeverReadsTheSetting()
    {
        var settings = SettingHolds(99);

        var created = Factory(settings).Create(Configuration(45), resolvedSecret: null);

        created.IsSuccess.ShouldBeTrue();
        CommandTimeoutOf(created.Value.ShouldBeOfType<MsSqlConnection>()).ShouldBe(45);
        settings.IsValueCreated.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoTimeoutTakesTheInstancesSetting()
    {
        var created = Factory(SettingHolds(60)).Create(Configuration(null), resolvedSecret: null);

        created.IsSuccess.ShouldBeTrue();
        CommandTimeoutOf(created.Value.ShouldBeOfType<MsSqlConnection>()).ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoTimeoutAndFindsNoSettingIsNotBuiltAndTheFailureNamesBoth()
    {
        var created = Factory(SettingAbsent()).Create(Configuration(null), resolvedSecret: null);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("SETTINGS-62030");
        created.Messages[^1].Message.ShouldContain("connection 'Warehouse'");
        created.Messages[^1].Message.ShouldContain(SettingDefinitions.DefaultCommandTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoTimeoutInAHostWithNoSettingsIsNotBuilt()
    {
        var created = Factory(null).Create(Configuration(null), resolvedSecret: null);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("SETTINGS-62031");
        created.Messages[^1].Message.ShouldContain("connection 'Warehouse'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesATimeoutThatIsNotPositiveIsNotBuilt()
    {
        var created = Factory(SettingHolds(30)).Create(Configuration(0), resolvedSecret: null);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("SETTINGS-62032");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheConnectionThatReachesPlatformConfigurationStatesItsOwnTimeoutBecauseTheSettingsAreReadThroughIt()
    {
        var factory = new MsSqlConnectionFactory(NullLogger<MsSqlConnectionFactory>.Instance, NullLogger<MsSqlConnection>.Instance) { DefaultMaxParametersBeforeTempTable = 50 };

        var withoutOne = factory.Create(Configuration(null), resolvedSecret: null);
        var withOne = factory.Create(Configuration(30), resolvedSecret: null);

        withoutOne.IsFailure.ShouldBeTrue();
        withoutOne.Messages[^1].Code.ShouldBe("SETTINGS-62031");
        withOne.IsSuccess.ShouldBeTrue();
    }
}
