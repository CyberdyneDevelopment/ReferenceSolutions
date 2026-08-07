#!/usr/bin/env bash
# tests/07-connections.sh — Connection endpoint tests
#
# Seed connections (8):
#   ControlDb (MsSql), AuthDb (MsSql), TenantDb (MsSql),
#   EtlDb (MsSql), SchedDb (MsSql), OpsDb (MsSql),
#   DemoPostgreSql (PostgreSql), DemoHttpApi (Http)
#
# Known IDs:
#   ControlDb: C3D4E5F6-A7B8-9012-CDEF-234567890ABC
#
# Endpoint policies:
#   GET  /connections, /connections/{name} → fdw:connections:read
#   POST /connections, PUT /connections/{name}, POST /connections/{name}/test → fdw:connections:write
#   DELETE /connections/{name} → fdw:connections:delete

test_connections() {
    describe "Connections — Auth Matrix"

    it "GET /connections without auth returns 401"
    anon_get "/connections"
    assert_status 401

    it "GET /connections as viewer returns 200 (has connections:read)"
    viewer_get "/connections"
    assert_status 200

    it "GET /connections as operator returns 200"
    operator_get "/connections"
    assert_status 200

    it "POST /connections as viewer returns 403 (no connections:write)"
    viewer_post "/connections" '{"name":"x","server":"x","port":1433,"database":"x"}'
    assert_status 403

    it "DELETE /connections/ControlDb as operator returns 403 (no connections:delete)"
    operator_delete "/connections/ControlDb"
    assert_status 403

    # ── List Connections ──
    describe "Connections — List"

    it "GET /connections as admin returns 200"
    admin_get "/connections"
    assert_status 200

    it "connections is array"
    assert_json_array "."

    it "connections has exactly 8 seed entries"
    assert_json_array_length "." 8

    it "connection summary has name (string)"
    assert_json_string ".[0].name"

    it "connection summary has connectionType (string)"
    assert_json_string ".[0].connectionType"

    # Verify known connections exist
    it "connections contains ControlDb"
    local has_cfg
    has_cfg=$(response_body | jq '[.[] | select(.name == "ControlDb")] | length' 2>/dev/null)
    if [[ "$has_cfg" -ge 1 ]]; then
        pass
    else
        fail "expected connections to contain ControlDb"
    fi

    it "connections contains AuthDb"
    local has_auth
    has_auth=$(response_body | jq '[.[] | select(.name == "AuthDb")] | length' 2>/dev/null)
    if [[ "$has_auth" -ge 1 ]]; then
        pass
    else
        fail "expected connections to contain AuthDb"
    fi

    it "connections contains DemoPostgreSql"
    local has_pg
    has_pg=$(response_body | jq '[.[] | select(.name == "DemoPostgreSql")] | length' 2>/dev/null)
    if [[ "$has_pg" -ge 1 ]]; then
        pass
    else
        fail "expected connections to contain DemoPostgreSql"
    fi

    it "connections contains DemoHttpApi"
    local has_http
    has_http=$(response_body | jq '[.[] | select(.name == "DemoHttpApi")] | length' 2>/dev/null)
    if [[ "$has_http" -ge 1 ]]; then
        pass
    else
        fail "expected connections to contain DemoHttpApi"
    fi

    # ── Get Connection Detail: ControlDb ──
    describe "Connections — ControlDb Detail"

    it "GET /connections/ControlDb returns 200"
    admin_get "/connections/ControlDb"
    assert_status 200

    it "ControlDb has id (GUID)"
    assert_json_guid ".id"

    it "ControlDb id matches seed value"
    assert_json_string_value ".id" "c3d4e5f6-a7b8-9012-cdef-234567890abc"

    it "ControlDb name is ControlDb"
    assert_json_string_value ".name" "ControlDb"

    it "ControlDb serviceType is MsSql"
    assert_json_string_value ".serviceType" "MsSql"

    it "ControlDb server is localhost"
    assert_json_string_value ".server" "localhost"

    it "ControlDb has port (number)"
    assert_json_number ".port"

    it "ControlDb database is ControlDb"
    assert_json_string_value ".database" "ControlDb"

    it "ControlDb has authenticationType (string)"
    assert_json_string ".authenticationType"

    it "ControlDb has authentication (object)"
    assert_json_object ".authentication"

    it "ControlDb has isActive (bool)"
    assert_json_bool ".isActive"

    it "ControlDb has createdAt (datetime)"
    assert_json_datetime ".createdAt"

    it "ControlDb has updatedAt (datetime)"
    assert_json_datetime ".updatedAt"

    # ── 404 ──
    it "GET /connections/NonExistent returns 404"
    admin_get "/connections/NonExistent"
    assert_status 404

    # ── Test Connection ──
    describe "Connections — Test"

    it "POST /connections/ControlDb/test returns result"
    admin_post "/connections/ControlDb/test" ""
    local test_status
    test_status=$(status_code)
    if [[ "$test_status" == "200" ]]; then
        pass
    else
        skip "connection test returned $test_status (DB may not be available)"
    fi

    if [[ "$test_status" == "200" ]]; then
        it "test result has name"
        assert_json_string_value ".name" "ControlDb"

        it "test result has success (bool)"
        assert_json_bool ".success"

        it "test result has message (string)"
        assert_json_string ".message"
    fi

    # ── CRUD Lifecycle ──
    describe "Connections — CRUD Lifecycle"

    local test_conn="e2e-conn-$(date +%s)"

    it "POST /connections creates new connection (201)"
    admin_post "/connections" "{
        \"name\": \"$test_conn\",
        \"server\": \"localhost\",
        \"port\": 1433,
        \"database\": \"TestDb\",
        \"authenticationType\": \"SqlAuth\",
        \"authentication\": {\"username\":\"sa\",\"password\":\"test\"},
        \"trustServerCertificate\": true,
        \"encrypt\": false
    }"
    assert_status 201

    it "created connection has correct name"
    assert_json_string_value ".name" "$test_conn"

    it "GET /connections/{name} reads back created connection"
    admin_get "/connections/$test_conn"
    assert_status 200

    it "read back has correct name"
    assert_json_string_value ".name" "$test_conn"

    it "read back has correct server"
    assert_json_string_value ".server" "localhost"

    it "read back has correct database"
    assert_json_string_value ".database" "TestDb"

    it "PUT /connections/{name} updates connection (204)"
    admin_put "/connections/$test_conn" "{
        \"name\": \"$test_conn\",
        \"server\": \"updated-host\",
        \"port\": 5432,
        \"database\": \"UpdatedDb\",
        \"authenticationType\": \"SqlAuth\",
        \"authentication\": {\"username\":\"sa\",\"password\":\"updated\"},
        \"trustServerCertificate\": true,
        \"encrypt\": false
    }"
    assert_status 204

    it "DELETE /connections/{name} removes connection (204)"
    admin_delete "/connections/$test_conn"
    assert_status 204

    it "GET deleted connection returns 404"
    admin_get "/connections/$test_conn"
    assert_status 404
}
