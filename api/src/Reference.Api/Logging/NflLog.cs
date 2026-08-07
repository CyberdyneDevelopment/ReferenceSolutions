using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for NFL endpoints.
/// EventId range: 9000-9099
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class NflLog
{
    // List Teams (9000-9003)
    [MessageLogging(EventId = 9000, Level = LogLevel.Trace, Message = "Listing NFL teams")]
    public static partial IGenericMessage ListingTeams(ILogger logger);

    [MessageLogging(EventId = 9001, Level = LogLevel.Information, Message = "Found {count} NFL teams")]
    public static partial IGenericMessage TeamsFound(ILogger logger, int count);

    [MessageLogging(EventId = 9002, Level = LogLevel.Error, Message = "Failed to list NFL teams: {message}")]
    public static partial IGenericMessage ListTeamsFailed(ILogger logger, string message);

    // Get Team Roster (9004-9007)
    [MessageLogging(EventId = 9004, Level = LogLevel.Trace, Message = "Getting roster for team {teamId}")]
    public static partial IGenericMessage GettingTeamRoster(ILogger logger, Guid teamId);

    [MessageLogging(EventId = 9005, Level = LogLevel.Information, Message = "Found {count} players for team {teamId}")]
    public static partial IGenericMessage TeamRosterFound(ILogger logger, int count, Guid teamId);

    [MessageLogging(EventId = 9006, Level = LogLevel.Warning, Message = "No roster found for team {teamId}")]
    public static partial IGenericMessage TeamRosterNotFound(ILogger logger, Guid teamId);

    [MessageLogging(EventId = 9007, Level = LogLevel.Error, Message = "Failed to get roster for team {teamId}: {message}")]
    public static partial IGenericMessage GetTeamRosterFailed(ILogger logger, Guid teamId, string message);

    // List Games (9008-9010)
    [MessageLogging(EventId = 9008, Level = LogLevel.Trace, Message = "Listing NFL games")]
    public static partial IGenericMessage ListingGames(ILogger logger);

    [MessageLogging(EventId = 9009, Level = LogLevel.Information, Message = "Found {count} NFL games")]
    public static partial IGenericMessage GamesFound(ILogger logger, int count);

    [MessageLogging(EventId = 9010, Level = LogLevel.Error, Message = "Failed to list NFL games: {message}")]
    public static partial IGenericMessage ListGamesFailed(ILogger logger, string message);

    // Get Game Boxscore (9011-9014)
    [MessageLogging(EventId = 9011, Level = LogLevel.Trace, Message = "Getting boxscore for game {gameId}")]
    public static partial IGenericMessage GettingGameBoxscore(ILogger logger, Guid gameId);

    [MessageLogging(EventId = 9012, Level = LogLevel.Information, Message = "Found {count} stat records for game {gameId}")]
    public static partial IGenericMessage GameBoxscoreFound(ILogger logger, int count, Guid gameId);

    [MessageLogging(EventId = 9013, Level = LogLevel.Warning, Message = "No stats found for game {gameId}")]
    public static partial IGenericMessage GameBoxscoreNotFound(ILogger logger, Guid gameId);

    [MessageLogging(EventId = 9014, Level = LogLevel.Error, Message = "Failed to get boxscore for game {gameId}: {message}")]
    public static partial IGenericMessage GetGameBoxscoreFailed(ILogger logger, Guid gameId, string message);

    // List Standings (9015-9017)
    [MessageLogging(EventId = 9015, Level = LogLevel.Trace, Message = "Listing NFL standings")]
    public static partial IGenericMessage ListingStandings(ILogger logger);

    [MessageLogging(EventId = 9016, Level = LogLevel.Information, Message = "Found {count} standings")]
    public static partial IGenericMessage StandingsFound(ILogger logger, int count);

    [MessageLogging(EventId = 9017, Level = LogLevel.Error, Message = "Failed to list NFL standings: {message}")]
    public static partial IGenericMessage ListStandingsFailed(ILogger logger, string message);

    // Get Player Stats (9018-9021)
    [MessageLogging(EventId = 9018, Level = LogLevel.Trace, Message = "Getting stats for player {playerId}")]
    public static partial IGenericMessage GettingPlayerStats(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9019, Level = LogLevel.Information, Message = "Found {count} stat records for player {playerId}")]
    public static partial IGenericMessage PlayerStatsFound(ILogger logger, int count, Guid playerId);

    [MessageLogging(EventId = 9020, Level = LogLevel.Warning, Message = "No stats found for player {playerId}")]
    public static partial IGenericMessage PlayerStatsNotFound(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9021, Level = LogLevel.Error, Message = "Failed to get stats for player {playerId}: {message}")]
    public static partial IGenericMessage GetPlayerStatsFailed(ILogger logger, Guid playerId, string message);

    // Create Player (9022-9025)
    [MessageLogging(EventId = 9022, Level = LogLevel.Debug, Message = "Creating NFL player '{firstName} {lastName}' for team {teamId}")]
    public static partial IGenericMessage CreatingPlayer(ILogger logger, string firstName, string lastName, Guid teamId);

    [MessageLogging(EventId = 9023, Level = LogLevel.Information, Message = "Created NFL player {playerId} '{firstName} {lastName}'")]
    public static partial IGenericMessage PlayerCreated(ILogger logger, Guid playerId, string firstName, string lastName);

    [MessageLogging(EventId = 9024, Level = LogLevel.Error, Message = "Failed to create NFL player '{firstName} {lastName}': {message}")]
    public static partial IGenericMessage CreatePlayerFailed(ILogger logger, string firstName, string lastName, string message);

    // Delete Player (9026-9030)
    [MessageLogging(EventId = 9026, Level = LogLevel.Debug, Message = "Deleting NFL player {playerId}")]
    public static partial IGenericMessage DeletingPlayer(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9027, Level = LogLevel.Information, Message = "Deleted NFL player {playerId}")]
    public static partial IGenericMessage PlayerDeleted(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9028, Level = LogLevel.Warning, Message = "NFL player {playerId} not found for deletion")]
    public static partial IGenericMessage DeletePlayerNotFound(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9029, Level = LogLevel.Error, Message = "Failed to delete NFL player {playerId}: {message}")]
    public static partial IGenericMessage DeletePlayerFailed(ILogger logger, Guid playerId, string message);

    // Delete Stat (9031-9034)
    [MessageLogging(EventId = 9031, Level = LogLevel.Debug, Message = "Deleting NFL stat record {statId}")]
    public static partial IGenericMessage DeletingStat(ILogger logger, Guid statId);

    [MessageLogging(EventId = 9032, Level = LogLevel.Information, Message = "Deleted NFL stat record {statId}")]
    public static partial IGenericMessage StatDeleted(ILogger logger, Guid statId);

    [MessageLogging(EventId = 9033, Level = LogLevel.Warning, Message = "NFL stat record {statId} not found for deletion")]
    public static partial IGenericMessage DeleteStatNotFound(ILogger logger, Guid statId);

    // Create Player Game Stat (9035-9038)
    [MessageLogging(EventId = 9035, Level = LogLevel.Debug, Message = "Creating stat record for player {playerId} in game {gameId}")]
    public static partial IGenericMessage CreatingPlayerGameStat(ILogger logger, Guid playerId, Guid gameId);

    [MessageLogging(EventId = 9036, Level = LogLevel.Information, Message = "Created stat record {statId} for player {playerId} in game {gameId}")]
    public static partial IGenericMessage PlayerGameStatCreated(ILogger logger, Guid statId, Guid playerId, Guid gameId);

    [MessageLogging(EventId = 9037, Level = LogLevel.Error, Message = "Failed to create stat record for player {playerId} in game {gameId}: {message}")]
    public static partial IGenericMessage CreatePlayerGameStatFailed(ILogger logger, Guid playerId, Guid gameId, string message);

    // Retire Player (9039-9043)
    [MessageLogging(EventId = 9039, Level = LogLevel.Debug, Message = "Retiring NFL player {playerId}")]
    public static partial IGenericMessage RetiringPlayer(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9040, Level = LogLevel.Information, Message = "Retired NFL player {playerId}")]
    public static partial IGenericMessage PlayerRetired(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9041, Level = LogLevel.Warning, Message = "NFL player {playerId} not found for retirement")]
    public static partial IGenericMessage RetirePlayerNotFound(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9042, Level = LogLevel.Error, Message = "Failed to retire NFL player {playerId}: {message}")]
    public static partial IGenericMessage RetirePlayerFailed(ILogger logger, Guid playerId, string message);

    // Transfer Player (9044-9048)
    [MessageLogging(EventId = 9044, Level = LogLevel.Debug, Message = "Transferring NFL player {playerId} to team {teamId}")]
    public static partial IGenericMessage TransferringPlayer(ILogger logger, Guid playerId, Guid teamId);

    [MessageLogging(EventId = 9045, Level = LogLevel.Information, Message = "Transferred NFL player {playerId} to team {teamId}")]
    public static partial IGenericMessage PlayerTransferred(ILogger logger, Guid playerId, Guid teamId);

    [MessageLogging(EventId = 9046, Level = LogLevel.Warning, Message = "NFL player {playerId} not found for transfer")]
    public static partial IGenericMessage TransferPlayerNotFound(ILogger logger, Guid playerId);

    [MessageLogging(EventId = 9047, Level = LogLevel.Error, Message = "Failed to transfer NFL player {playerId}: {message}")]
    public static partial IGenericMessage TransferPlayerFailed(ILogger logger, Guid playerId, string message);

    // General (9050-9052)
    [MessageLogging(EventId = 9050, Level = LogLevel.Error, Message = "NFL operation '{operation}' failed: {details}")]
    public static partial IGenericMessage NflOperationFailed(ILogger logger, string operation, string details);
}
