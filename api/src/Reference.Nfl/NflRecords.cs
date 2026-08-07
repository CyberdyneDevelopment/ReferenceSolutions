using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Reference.Nfl;

/// <summary>
/// POCO record for the nfl.Team table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class TeamRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public int FoundedYear { get; set; }
    public string StadiumName { get; set; } = string.Empty;
    public string HeadCoach { get; set; } = string.Empty;
    public DateTimeOffset CreateDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string CreateOnBehalfOf { get; set; } = string.Empty;
    public DateTimeOffset ModifyDate { get; set; }
    public string ModifyBy { get; set; } = string.Empty;
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}

/// <summary>
/// POCO record for the nfl.Player table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class PlayerRecord
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public int HeightInches { get; set; }
    public int WeightLbs { get; set; }
    public string College { get; set; } = string.Empty;
    public int DraftYear { get; set; }
    public int DraftRound { get; set; }
    public int DraftPick { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string CreateOnBehalfOf { get; set; } = string.Empty;
    public DateTimeOffset ModifyDate { get; set; }
    public string ModifyBy { get; set; } = string.Empty;
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}

/// <summary>
/// POCO record for the nfl.Game table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class GameRecord
{
    public Guid Id { get; set; }
    public int Season { get; set; }
    public int Week { get; set; }
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Stadium { get; set; } = string.Empty;
    public DateTimeOffset GameDate { get; set; }
    public bool IsPlayoff { get; set; }
    public bool IsOvertime { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string CreateOnBehalfOf { get; set; } = string.Empty;
    public DateTimeOffset ModifyDate { get; set; }
    public string ModifyBy { get; set; } = string.Empty;
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}

/// <summary>
/// POCO record for the nfl.PlayerGameStat table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class PlayerGameStatRecord
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public int PassingYards { get; set; }
    public int PassingTDs { get; set; }
    public int Interceptions { get; set; }
    public int RushingYards { get; set; }
    public int RushingTDs { get; set; }
    public int Receptions { get; set; }
    public int ReceivingYards { get; set; }
    public int ReceivingTDs { get; set; }
    public int Tackles { get; set; }
    public decimal Sacks { get; set; }
    public int FumblesForced { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string CreateOnBehalfOf { get; set; } = string.Empty;
    public DateTimeOffset ModifyDate { get; set; }
    public string ModifyBy { get; set; } = string.Empty;
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}

/// <summary>
/// POCO record for the nfl.SeasonStanding table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class SeasonStandingRecord
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public int Season { get; set; }
    public string Conference { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int DivisionRank { get; set; }
    public int? PlayoffSeed { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string CreateOnBehalfOf { get; set; } = string.Empty;
    public DateTimeOffset ModifyDate { get; set; }
    public string ModifyBy { get; set; } = string.Empty;
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
