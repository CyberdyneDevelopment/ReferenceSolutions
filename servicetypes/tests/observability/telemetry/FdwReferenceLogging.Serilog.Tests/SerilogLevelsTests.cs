using FdwReferenceLogging.Serilog;
using FdwReferenceSerilog.Levels;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;

namespace FdwReferenceLogging.Serilog.Tests;

/// <summary>
/// A Serilog row's MinimumLevel and override levels are SerilogLevels options (FDW-4153): each carries the Serilog level it is, and the
/// factory refuses a name that is none of them instead of parsing an enum.
/// </summary>
public sealed class SerilogLevelsTests
{
    public static TheoryData<string, LogEventLevel> Levels => new()
    {
        { "Verbose", LogEventLevel.Verbose },
        { "Debug", LogEventLevel.Debug },
        { "Information", LogEventLevel.Information },
        { "Warning", LogEventLevel.Warning },
        { "Error", LogEventLevel.Error },
        { "Fatal", LogEventLevel.Fatal },
    };

    [Theory]
    [Trait("Priority", "P1")]
    [MemberData(nameof(Levels))]
    public void EachLevelCarriesItsSerilogValue(string name, LogEventLevel expected)
    {
        var level = SerilogLevels.ByName(name);

        ReferenceEquals(level, SerilogLevels.NotFound).ShouldBeFalse();
        level.SerilogLevel.ShouldBe(expected);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("information")]
    [InlineData("Trace")]
    [InlineData("")]
    public void ANameThatIsNoLevelIsNotFound(string name)
        => ReferenceEquals(SerilogLevels.ByName(name), SerilogLevels.NotFound).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void TheFactoryRefusesAMinimumLevelThatIsNoLevel()
    {
        var result = new SerilogLoggingFactory(NullLoggerFactory.Instance).Create(
            new SerilogLoggingConfiguration { Name = "Logging", MinimumLevel = "Loud" });

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheFactoryRefusesAnOverrideLevelThatIsNoLevel()
    {
        var configuration = new SerilogLoggingConfiguration { Name = "Logging", MinimumLevel = "Information" };
        configuration.Overrides.Add(new SerilogLevelOverride { SourceContext = "Microsoft", Level = "Quiet" });

        var result = new SerilogLoggingFactory(NullLoggerFactory.Instance).Create(configuration);

        result.IsSuccess.ShouldBeFalse();
    }
}
