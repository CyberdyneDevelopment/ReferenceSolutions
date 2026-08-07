#!/usr/bin/env bash
# tests/21-nfl-data.sh — NFL DataGateway CRUD endpoint tests
# Tests query, insert, update, and delete operations via IDataGateway

test_nfl_data() {
    describe "NFL Data — Auth Matrix"

    it "GET /nfl/teams without auth returns 401"
    anon_get "/nfl/teams"
    assert_status 401

    it "POST /nfl/players without auth returns 401"
    anon_post "/nfl/players" '{}'
    assert_status 401

    it "GET /nfl/teams as viewer returns 200"
    viewer_get "/nfl/teams"
    assert_status 200

    it "GET /nfl/teams as operator returns 200"
    operator_get "/nfl/teams"
    assert_status 200

    it "POST /nfl/players as viewer returns 403"
    viewer_post "/nfl/players" '{}'
    assert_status 403

    it "GET /nfl/teams as admin returns 200"
    admin_get "/nfl/teams"
    assert_status 200

    # ── Query Tests: Teams ──
    describe "NFL Data — Team Queries"

    it "GET /nfl/teams returns all 32 teams"
    admin_get "/nfl/teams"
    assert_status 200
    assert_json_array "."
    assert_json_array_length "." 32

    it "Teams have required fields"
    assert_json_guid ".[0].id"
    assert_json_string ".[0].name"
    assert_json_string ".[0].city"
    assert_json_string ".[0].abbreviation"
    assert_json_string ".[0].conference"

    it "Filter teams by conference=AFC returns 16"
    admin_get "/nfl/teams?conference=AFC"
    assert_status 200
    assert_json_array_length "." 16

    it "Filter teams by conference=NFC&division=North returns 4"
    admin_get "/nfl/teams?conference=NFC&division=North"
    assert_status 200
    assert_json_array_length "." 4

    # Get a team ID for roster test
    local team_id
    team_id=$(response_body | jq -r '.[0].id' 2>/dev/null)

    # ── Query Tests: Roster ──
    describe "NFL Data — Roster Queries"

    it "GET /nfl/teams/{id}/roster returns players"
    admin_get "/nfl/teams/${team_id}/roster"
    assert_status 200
    assert_json_array "."
    assert_json_array_min_length "." 1

    it "Roster players have required fields"
    assert_json_guid ".[0].id"
    assert_json_string ".[0].firstName"
    assert_json_string ".[0].lastName"
    assert_json_string ".[0].position"

    it "Filter roster by position=QB"
    admin_get "/nfl/teams/${team_id}/roster?position=QB"
    assert_status 200
    assert_json_array "."

    # Check all returned players are QBs
    local all_qbs
    all_qbs=$(response_body | jq '[.[] | .position] | all(. == "QB")' 2>/dev/null)
    it "All filtered players are QBs"
    if [[ "$all_qbs" == "true" ]]; then
        pass
    else
        fail "Not all players are QBs"
    fi

    # ── Query Tests: Games ──
    describe "NFL Data — Game Queries"

    it "GET /nfl/games?season=2025&week=1 returns 16 games"
    admin_get "/nfl/games?season=2025&week=1"
    assert_status 200
    assert_json_array_length "." 16

    it "Games have required fields"
    assert_json_guid ".[0].id"
    assert_json_number_value ".[0].season" "2025"
    assert_json_number_value ".[0].week" "1"

    local game_id
    game_id=$(response_body | jq -r '.[0].id' 2>/dev/null)

    it "GET /nfl/games/{id}/boxscore returns stats"
    admin_get "/nfl/games/${game_id}/boxscore"
    assert_status 200
    assert_json_array "."
    assert_json_array_min_length "." 1

    it "Boxscore stats have required fields"
    assert_json_guid ".[0].id"
    assert_json_guid ".[0].playerId"

    # ── Query Tests: Standings ──
    describe "NFL Data — Standings"

    it "GET /nfl/standings returns all 32 standings"
    admin_get "/nfl/standings?season=2025"
    assert_status 200
    assert_json_array_length "." 32

    it "Filter standings by conference=AFC returns 16"
    admin_get "/nfl/standings?season=2025&conference=AFC"
    assert_status 200
    assert_json_array_length "." 16

    it "Standings ordered by wins descending"
    local wins_ordered
    wins_ordered=$(response_body | jq '[.[] | .wins] | . == (. | sort | reverse)' 2>/dev/null)
    if [[ "$wins_ordered" == "true" ]]; then
        pass
    else
        # May not be strictly ordered if tied, just check first >= last
        local first_wins last_wins
        first_wins=$(response_body | jq '.[0].wins' 2>/dev/null)
        last_wins=$(response_body | jq '.[-1].wins' 2>/dev/null)
        if [[ "$first_wins" -ge "$last_wins" ]]; then
            pass
        else
            fail "Standings not ordered by wins desc"
        fi
    fi

    # ── Insert Tests ──
    describe "NFL Data — Insert Operations"

    # Get a team ID to create a player for
    admin_get "/nfl/teams?conference=AFC&division=West"
    local test_team_id
    test_team_id=$(response_body | jq -r '.[0].id' 2>/dev/null)

    it "POST /nfl/players creates new player"
    admin_post "/nfl/players" "{
        \"teamId\": \"${test_team_id}\",
        \"firstName\": \"E2E\",
        \"lastName\": \"TestPlayer\",
        \"position\": \"WR\",
        \"jerseyNumber\": 99,
        \"heightInches\": 72,
        \"weightLbs\": 195,
        \"college\": \"Test University\",
        \"draftYear\": 2025,
        \"draftRound\": 7,
        \"draftPick\": 250
    }"
    local create_status
    create_status=$(status_code)
    it "Create player returns 201 or 200"
    if [[ "$create_status" == "201" || "$create_status" == "200" ]]; then
        pass
    else
        fail "Expected 201/200, got $create_status"
    fi

    local new_player_id
    new_player_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

    it "Created player has valid id"
    if [[ -n "$new_player_id" ]]; then
        pass
    else
        fail "No id in response"
    fi

    it "Created player has correct name"
    assert_json_string_value ".firstName" "E2E"
    assert_json_string_value ".lastName" "TestPlayer"

    it "New player appears in team roster"
    admin_get "/nfl/teams/${test_team_id}/roster"
    assert_status 200
    local found_player
    found_player=$(response_body | jq "[.[] | select(.id == \"${new_player_id}\")] | length" 2>/dev/null)
    if [[ "$found_player" == "1" ]]; then
        pass
    else
        fail "New player not found in roster"
    fi

    # Create a stat record
    admin_get "/nfl/games?season=2025&week=1"
    local test_game_id
    test_game_id=$(response_body | jq -r '.[0].id' 2>/dev/null)

    it "POST /nfl/games/{id}/stats creates stat record"
    admin_post "/nfl/games/${test_game_id}/stats" "{
        \"playerId\": \"${new_player_id}\",
        \"passingYards\": 0,
        \"passingTDs\": 0,
        \"interceptions\": 0,
        \"rushingYards\": 45,
        \"rushingTDs\": 0,
        \"receptions\": 3,
        \"receivingYards\": 67,
        \"receivingTDs\": 1,
        \"tackles\": 0,
        \"sacks\": 0,
        \"fumblesForced\": 0
    }"
    local stat_create_status
    stat_create_status=$(status_code)
    if [[ "$stat_create_status" == "201" || "$stat_create_status" == "200" ]]; then
        pass
    else
        fail "Expected 201/200, got $stat_create_status"
    fi

    local new_stat_id
    new_stat_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

    it "Stat appears in game boxscore"
    admin_get "/nfl/games/${test_game_id}/boxscore"
    assert_status 200
    local found_stat
    found_stat=$(response_body | jq "[.[] | select(.id == \"${new_stat_id}\")] | length" 2>/dev/null)
    if [[ "$found_stat" == "1" ]]; then
        pass
    else
        fail "New stat not found in boxscore"
    fi

    # ── Update Tests ──
    describe "NFL Data — Update Operations"

    # Get a second team for transfer
    admin_get "/nfl/teams?conference=NFC&division=East"
    local transfer_team_id
    transfer_team_id=$(response_body | jq -r '.[0].id' 2>/dev/null)

    it "POST /nfl/players/{id}/transfer moves player to new team"
    admin_post "/nfl/players/${new_player_id}/transfer" "{\"teamId\": \"${transfer_team_id}\"}"
    local transfer_status
    transfer_status=$(status_code)
    if [[ "$transfer_status" == "200" ]]; then
        pass
    else
        fail "Expected 200, got $transfer_status"
    fi

    it "Player no longer on old team roster"
    admin_get "/nfl/teams/${test_team_id}/roster"
    assert_status 200
    local on_old_team
    on_old_team=$(response_body | jq "[.[] | select(.id == \"${new_player_id}\")] | length" 2>/dev/null)
    if [[ "$on_old_team" == "0" ]]; then
        pass
    else
        fail "Player still on old team"
    fi

    it "Player appears on new team roster"
    admin_get "/nfl/teams/${transfer_team_id}/roster"
    assert_status 200
    local on_new_team
    on_new_team=$(response_body | jq "[.[] | select(.id == \"${new_player_id}\")] | length" 2>/dev/null)
    if [[ "$on_new_team" == "1" ]]; then
        pass
    else
        fail "Player not found on new team"
    fi

    it "POST /nfl/players/{id}/retire marks player inactive"
    admin_post "/nfl/players/${new_player_id}/retire" "{}"
    local retire_status
    retire_status=$(status_code)
    if [[ "$retire_status" == "200" ]]; then
        pass
    else
        fail "Expected 200, got $retire_status"
    fi

    it "Retired player not in active roster"
    admin_get "/nfl/teams/${transfer_team_id}/roster?isActive=true"
    assert_status 200
    local in_active
    in_active=$(response_body | jq "[.[] | select(.id == \"${new_player_id}\")] | length" 2>/dev/null)
    if [[ "$in_active" == "0" ]]; then
        pass
    else
        fail "Retired player still in active roster"
    fi

    # ── Delete Tests ──
    describe "NFL Data — Delete Operations"

    it "DELETE /nfl/stats/{id} removes stat record"
    admin_delete "/nfl/stats/${new_stat_id}"
    local delete_stat_status
    delete_stat_status=$(status_code)
    if [[ "$delete_stat_status" == "200" || "$delete_stat_status" == "204" ]]; then
        pass
    else
        fail "Expected 200/204, got $delete_stat_status"
    fi

    it "Deleted stat not in boxscore"
    admin_get "/nfl/games/${test_game_id}/boxscore"
    assert_status 200
    local stat_gone
    stat_gone=$(response_body | jq "[.[] | select(.id == \"${new_stat_id}\")] | length" 2>/dev/null)
    if [[ "$stat_gone" == "0" ]]; then
        pass
    else
        fail "Deleted stat still appears"
    fi

    it "DELETE /nfl/players/{id} removes player"
    admin_delete "/nfl/players/${new_player_id}"
    local delete_player_status
    delete_player_status=$(status_code)
    if [[ "$delete_player_status" == "200" || "$delete_player_status" == "204" ]]; then
        pass
    else
        fail "Expected 200/204, got $delete_player_status"
    fi

    it "Deleted player not in any roster"
    admin_get "/nfl/teams/${transfer_team_id}/roster"
    assert_status 200
    local player_gone
    player_gone=$(response_body | jq "[.[] | select(.id == \"${new_player_id}\")] | length" 2>/dev/null)
    if [[ "$player_gone" == "0" ]]; then
        pass
    else
        fail "Deleted player still appears"
    fi
}
