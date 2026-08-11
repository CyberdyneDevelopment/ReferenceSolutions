using System.Diagnostics.CodeAnalysis;
using System;
using FastEndpoints;

namespace ReferenceNfl.Endpoints;

// =============================================================================
// Route Parameter Request DTOs
// =============================================================================

/// <summary>
/// Request containing a TeamId route parameter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TeamIdRequest
{
    /// <summary>
    /// Gets or sets the team identifier.
    /// </summary>
    public Guid TeamId { get; set; }
}

/// <summary>
/// Request containing a GameId route parameter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GameIdRequest
{
    /// <summary>
    /// Gets or sets the game identifier.
    /// </summary>
    public Guid GameId { get; set; }
}

/// <summary>
/// Request containing a PlayerId route parameter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PlayerIdRequest
{
    /// <summary>
    /// Gets or sets the player identifier.
    /// </summary>
    public Guid PlayerId { get; set; }
}

/// <summary>
/// Request containing a StatId route parameter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class StatIdRequest
{
    /// <summary>
    /// Gets or sets the stat identifier.
    /// </summary>
    public Guid StatId { get; set; }
}

// =============================================================================
// Query Parameter Request DTOs
// =============================================================================

/// <summary>
/// Request for listing teams with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTeamsRequest
{
    /// <summary>
    /// Gets or sets the optional conference filter (AFC or NFC).
    /// </summary>
    [QueryParam]
    public string? Conference { get; set; }

    /// <summary>
    /// Gets or sets the optional division filter (North, South, East, West).
    /// </summary>
    [QueryParam]
    public string? Division { get; set; }
}

/// <summary>
/// Request for listing a team roster with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListRosterRequest
{
    /// <summary>
    /// Gets or sets the team identifier from the route.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the optional position filter.
    /// </summary>
    [QueryParam]
    public string? Position { get; set; }

    /// <summary>
    /// Gets or sets the optional active status filter.
    /// </summary>
    [QueryParam]
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request for listing games with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGamesRequest
{
    /// <summary>
    /// Gets or sets the optional season filter.
    /// </summary>
    [QueryParam]
    public int? Season { get; set; }

    /// <summary>
    /// Gets or sets the optional week filter.
    /// </summary>
    [QueryParam]
    public int? Week { get; set; }

    /// <summary>
    /// Gets or sets the optional team filter (matches home or away).
    /// </summary>
    [QueryParam]
    public Guid? TeamId { get; set; }
}

/// <summary>
/// Request for listing standings with optional filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListStandingsRequest
{
    /// <summary>
    /// Gets or sets the optional season filter.
    /// </summary>
    [QueryParam]
    public int? Season { get; set; }

    /// <summary>
    /// Gets or sets the optional conference filter.
    /// </summary>
    [QueryParam]
    public string? Conference { get; set; }
}

/// <summary>
/// Request for player stats with optional season filter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PlayerStatsRequest
{
    /// <summary>
    /// Gets or sets the player identifier from the route.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the optional season filter.
    /// </summary>
    [QueryParam]
    public int? Season { get; set; }
}

// =============================================================================
// Write Request DTOs
// =============================================================================

/// <summary>
/// Request for creating a new player.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreatePlayerRequest
{
    /// <summary>
    /// Gets or sets the team to assign the player to.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the player first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the player last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the player position.
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the jersey number.
    /// </summary>
    public int JerseyNumber { get; set; }

    /// <summary>
    /// Gets or sets the player height in inches.
    /// </summary>
    public int HeightInches { get; set; }

    /// <summary>
    /// Gets or sets the player weight in pounds.
    /// </summary>
    public int WeightLbs { get; set; }

    /// <summary>
    /// Gets or sets the college the player attended.
    /// </summary>
    public string College { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the draft year.
    /// </summary>
    public int DraftYear { get; set; }

    /// <summary>
    /// Gets or sets the draft round.
    /// </summary>
    public int DraftRound { get; set; }

    /// <summary>
    /// Gets or sets the draft pick number.
    /// </summary>
    public int DraftPick { get; set; }
}

/// <summary>
/// Request for creating a player game stat record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateStatRequest
{
    /// <summary>
    /// Gets or sets the game identifier (from route).
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Gets or sets the player identifier.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the passing yards.
    /// </summary>
    public int PassingYards { get; set; }

    /// <summary>
    /// Gets or sets the passing touchdowns.
    /// </summary>
    public int PassingTDs { get; set; }

    /// <summary>
    /// Gets or sets the interceptions thrown.
    /// </summary>
    public int Interceptions { get; set; }

    /// <summary>
    /// Gets or sets the rushing yards.
    /// </summary>
    public int RushingYards { get; set; }

    /// <summary>
    /// Gets or sets the rushing touchdowns.
    /// </summary>
    public int RushingTDs { get; set; }

    /// <summary>
    /// Gets or sets the number of receptions.
    /// </summary>
    public int Receptions { get; set; }

    /// <summary>
    /// Gets or sets the receiving yards.
    /// </summary>
    public int ReceivingYards { get; set; }

    /// <summary>
    /// Gets or sets the receiving touchdowns.
    /// </summary>
    public int ReceivingTDs { get; set; }

    /// <summary>
    /// Gets or sets the tackles.
    /// </summary>
    public int Tackles { get; set; }

    /// <summary>
    /// Gets or sets the sacks.
    /// </summary>
    public decimal Sacks { get; set; }

    /// <summary>
    /// Gets or sets the fumbles forced.
    /// </summary>
    public int FumblesForced { get; set; }
}

/// <summary>
/// Request for transferring a player to a new team.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TransferPlayerRequest
{
    /// <summary>
    /// Gets or sets the player identifier (from route).
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the target team identifier.
    /// </summary>
    public Guid TeamId { get; set; }
}
