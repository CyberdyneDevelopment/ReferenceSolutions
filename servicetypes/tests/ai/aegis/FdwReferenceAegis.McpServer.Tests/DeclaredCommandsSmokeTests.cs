using Fdw.Aegis.Configuration;

namespace FdwReferenceAegis.McpServer.Tests;

/// <summary>
/// The startup read <c>Program</c> performs: the AegisCommand domain's provider reads the declared FileSystem
/// connection, with the PreApproved command's allow-list loading with it.
/// </summary>
public sealed class DeclaredCommandsSmokeTests : IClassFixture<DeclaredConfigurationSourceFixture>
{
    private readonly DeclaredConfigurationSourceFixture _fixture;

    public DeclaredCommandsSmokeTests(DeclaredConfigurationSourceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetReadsTheDeclaredCommandsThroughTheDeclaredConnection()
    {
        var commands = await _fixture.Commands.Get(TestContext.Current.CancellationToken);

        commands.IsSuccess.ShouldBeTrue(commands.CurrentMessage);
        commands.Value!.Select(command => command.Name)
            .ShouldBe(["echo_credential", "echo_adhoc"], ignoreOrder: true);
    }

    [Fact]
    public async Task GetLoadsThePreApprovedAllowListWithTheCommand()
    {
        var commands = await _fixture.Commands.Get(TestContext.Current.CancellationToken);

        var preApproved = commands.Value!.OfType<PreApprovedCommandConfiguration>().ShouldHaveSingleItem();
        preApproved.SecretManagerName.ShouldBe("EnvSecrets");
        var entry = preApproved.ParameterAllowList.ShouldHaveSingleItem();
        entry.ParameterName.ShouldBe("mode");
        entry.Required.ShouldBeTrue();
        entry.PermittedValues.Select(value => value.Value).ShouldBe(["echo"]);
    }

    [Fact]
    public async Task GetReadsTheAdHocImplementationBody()
    {
        var commands = await _fixture.Commands.Get(TestContext.Current.CancellationToken);

        var adHoc = commands.Value!.OfType<AdHocCommandConfiguration>().ShouldHaveSingleItem();
        adHoc.Name.ShouldBe("echo_adhoc");
        adHoc.ConnectionName.ShouldBe("synthetic-echo");
        adHoc.RequiresFreshVerdict.ShouldBeFalse();
    }
}
