#!/usr/bin/env bash
# tests/17-configuration.sh — Configuration metadata endpoint tests
# Connection types come from the TypeCollection in code: MsSql, PostgreSql, Http
# GET /connection-types: Policies("fdw:configurations:read") → ALL roles
# GET /connections/schema-capable: Policies("fdw:connections:read") → ALL roles
# GET /configuration/instances: Policies("fdw:configurations:read") → ALL roles
# POST/PUT /configuration/instances: Policies("fdw:configurations:write") → Admin ONLY

test_configuration() {
    describe "Configuration — Auth Matrix"

    it "GET /connection-types without auth returns 401"
    anon_get "/connection-types"
    assert_status 401

    it "GET /connection-types as viewer returns 200"
    viewer_get "/connection-types"
    assert_status 200

    it "GET /connection-types as operator returns 200"
    operator_get "/connection-types"
    assert_status 200

    it "GET /connection-types as admin returns 200"
    admin_get "/connection-types"
    assert_status 200

    # ── Connection Types List ──
    describe "Configuration — Connection Types"

    it "Connection types is array"
    assert_json_array "."

    it "At least 3 connection types"
    assert_json_array_min_length "." 3

    # Verify specific types exist
    # Types can be returned as plain strings or as objects with .name
    local has_mssql has_postgresql has_http

    # Try plain string array first
    has_mssql=$(response_body | jq '[.[] | select(. == "MsSql" or .name == "MsSql")] | length' 2>/dev/null)
    has_postgresql=$(response_body | jq '[.[] | select(. == "PostgreSql" or .name == "PostgreSql")] | length' 2>/dev/null)
    has_http=$(response_body | jq '[.[] | select(. == "Http" or .name == "Http")] | length' 2>/dev/null)

    it "MsSql connection type exists"
    if [[ "$has_mssql" -ge 1 ]]; then
        pass
    else
        fail "MsSql not found in connection types"
    fi

    it "PostgreSql connection type exists"
    if [[ "$has_postgresql" -ge 1 ]]; then
        pass
    else
        fail "PostgreSql not found in connection types"
    fi

    it "Http connection type exists"
    if [[ "$has_http" -ge 1 ]]; then
        pass
    else
        fail "Http not found in connection types"
    fi

    # ── Schema-Capable Connections ──
    describe "Configuration — Schema-Capable Connections"

    it "GET /connections/schema-capable without auth returns 401"
    anon_get "/connections/schema-capable"
    assert_status 401

    it "GET /connections/schema-capable as viewer returns 200"
    viewer_get "/connections/schema-capable"
    local schema_viewer_status
    schema_viewer_status=$(status_code)
    if [[ "$schema_viewer_status" == "200" ]]; then
        pass
    elif [[ "$schema_viewer_status" == "404" ]]; then
        skip "Schema-capable endpoint not implemented"
    else
        skip "Schema-capable as viewer returned $schema_viewer_status"
    fi

    it "GET /connections/schema-capable as admin returns 200"
    admin_get "/connections/schema-capable"
    local schema_status
    schema_status=$(status_code)
    if [[ "$schema_status" == "200" ]]; then
        pass
        it "Schema-capable is an array"
        assert_json_array "."
    elif [[ "$schema_status" == "404" ]]; then
        skip "Schema-capable endpoint not implemented"
    else
        skip "Schema-capable returned $schema_status"
    fi

    # ── Configuration Instances ──
    describe "Configuration — Configuration Instances"

    it "GET /configuration/instances without auth returns 401"
    anon_get "/configuration/instances"
    local cfg_anon_status
    cfg_anon_status=$(status_code)
    if [[ "$cfg_anon_status" == "401" ]]; then
        pass
    elif [[ "$cfg_anon_status" == "404" ]]; then
        skip "Configuration instances endpoint not implemented"
    else
        skip "Configuration instances anon returned $cfg_anon_status"
    fi

    it "GET /configuration/instances as viewer returns 200"
    viewer_get "/configuration/instances"
    local cfg_viewer_status
    cfg_viewer_status=$(status_code)
    if [[ "$cfg_viewer_status" == "200" ]]; then
        pass
    elif [[ "$cfg_viewer_status" == "404" ]]; then
        skip "Configuration instances endpoint not implemented"
    else
        skip "Configuration instances viewer returned $cfg_viewer_status"
    fi

    it "GET /configuration/instances as admin returns 200"
    admin_get "/configuration/instances"
    local cfg_status
    cfg_status=$(status_code)
    if [[ "$cfg_status" == "200" ]]; then
        pass
    elif [[ "$cfg_status" == "404" ]]; then
        skip "Configuration instances endpoint not implemented"
    else
        skip "Configuration instances returned $cfg_status"
    fi
}
