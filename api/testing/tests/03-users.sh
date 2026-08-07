#!/usr/bin/env bash
# tests/03-users.sh — User endpoint tests
#
# Seed users:
#   admin    (BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB) Admin role, 21 perms
#   testuser (AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA) Operator role, 14 perms
#   afcuser  (CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC) Viewer role, 7 perms, AFC tenant
#   nfcuser  (DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD) Viewer role, 7 perms, NFC tenant
#
# Endpoint policies:
#   GET  /users, /users/{id}, /users/me, /users/{id}/roles → fdw:users:read
#   POST /users, PUT /users/{id}, POST /users/{id}/roles, DELETE /users/{id}/roles/{name} → fdw:users:write
#   DELETE /users/{id} → fdw:users:delete

test_users() {
    describe "Users — Auth Matrix"

    it "GET /users without auth returns 401"
    anon_get "/users"
    assert_status 401

    it "GET /users as viewer returns 200 (has users:read)"
    viewer_get "/users"
    assert_status 200

    it "GET /users as operator returns 200 (has users:read)"
    operator_get "/users"
    assert_status 200

    it "GET /users as admin returns 200"
    admin_get "/users"
    assert_status 200

    it "POST /users as viewer returns 403 (no users:write)"
    viewer_post "/users" '{"username":"x","password":"Temp1234#","email":"x@x.com"}'
    assert_status 403

    it "POST /users as operator returns 201 (has users:write)"
    local ts
    ts=$(date +%s)
    operator_post "/users" "{\"username\":\"opprobe$ts\",\"password\":\"Probe1234#\",\"email\":\"opprobe$ts@test.com\",\"roles\":[\"Viewer\"]}"
    assert_status 201
    # cleanup: grab the id and delete with admin
    local probe_id
    probe_id=$(response_body | jq -r '.userId' 2>/dev/null)
    if [[ -n "$probe_id" && "$probe_id" != "null" ]]; then
        cleanup_entity "DELETE" "/users/$probe_id" "$ADMIN_TOKEN"
    fi

    it "DELETE /users as operator returns 403 (no users:delete)"
    operator_delete "/users/AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"
    assert_status 403

    # ── /users/me (admin) ──
    describe "Users — /users/me"

    it "GET /users/me as admin returns 200"
    admin_get "/users/me"
    assert_status 200

    it "/users/me id is admin GUID"
    assert_json_string_value ".id" "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

    it "/users/me username is admin"
    assert_json_string_value ".username" "admin"

    it "/users/me has email field"
    assert_json_string ".email"

    it "/users/me has roles array"
    assert_json_array ".roles"

    it "admin has Admin role"
    assert_json_array_contains ".roles" "Admin"

    it "/users/me has permissions array"
    assert_json_array ".permissions"

    it "admin has 21 permissions"
    assert_json_array_length ".permissions" 21

    # ── /users/me (operator = testuser) ──
    it "GET /users/me as operator returns 200"
    operator_get "/users/me"
    assert_status 200

    it "operator username is testuser"
    assert_json_string_value ".username" "testuser"

    it "operator has Operator role"
    assert_json_array_contains ".roles" "Operator"

    it "operator has 14 permissions"
    assert_json_array_length ".permissions" 14

    # ── /users/me (viewer = afcuser) ──
    it "GET /users/me as viewer returns 200"
    viewer_get "/users/me"
    assert_status 200

    it "viewer username is afcuser"
    assert_json_string_value ".username" "afcuser"

    it "viewer has Viewer role"
    assert_json_array_contains ".roles" "Viewer"

    it "viewer has 7 permissions"
    assert_json_array_length ".permissions" 7

    it "viewer /users/me has availableTenants array"
    assert_json_array ".availableTenants"

    # ── List Users ──
    describe "Users — List"

    it "GET /users as admin returns 200"
    admin_get "/users"
    assert_status 200

    it "users list is an array"
    assert_json_array "."

    it "users list has at least 4 seed users"
    assert_json_array_min_length "." 4

    it "first user has id (GUID)"
    assert_json_guid ".[0].id"

    it "first user has username (string)"
    assert_json_string ".[0].username"

    it "first user has name (string)"
    assert_json_string ".[0].name"

    it "first user has email (string)"
    assert_json_string ".[0].email"

    it "first user has isActive (bool)"
    assert_json_bool ".[0].isActive"

    it "first user has roles (array)"
    assert_json_array ".[0].roles"

    it "first user has createdAt (datetime)"
    assert_json_datetime ".[0].createdAt"

    # ── Get User by Known ID ──
    describe "Users — Get by ID"

    it "GET /users/BBBBBBBB-... returns admin"
    admin_get "/users/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    assert_status 200

    it "admin user detail has username admin"
    assert_json_string_value ".username" "admin"

    it "GET /users/AAAAAAAA-... returns testuser"
    admin_get "/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    assert_status 200

    it "testuser detail has username testuser"
    assert_json_string_value ".username" "testuser"

    it "GET /users/00000000-... returns 404"
    admin_get "/users/00000000-0000-0000-0000-000000000000"
    assert_status 404

    # ── CRUD Lifecycle ──
    describe "Users — CRUD Lifecycle"

    local test_user="e2e-user-$(date +%s)"
    local created_id=""

    it "POST /users creates new user (201)"
    admin_post "/users" "{
        \"username\": \"$test_user\",
        \"password\": \"E2ETestPass1#\",
        \"email\": \"${test_user}@test.com\",
        \"roles\": [\"Operator\"]
    }"
    assert_status 201

    it "create response has success=true"
    assert_json_bool ".success" "true"

    it "create response has userId (GUID)"
    assert_json_guid ".userId"

    it "create response has message"
    assert_json_string ".message"

    created_id=$(response_body | jq -r '.userId' 2>/dev/null)

    if [[ -n "$created_id" && "$created_id" != "null" ]]; then
        it "GET /users/{id} returns created user"
        admin_get "/users/$created_id"
        assert_status 200

        it "created user has correct username"
        assert_json_string_value ".username" "$test_user"

        it "PUT /users/{id} updates user (204)"
        admin_put "/users/$created_id" "{
            \"userId\": \"$created_id\",
            \"email\": \"updated-${test_user}@test.com\"
        }"
        assert_status 204

        # ── Role Assignments ──
        it "GET /users/{id}/roles returns roles array"
        admin_get "/users/$created_id/roles"
        assert_status 200

        it "roles response is array"
        assert_json_array "."

        it "POST /users/{id}/roles assigns Viewer role"
        admin_post "/users/$created_id/roles" '{"roleName":"Viewer"}'
        assert_status 200

        it "DELETE /users/{id}/roles/Viewer revokes role"
        admin_delete "/users/$created_id/roles/Viewer"
        assert_status 200

        # ── Delete ──
        it "DELETE /users/{id} removes user (204)"
        admin_delete "/users/$created_id"
        assert_status 204

        it "GET deleted user returns 404"
        admin_get "/users/$created_id"
        assert_status 404
    else
        skip "user CRUD skipped — create did not return userId"
    fi
}
