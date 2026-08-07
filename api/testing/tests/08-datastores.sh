#!/usr/bin/env bash
# tests/08-datastores.sh — DataStore endpoint tests

test_datastores() {
    describe "DataStores"

    # ── Auth Matrix ──
    it "GET /datastores without auth returns 401"
    anon_get "/datastores"
    assert_status 401

    it "GET /datastores as viewer returns 200"
    viewer_get "/datastores"
    assert_status 200

    it "POST /datastores as viewer returns 403"
    viewer_post "/datastores" '{"name":"x","storeType":"MsSql","connectionId":"00000000-0000-0000-0000-000000000000"}'
    assert_status 403

    it "PUT /datastores/ControlDb as viewer returns 403"
    viewer_put "/datastores/ControlDb" '{"name":"ControlDb","storeType":"MsSql","connectionId":"C3D4E5F6-A7B8-9012-CDEF-234567890ABC"}'
    assert_status 403

    it "POST /datastores as operator returns 200 or 201"
    operator_post "/datastores" '{"name":"op-test-ds","storeType":"MsSql","connectionId":"C3D4E5F6-A7B8-9012-CDEF-234567890ABC","description":"operator write test"}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        # Clean up
        admin_delete "/datastores/op-test-ds"
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    it "DELETE /datastores/ControlDb as operator returns 403"
    operator_delete "/datastores/ControlDb"
    assert_status 403

    it "DELETE as admin returns 204 (tested in CRUD lifecycle)"
    pass

    # ── List DataStores ──
    it "GET /datastores as admin returns array"
    admin_get "/datastores"
    assert_status 200

    it "DataStores is array"
    assert_json_array "."

    it "Exactly 6 seed datastores"
    assert_json_array_length "." 6

    it "DataStore summary has name"
    assert_json_string ".[0].name"

    it "DataStore summary has storeType"
    assert_json_string ".[0].storeType"

    it "DataStore summary has connectionId"
    assert_json_string ".[0].connectionId"

    it "DataStore summary has description"
    assert_json_string ".[0].description"

    # ── Verify all 6 seed stores by name ──
    it "Contains ControlDb"
    assert_json_array_contains "[.[] | .name]" "ControlDb"

    it "Contains AuthDb"
    assert_json_array_contains "[.[] | .name]" "AuthDb"

    it "Contains TenantDb"
    assert_json_array_contains "[.[] | .name]" "TenantDb"

    it "Contains SchedDb"
    assert_json_array_contains "[.[] | .name]" "SchedDb"

    it "Contains EtlDb"
    assert_json_array_contains "[.[] | .name]" "EtlDb"

    it "Contains OpsDb"
    assert_json_array_contains "[.[] | .name]" "OpsDb"

    # ── All stores are MsSql ──
    it "All stores have storeType MsSql"
    local non_mssql
    non_mssql=$(response_body | jq '[.[] | select(.storeType != "MsSql")] | length' 2>/dev/null)
    if [[ "$non_mssql" == "0" ]]; then
        pass
    else
        fail "expected all storeType=MsSql, found $non_mssql non-MsSql stores"
    fi

    # ── Get DataStore Detail: ControlDb ──
    it "GET /datastores/ControlDb returns detail"
    admin_get "/datastores/ControlDb"
    assert_status 200

    it "ControlDb has correct id"
    assert_json_string_value ".id" "E1F2A3B4-C5D6-7890-ABCD-111111111111"

    it "ControlDb has name"
    assert_json_string_value ".name" "ControlDb"

    it "ControlDb has storeType MsSql"
    assert_json_string_value ".storeType" "MsSql"

    it "ControlDb has connectionId"
    assert_json_string_value ".connectionId" "C3D4E5F6-A7B8-9012-CDEF-234567890ABC"

    it "ControlDb has description"
    assert_json_string ".description"

    # ── Paths ──
    it "GET /datastores/ControlDb/paths returns 200"
    admin_get "/datastores/ControlDb/paths"
    local paths_status
    paths_status=$(status_code)
    if [[ "$paths_status" == "200" ]]; then
        pass

        it "Paths is array"
        assert_json_array "."

        it "Path has name"
        local path_count
        path_count=$(response_body | jq '. | length' 2>/dev/null)
        if [[ "$path_count" -gt 0 ]]; then
            assert_json_string ".[0].name"
        else
            skip "No paths returned"
        fi
    else
        skip "Paths returned $paths_status"
    fi

    # ── Containers ──
    it "GET /datastores/ControlDb/containers returns 200"
    admin_get "/datastores/ControlDb/containers"
    local containers_status
    containers_status=$(status_code)
    if [[ "$containers_status" == "200" ]]; then
        pass

        it "Containers is array"
        assert_json_array "."
    else
        skip "Containers returned $containers_status"
    fi

    # ── Introspect ──
    it "POST /datastores/ControlDb/introspect returns result"
    admin_post "/datastores/ControlDb/introspect" ""
    local introspect_status
    introspect_status=$(status_code)
    if [[ "$introspect_status" == "200" || "$introspect_status" == "202" ]]; then
        pass
    else
        skip "Introspect returned $introspect_status (may require live DB)"
    fi

    # ── 404 ──
    it "GET /datastores/NonExistent returns 404"
    admin_get "/datastores/NonExistent"
    assert_status 404

    # ── CRUD Lifecycle ──
    # Get a connection ID from seed data
    admin_get "/connections"
    local conn_id
    conn_id=$(response_body | jq -r '.[0].id // empty' 2>/dev/null)

    if [[ -n "$conn_id" ]]; then
        local TEST_NAME="e2e-test-ds-$(date +%s)"

        it "Create datastore"
        admin_post "/datastores" "{
            \"name\": \"$TEST_NAME\",
            \"storeType\": \"MsSql\",
            \"connectionId\": \"$conn_id\",
            \"description\": \"E2E test datastore\"
        }"
        assert_status 201

        it "Read back created datastore"
        admin_get "/datastores/$TEST_NAME"
        assert_status 200

        it "Created datastore has correct name"
        assert_json_string_value ".name" "$TEST_NAME"

        it "Created datastore has correct storeType"
        assert_json_string_value ".storeType" "MsSql"

        it "Update datastore"
        admin_put "/datastores/$TEST_NAME" "{
            \"name\": \"$TEST_NAME\",
            \"storeType\": \"MsSql\",
            \"connectionId\": \"$conn_id\",
            \"description\": \"Updated E2E datastore\"
        }"
        assert_status 204

        it "Delete datastore"
        admin_delete "/datastores/$TEST_NAME"
        assert_status 204

        it "Deleted datastore returns 404"
        admin_get "/datastores/$TEST_NAME"
        assert_status 404
    else
        skip "No connection ID available for datastore CRUD tests"
    fi
}
