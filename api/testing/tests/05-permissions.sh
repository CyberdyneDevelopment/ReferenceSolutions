#!/usr/bin/env bash
# tests/05-permissions.sh — Permission endpoint tests
#
# 21 seed permissions:
#   connections:read/write/delete, datastores:read/write/delete,
#   datasets:read/write/delete, pipelines:read/write/execute/delete,
#   schedules:read/write/delete, users:read/write/delete,
#   configurations:read/write
#
# Endpoint policies:
#   GET  /permissions, /permissions/grouped → fdw:users:read
#   PUT  /roles/{name}/permissions → fdw:users:write

test_permissions() {
    describe "Permissions — Auth Matrix"

    it "GET /permissions without auth returns 401"
    anon_get "/permissions"
    assert_status 401

    it "GET /permissions as viewer returns 200 (has users:read)"
    viewer_get "/permissions"
    assert_status 200

    it "GET /permissions as operator returns 200"
    operator_get "/permissions"
    assert_status 200

    it "GET /permissions as admin returns 200"
    admin_get "/permissions"
    assert_status 200

    # ── List Permissions ──
    describe "Permissions — List"

    it "GET /permissions returns 200"
    admin_get "/permissions"
    assert_status 200

    it "permissions is array"
    assert_json_array "."

    it "permissions has exactly 21 entries"
    assert_json_array_length "." 21

    # Verify first permission shape
    it "permission item has name (string)"
    assert_json_string ".[0].name"

    it "permission item has displayName (string)"
    assert_json_string ".[0].displayName"

    it "permission item has description (string)"
    assert_json_string ".[0].description"

    it "permission item has resource (string)"
    assert_json_string ".[0].resource"

    it "permission item has action (string)"
    assert_json_string ".[0].action"

    it "permission item has category (string)"
    assert_json_string ".[0].category"

    # Verify known permissions exist
    it "permissions contains connections:read"
    local has_conn_read
    has_conn_read=$(response_body | jq '[.[] | select(.name == "connections:read")] | length' 2>/dev/null)
    if [[ "$has_conn_read" -ge 1 ]]; then
        pass
    else
        fail "expected permissions to contain connections:read"
    fi

    it "permissions contains pipelines:execute"
    local has_pipe_exec
    has_pipe_exec=$(response_body | jq '[.[] | select(.name == "pipelines:execute")] | length' 2>/dev/null)
    if [[ "$has_pipe_exec" -ge 1 ]]; then
        pass
    else
        fail "expected permissions to contain pipelines:execute"
    fi

    it "permissions contains configurations:write"
    local has_cfg_write
    has_cfg_write=$(response_body | jq '[.[] | select(.name == "configurations:write")] | length' 2>/dev/null)
    if [[ "$has_cfg_write" -ge 1 ]]; then
        pass
    else
        fail "expected permissions to contain configurations:write"
    fi

    # ── Grouped Permissions ──
    describe "Permissions — Grouped"

    it "GET /permissions/grouped returns 200"
    admin_get "/permissions/grouped"
    assert_status 200

    it "grouped permissions is array"
    assert_json_array "."

    it "grouped permissions has at least 7 resource groups"
    assert_json_array_min_length "." 7

    it "grouped item has resource (string)"
    assert_json_string ".[0].resource"

    it "grouped item has permissions (array)"
    assert_json_array ".[0].permissions"

    # ── Set Role Permissions ──
    describe "Permissions — Role Permission Assignment"

    # Create a temporary role for permission testing
    local test_role="e2e-perm-role-$(date +%s)"
    admin_post "/roles" "{
        \"name\": \"$test_role\",
        \"displayName\": \"Perm Test Role\"
    }" > /dev/null 2>&1

    it "PUT /roles/{name}/permissions as viewer returns 403 (no users:write)"
    viewer_put "/roles/$test_role/permissions" '{"permissions":["connections:read"]}'
    assert_status 403

    it "PUT /roles/{name}/permissions as operator returns 200 (has users:write)"
    operator_put "/roles/$test_role/permissions" '{"permissions":["connections:read","datastores:read"]}'
    assert_status 200

    it "PUT /roles/{name}/permissions as admin returns 200"
    admin_put "/roles/$test_role/permissions" '{"permissions":["connections:read","datastores:read","datasets:read"]}'
    assert_status 200

    # Cleanup
    cleanup_entity "DELETE" "/roles/$test_role" "$ADMIN_TOKEN"
}
