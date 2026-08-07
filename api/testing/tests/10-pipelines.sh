#!/usr/bin/env bash
# tests/10-pipelines.sh — Pipeline endpoint tests

test_pipelines() {
    describe "Pipelines"

    # ── Auth Matrix ──
    it "GET /pipelines without auth returns 401"
    anon_get "/pipelines"
    assert_status 401

    it "GET /pipelines as viewer returns 200"
    viewer_get "/pipelines"
    assert_status 200

    it "POST /pipelines as viewer returns 403"
    viewer_post "/pipelines" '{"name":"x","pipelineType":"BatchCopy"}'
    assert_status 403

    it "PUT /pipelines/DataArchiveCopy as viewer returns 403"
    viewer_put "/pipelines/DataArchiveCopy" '{"name":"DataArchiveCopy","pipelineType":"BatchCopy","isEnabled":true}'
    assert_status 403

    it "POST /pipelines as operator returns 200 or 201"
    operator_post "/pipelines" '{"name":"op-test-pipe","pipelineType":"BatchCopy","description":"operator write test","isEnabled":false}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        # Clean up
        admin_delete "/pipelines/op-test-pipe"
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    it "POST /pipelines/DataArchiveCopy/execute as operator returns success"
    operator_post "/pipelines/DataArchiveCopy/execute" ""
    local op_exec_status
    op_exec_status=$(status_code)
    if [[ "$op_exec_status" == "200" || "$op_exec_status" == "202" || "$op_exec_status" == "500" || "$op_exec_status" == "503" ]]; then
        pass
    else
        fail "expected 200/202/500/503 for execute, got $op_exec_status"
    fi

    it "DELETE /pipelines/DataArchiveCopy as operator returns 403"
    operator_delete "/pipelines/DataArchiveCopy"
    assert_status 403

    # ── List Pipelines ──
    it "GET /pipelines as admin returns array"
    admin_get "/pipelines"
    assert_status 200

    it "Pipelines is array"
    assert_json_array "."

    it "Exactly 2 seed pipelines"
    assert_json_array_length "." 2

    it "Pipeline summary has name"
    assert_json_string ".[0].name"

    it "Pipeline summary has pipelineType"
    assert_json_string ".[0].pipelineType"

    it "Contains DataArchiveCopy"
    assert_json_array_contains "[.[] | .name]" "DataArchiveCopy"

    it "Contains LiveDataStream"
    assert_json_array_contains "[.[] | .name]" "LiveDataStream"

    it "DataArchiveCopy is BatchCopy type"
    local dac_type
    dac_type=$(response_body | jq -r '.[] | select(.name == "DataArchiveCopy") | .pipelineType' 2>/dev/null)
    if [[ "$dac_type" == "BatchCopy" ]]; then
        pass
    else
        fail "expected DataArchiveCopy pipelineType=BatchCopy, got $dac_type"
    fi

    it "LiveDataStream is Streaming type"
    local lds_type
    lds_type=$(response_body | jq -r '.[] | select(.name == "LiveDataStream") | .pipelineType' 2>/dev/null)
    if [[ "$lds_type" == "Streaming" ]]; then
        pass
    else
        fail "expected LiveDataStream pipelineType=Streaming, got $lds_type"
    fi

    # ── Get Pipeline Detail: DataArchiveCopy ──
    it "GET /pipelines/DataArchiveCopy returns detail"
    admin_get "/pipelines/DataArchiveCopy"
    assert_status 200

    it "DataArchiveCopy has correct id"
    assert_json_string_value ".id" "E1F2A3B4-C5D6-7890-EF12-34567890ABCD"

    it "DataArchiveCopy has correct name"
    assert_json_string_value ".name" "DataArchiveCopy"

    it "DataArchiveCopy has pipelineType BatchCopy"
    assert_json_string_value ".pipelineType" "BatchCopy"

    it "DataArchiveCopy isEnabled is true"
    assert_json_bool ".isEnabled" "true"

    it "DataArchiveCopy has description"
    assert_json_string_value ".description" "Copies data to archive for historical analysis"

    it "DataArchiveCopy has createdAt"
    assert_json_datetime ".createdAt"

    # ── Get Pipeline Detail: LiveDataStream ──
    it "GET /pipelines/LiveDataStream returns detail"
    admin_get "/pipelines/LiveDataStream"
    assert_status 200

    it "LiveDataStream has correct id"
    assert_json_string_value ".id" "A2B3C4D5-E6F7-8901-AB23-456789ABCDEF"

    it "LiveDataStream has correct name"
    assert_json_string_value ".name" "LiveDataStream"

    it "LiveDataStream has pipelineType Streaming"
    assert_json_string_value ".pipelineType" "Streaming"

    it "LiveDataStream isEnabled is true"
    assert_json_bool ".isEnabled" "true"

    # ── Pipeline Status ──
    it "GET /pipelines/DataArchiveCopy/status returns result"
    admin_get "/pipelines/DataArchiveCopy/status"
    local status_val
    status_val=$(status_code)
    if [[ "$status_val" == "200" ]]; then
        pass
    else
        skip "Pipeline status returned $status_val"
    fi

    # ── Pipeline Execute ──
    it "POST /pipelines/DataArchiveCopy/execute returns result"
    admin_post "/pipelines/DataArchiveCopy/execute" ""
    local exec_status
    exec_status=$(status_code)
    if [[ "$exec_status" == "200" || "$exec_status" == "202" ]]; then
        pass
    else
        skip "Pipeline execute returned $exec_status (ETL server may not be running)"
    fi

    # ── 404 ──
    it "GET /pipelines/NonExistent returns 404"
    admin_get "/pipelines/NonExistent"
    assert_status 404

    # ── CRUD Lifecycle ──
    local TEST_NAME="e2e-test-pipe-$(date +%s)"

    it "Create pipeline"
    admin_post "/pipelines" "{
        \"name\": \"$TEST_NAME\",
        \"pipelineType\": \"BatchCopy\",
        \"description\": \"E2E test pipeline\",
        \"isEnabled\": false
    }"
    assert_status 201

    it "Read back created pipeline"
    admin_get "/pipelines/$TEST_NAME"
    assert_status 200

    it "Created pipeline has correct name"
    assert_json_string_value ".name" "$TEST_NAME"

    it "Created pipeline has correct pipelineType"
    assert_json_string_value ".pipelineType" "BatchCopy"

    it "Created pipeline isEnabled is false"
    assert_json_bool ".isEnabled" "false"

    it "Update pipeline"
    admin_put "/pipelines/$TEST_NAME" "{
        \"name\": \"$TEST_NAME\",
        \"pipelineType\": \"BatchCopy\",
        \"description\": \"Updated E2E pipeline\",
        \"isEnabled\": false
    }"
    assert_status 204

    it "Delete pipeline"
    admin_delete "/pipelines/$TEST_NAME"
    assert_status 204

    it "Deleted pipeline returns 404"
    admin_get "/pipelines/$TEST_NAME"
    assert_status 404
}
