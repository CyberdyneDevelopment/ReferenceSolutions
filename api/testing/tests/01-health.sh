#!/usr/bin/env bash
# tests/01-health.sh — Health endpoint tests (anonymous)

test_health() {
    describe "Health"

    # ── Anonymous GET /health ──
    it "GET /health returns 200 (anonymous)"
    anon_get "/health"
    assert_status 200

    it "status is Healthy"
    assert_json_string_value ".status" "Healthy"

    it "application is Reference.Api"
    assert_json_string_value ".application" "Reference.Api"

    it "timestamp is ISO 8601 datetime"
    assert_json_datetime ".timestamp"

    it "checks is an array"
    assert_json_array ".checks"

    it "checks array is non-empty"
    assert_json_array_min_length ".checks" 1

    it "checks contains ControlDb entry"
    local has_config_db
    has_config_db=$(response_body | jq '[.checks[] | select(.name == "ControlDb")] | length' 2>/dev/null)
    if [[ "$has_config_db" -ge 1 ]]; then
        pass
    else
        fail "expected checks to contain ControlDb"
    fi

    it "ControlDb check has status field"
    local db_status
    db_status=$(response_body | jq -r '.checks[] | select(.name == "ControlDb") | .status' 2>/dev/null)
    if [[ -n "$db_status" && "$db_status" != "null" ]]; then
        pass
    else
        fail "expected ControlDb check to have status field"
    fi
}
