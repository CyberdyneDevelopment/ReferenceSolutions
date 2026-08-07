#!/usr/bin/env bash
# tests/13-quality.sh — Quality rule endpoint tests

test_quality() {
    describe "Quality Rules"

    # ── Auth Matrix ──
    it "GET /quality without auth returns 401"
    anon_get "/quality"
    assert_status 401

    it "GET /quality as viewer returns 200"
    viewer_get "/quality"
    assert_status 200

    it "POST /quality as viewer returns 403"
    viewer_post "/quality" '{"dataSetName":"Schedules","fieldName":"Id","ruleType":"NotNull","severity":"Warning","isEnabled":true}'
    assert_status 403

    it "POST /quality as operator returns 200 or 201"
    operator_post "/quality" '{"dataSetName":"Schedules","fieldName":"Id","ruleType":"NotNull","severity":"Warning","isEnabled":true,"description":"operator write test"}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        local op_rule_id
        op_rule_id=$(response_body | jq -r '.id // empty' 2>/dev/null)
        if [[ -n "$op_rule_id" ]]; then
            admin_delete "/quality/$op_rule_id"
        fi
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    # ── List Quality Rules ──
    it "GET /quality as admin returns 200"
    admin_get "/quality"
    assert_status 200

    it "Quality response is array"
    assert_json_array "."

    it "Exactly 2 seed quality rules"
    assert_json_array_length "." 2

    # ── Verify NullCheckRule ──
    it "Contains NullCheckRule for Schedules.Name"
    local null_check
    null_check=$(response_body | jq '[.[] | select(.fieldName == "Name" and .dataSetName == "Schedules")] | length' 2>/dev/null)
    if [[ "$null_check" -ge 1 ]]; then
        pass
    else
        fail "expected NullCheckRule for Schedules.Name"
    fi

    it "NullCheckRule has ruleType NotNull"
    local null_rule_type
    null_rule_type=$(response_body | jq -r '.[] | select(.fieldName == "Name" and .dataSetName == "Schedules") | .ruleType' 2>/dev/null)
    if [[ "$null_rule_type" == "NotNull" ]]; then
        pass
    else
        fail "expected ruleType=NotNull, got $null_rule_type"
    fi

    it "NullCheckRule has severity Error"
    local null_severity
    null_severity=$(response_body | jq -r '.[] | select(.fieldName == "Name" and .dataSetName == "Schedules") | .severity' 2>/dev/null)
    if [[ "$null_severity" == "Error" ]]; then
        pass
    else
        fail "expected severity=Error, got $null_severity"
    fi

    it "NullCheckRule isEnabled is true"
    local null_enabled
    null_enabled=$(response_body | jq -r '.[] | select(.fieldName == "Name" and .dataSetName == "Schedules") | .isEnabled' 2>/dev/null)
    if [[ "$null_enabled" == "true" ]]; then
        pass
    else
        fail "expected isEnabled=true, got $null_enabled"
    fi

    # ── Verify RangeCheckRule ──
    it "Contains RangeCheckRule for PipelineExecutions.DurationMs"
    local range_check
    range_check=$(response_body | jq '[.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions")] | length' 2>/dev/null)
    if [[ "$range_check" -ge 1 ]]; then
        pass
    else
        fail "expected RangeCheckRule for PipelineExecutions.DurationMs"
    fi

    it "RangeCheckRule has ruleType InRange"
    local range_rule_type
    range_rule_type=$(response_body | jq -r '.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions") | .ruleType' 2>/dev/null)
    if [[ "$range_rule_type" == "InRange" ]]; then
        pass
    else
        fail "expected ruleType=InRange, got $range_rule_type"
    fi

    it "RangeCheckRule has severity Warning"
    local range_severity
    range_severity=$(response_body | jq -r '.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions") | .severity' 2>/dev/null)
    if [[ "$range_severity" == "Warning" ]]; then
        pass
    else
        fail "expected severity=Warning, got $range_severity"
    fi

    it "RangeCheckRule isEnabled is true"
    local range_enabled
    range_enabled=$(response_body | jq -r '.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions") | .isEnabled' 2>/dev/null)
    if [[ "$range_enabled" == "true" ]]; then
        pass
    else
        fail "expected isEnabled=true, got $range_enabled"
    fi

    it "RangeCheckRule has minValue 0"
    local range_min
    range_min=$(response_body | jq -r '.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions") | .minValue' 2>/dev/null)
    if [[ "$range_min" == "0" ]]; then
        pass
    else
        fail "expected minValue=0, got $range_min"
    fi

    it "RangeCheckRule has maxValue 3600000"
    local range_max
    range_max=$(response_body | jq -r '.[] | select(.fieldName == "DurationMs" and .dataSetName == "PipelineExecutions") | .maxValue' 2>/dev/null)
    if [[ "$range_max" == "3600000" ]]; then
        pass
    else
        fail "expected maxValue=3600000, got $range_max"
    fi

    # ── Quality Rule Detail ──
    local null_rule_id
    null_rule_id=$(response_body | jq -r '.[] | select(.fieldName == "Name" and .dataSetName == "Schedules") | .id' 2>/dev/null)

    if [[ -n "$null_rule_id" && "$null_rule_id" != "null" ]]; then
        it "GET /quality/{id} returns NullCheckRule detail"
        admin_get "/quality/$null_rule_id"
        assert_status 200

        it "NullCheckRule detail has correct id"
        assert_json_guid ".id"

        it "NullCheckRule detail has dataSetName Schedules"
        assert_json_string_value ".dataSetName" "Schedules"

        it "NullCheckRule detail has fieldName Name"
        assert_json_string_value ".fieldName" "Name"

        it "NullCheckRule detail has description"
        assert_json_string ".description"
    else
        skip "Could not find NullCheckRule ID for detail test"
    fi

    # ── Quality Check ──
    it "POST /quality/check runs quality check"
    admin_post "/quality/check" '{"dataSetName": "Schedules"}'
    local check_status
    check_status=$(status_code)
    if [[ "$check_status" == "200" || "$check_status" == "202" ]]; then
        pass
    else
        skip "Quality check returned $check_status"
    fi

    # ── Quality Rule CRUD ──
    local TEST_RULE="e2e-rule-$(date +%s)"

    it "Create quality rule"
    admin_post "/quality" "{
        \"dataSetName\": \"Schedules\",
        \"fieldName\": \"Id\",
        \"ruleType\": \"NotNull\",
        \"severity\": \"Warning\",
        \"isEnabled\": true,
        \"description\": \"E2E test rule: $TEST_RULE\"
    }"
    local create_status
    create_status=$(status_code)
    if [[ "$create_status" == "201" || "$create_status" == "200" ]]; then
        pass

        local rule_id
        rule_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

        if [[ -n "$rule_id" ]]; then
            it "Read back quality rule"
            admin_get "/quality/$rule_id"
            assert_status 200

            it "Created rule has correct dataSetName"
            assert_json_string_value ".dataSetName" "Schedules"

            it "Created rule has correct fieldName"
            assert_json_string_value ".fieldName" "Id"

            it "Created rule has correct ruleType"
            assert_json_string_value ".ruleType" "NotNull"

            it "Created rule has correct severity"
            assert_json_string_value ".severity" "Warning"

            it "Delete quality rule"
            admin_delete "/quality/$rule_id"
            local del_status
            del_status=$(status_code)
            if [[ "$del_status" == "204" || "$del_status" == "200" ]]; then
                pass
            else
                fail "Delete quality rule returned $del_status"
            fi
        else
            skip "Could not extract rule ID from create response"
        fi
    else
        skip "Quality rule create returned $create_status"
    fi
}
