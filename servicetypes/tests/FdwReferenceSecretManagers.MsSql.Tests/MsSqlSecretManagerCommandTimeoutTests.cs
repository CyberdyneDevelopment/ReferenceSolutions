using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Timeouts;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using FdwReferenceSecretManagers.MsSql.Configuration;
using FdwReferenceSecretManagers.MsSql.Handlers;
using FdwReferenceSecretManagers.MsSql.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FdwReferenceSecretManagers.MsSql.Tests;

/// <summary>
/// Every command the SQL Server secret manager sends through the gateway carries the manager's command timeout and its name, so the
/// connection of its data store applies that limit to the lookup (FDW-3324).
/// </summary>
public sealed class MsSqlSecretManagerCommandTimeoutTests
{
    private const int Seconds = 7;
    private const string Owner = "secret manager 'Vault'";

    private static (MsSqlSecretManager Manager, List<IDataCommand> Sent) ManagerOverARecordingGateway()
    {
        var sent = new List<IDataCommand>();
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<SecretRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => sent.Add(command))
            .ReturnsAsync(GenericResult<IEnumerable<SecretRow>>.Success([]));
        gateway
            .Setup(g => g.Execute(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => sent.Add(command))
            .ReturnsAsync(GenericResult.Success());

        var configuration = new MsSqlSecretManagerConfiguration { Id = Guid.NewGuid(), Schema = "dbo", TableName = "Secret" };
        var manager = new MsSqlSecretManager(NullLogger<MsSqlSecretManager>.Instance, configuration, gateway.Object, "Store", Seconds, "Vault");
        return (manager, sent);
    }

    private static void EveryCommandCarriesTheManagersTimeout(List<IDataCommand> sent)
    {
        sent.ShouldNotBeEmpty();
        foreach (var command in sent)
        {
            CommandTimeoutPolicy.TryGet(command, out var timeout, out var owner).ShouldBeTrue();
            timeout.ShouldBe(TimeSpan.FromSeconds(Seconds));
            owner.ShouldBe(Owner);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGetSendsAQueryThatCarriesTheManagersTimeout()
    {
        var (manager, sent) = ManagerOverARecordingGateway();

        await manager.Execute(GetSecretManagerCommand.Latest(null, "key"), TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(1);
        EveryCommandCarriesTheManagersTimeout(sent);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AListSendsAQueryThatCarriesTheManagersTimeout()
    {
        var (manager, sent) = ManagerOverARecordingGateway();

        await manager.Execute(ListSecretsManagerCommand.All(null), TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(1);
        EveryCommandCarriesTheManagersTimeout(sent);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteSendsAnUpdateThatCarriesTheManagersTimeout()
    {
        var (manager, sent) = ManagerOverARecordingGateway();

        await manager.Execute(new DeleteSecretManagerCommand(null, "key"), TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(1);
        EveryCommandCarriesTheManagersTimeout(sent);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASetSendsTheDeactivatingUpdateAndTheInsertBothCarryingTheManagersTimeout()
    {
        var (manager, sent) = ManagerOverARecordingGateway();

        await manager.Execute(
            new SetSecretManagerCommand(
                container: null,
                secretKey: "key",
                secretValue: "value",
                parameters: new Dictionary<string, object?>(StringComparer.Ordinal) { ["SecretType"] = "Password" }),
            TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(2);
        EveryCommandCarriesTheManagersTimeout(sent);
    }
}
