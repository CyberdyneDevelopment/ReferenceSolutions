#!/usr/bin/env bash
# tests/04-roles.sh — Role endpoint tests
#
# Seed roles:
#   Viewer   — 7 read permissions, isSystem=true, sortOrder=1
#   Operator — 14 permissions (7 read + 7 write), isSystem=true, sortOrder=2
#   Admin    — 21 permissions (all), displayName="Administrator", isSystem=true, sortOrder=3
#
# Endpoint policies:
#   GET  /roles, /roles/{name} → fdw:users:read
#   POST /roles, PUT /roles/{name} → fdw:users:write
#   DELETE /roles/{name} → fdw:users:delete

test_roles() {
    describe "Roles — Auth Matrix"

    it "GET /roles without auth returns 401"
    anon_get "/roles"
    assert_status 401

    it "GET /roles as viewer returns 200 (has users:read)"
    viewer_get "/roles"
    assert_status 200

    it "GET /roles as operator returns 200"
    operator_get "/roles"
    assert_status 200

    it "POST /roles as viewer returns 403 (no users:write)"
    viewer_post "/roles" '{"name":"x","displayName":"x"}'
    assert_status 403

    it "POST /roles as operator returns 201 (has users:write)"
    local ts
    ts=$(date +%s)
    operator_post "/roles" "{\"name\":\"op-probe-$ts\",\"displayName\":\"Probe Role\"}"
    assert_status 201
    # cleanup
    cleanup_entity "DELETE" "/roles/op-probe-$ts" "$ADMIN_TOKEN"

    it "DELETE /roles as operator returns 403 (no users:delete)"
    operator_delete "/roles/Viewer"
    assert_status 403

    # ── List Roles ──
    describe "Roles — List"

    it "GET /roles as admin returns 200"
    admin_get "/roles"
    assert_status 200

    it "roles list is array"
    assert_json_array "."

    it "roles list has at least 3 seed roles"
    assert_json_array_min_length "." 3

    it "role item has name (string)"
    assert_json_string ".[0].name"

    it "role item has displayName (string)"
    assert_json_string ".[0].displayName"

    it "role item has isSystem (bool)"
    assert_json_bool ".[0].isSystem"

    it "role item has sortOrder (number)"
    assert_json_number ".[0].sortOrder"

    # Verify exact seed role names exist
    it "roles list contains Viewer"
    assert_json_array_contains "[.[].name]" "Viewer"

    it "roles list contains Operator"
    assert_json_array_contains "[.[].name]" "Operator"

    it "roles list contains Admin"
    assert_json_array_contains "[.[].name]" "Admin"

    # ── Get Role Detail: Admin ──
    describe "Roles — Admin Detail"

    it "GET /roles/Admin returns 200"
    admin_get "/roles/Admin"
    assert_status 200

    it "Admin role name is Admin"
    assert_json_string_value ".name" "Admin"

    it "Admin role displayName is Administrator"
    assert_json_string_value ".displayName" "Administrator"

    it "Admin role isSystem is true"
    assert_json_bool ".isSystem" "true"

    it "Admin role sortOrder is 3"
    assert_json_number_value ".sortOrder" "3"

    it "Admin role has permissions array"
    assert_json_array ".permissions"

    it "Admin role has exactly 21 permissions"
    assert_json_array_length ".permissions" 21

    # ── Get Role Detail: Operator ──
    describe "Roles — Operator Detail"

    it "GET /roles/Operator returns 200"
    admin_get "/roles/Operator"
    assert_status 200

    it "Operator role name is Operator"
    assert_json_string_value ".name" "Operator"

    it "Operator role isSystem is true"
    assert_json_bool ".isSystem" "true"

    it "Operator role sortOrder is 2"
    assert_json_number_value ".sortOrder" "2"

    it "Operator role has exactly 14 permissions"
    assert_json_array_length ".permissions" 14

    # ── Get Role Detail: Viewer ──
    describe "Roles — Viewer Detail"

    it "GET /roles/Viewer returns 200"
    admin_get "/roles/Viewer"
    assert_status 200

    it "Viewer role name is Viewer"
    assert_json_string_value ".name" "Viewer"

    it "Viewer role isSystem is true"
    assert_json_bool ".isSystem" "true"

    it "Viewer role sortOrder is 1"
    assert_json_number_value ".sortOrder" "1"

    it "Viewer role has exactly 7 permissions"
    assert_json_array_length ".permissions" 7

    # ── 404 ──
    it "GET /roles/NonExistent returns 404"
    admin_get "/roles/NonExistent"
    assert_status 404

    # ── CRUD Lifecycle ──
    describe "Roles — CRUD Lifecycle"

    local test_role="e2e-role-$(date +%s)"

    it "POST /roles creates new role (201)"
    admin_post "/roles" "{
        \"name\": \"$test_role\",
        \"displayName\": \"E2E Test Role\",
        \"description\": \"Created by E2E tests\"
    }"
    assert_status 201

    it "GET /roles/{name} reads back created role"
    admin_get "/roles/$test_role"
    assert_status 200

    it "created role has correct name"
    assert_json_string_value ".name" "$test_role"

    it "created role has correct displayName"
    assert_json_string_value ".displayName" "E2E Test Role"

    it "PUT /roles/{name} updates role (204)"
    admin_put "/roles/$test_role" "{
        \"name\": \"$test_role\",
        \"displayName\": \"Updated E2E Role\",
        \"description\": \"Updated by E2E tests\"
    }"
    assert_status 204

    it "DELETE /roles/{name} removes role (204)"
    admin_delete "/roles/$test_role"
    assert_status 204

    it "GET deleted role returns 404"
    admin_get "/roles/$test_role"
    assert_status 404
}
