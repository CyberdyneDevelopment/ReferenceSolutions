using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
using System;
using Fdw.Services.Connections.Sql;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// The SQL Server connection type declares the default of how many parameters a key list carries before it goes to a temporary table, the factory resolves it
/// with the connection's own number, and the connection tells its translators (FDW-4267).
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionTypeParameterLimitTests
{
    private static MsSqlConnectionConfiguration Configuration(int? maxParametersBeforeTempTable)
        => new()
        {
            Name = "Warehouse",
            MaxSerialInsert = 1000,
            MaxParametersBeforeTempTable = maxParametersBeforeTempTable,
            CommandTimeoutSeconds = 30,
            ConnectionTimeoutSeconds = 15,
            Server = "localhost",
            Database = "TestDb",
            AuthenticationType = "WindowsAuth"
        };

    private static MsSqlConnectionFactory Factory(int? declaredDefault)
        => new(
            NullLogger<MsSqlConnectionFactory>.Instance,
            NullLogger<MsSqlConnection>.Instance,
            new Mock<ISecretManagerProvider>().Object,
            authenticationContextAccessor: null,
            settings: null)
        {
            DefaultMaxParametersBeforeTempTable = declaredDefault,
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheTypeDeclaresALowDefaultOnItsServiceTypeOption()
    {
        ServiceTypeDefaults.TryResolve(new MsSqlConnectionType(), nameof(IDatabaseConnectionConfiguration.MaxParametersBeforeTempTable), out var declared).ShouldBeTrue();
        declared.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ATypeThatDeclaresNoDefaultIsRefusedWithItsOwnCode()
    {
        var refusal = Fdw.Commands.Data.Abstractions.Logging.DatabaseCommandLog.MaxParametersBeforeTempTableNotDeclared(NullLogger.Instance, "MsSql");

        refusal.Code.ShouldBe("DATAABSTRACTIONS-61155");
        refusal.Message.ShouldContain("MsSql connection type declares no integer default for MaxParametersBeforeTempTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesNoNumberTakesTheDefaultTheTypeDeclares()
    {
        var created = Factory(40).Create(Configuration(null), resolvedSecret: null);

        created.IsSuccess.ShouldBeTrue();
        created.Value.ShouldBeOfType<MsSqlConnection>().TranslationSettings.ShouldBe(new TranslationSettings(40));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AConnectionThatStatesANumberTakesItAndNotTheDefault()
    {
        var created = Factory(40).Create(Configuration(12), resolvedSecret: null);

        created.Value.ShouldBeOfType<MsSqlConnection>().TranslationSettings.ShouldBe(new TranslationSettings(12));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AFactoryBuiltWithoutTheTypesDefaultRefusesToBuildAConnectionAndNamesTheKind()
    {
        var created = Factory(null).Create(Configuration(null), resolvedSecret: null);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldBe("DATAABSTRACTIONS-61155");
        created.Messages[^1].Message.ShouldContain("MsSql connection type declares no integer default");
    }
}
