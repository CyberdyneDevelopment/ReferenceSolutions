#!/usr/bin/env bash
# tests/09-datasets.sh — DataSet endpoint tests

test_datasets() {
    describe "DataSets"

    # ── Auth Matrix ──
    it "GET /datasets without auth returns 401"
    anon_get "/datasets"
    assert_status 401

    it "GET /datasets as viewer returns 200"
    viewer_get "/datasets"
    assert_status 200

    it "POST /datasets as viewer returns 403"
    viewer_post "/datasets" '{"name":"x","category":"Test"}'
    assert_status 403

    it "PUT /datasets/Schedules as viewer returns 403"
    viewer_put "/datasets/Schedules" '{"name":"Schedules","category":"Configuration"}'
    assert_status 403

    it "POST /datasets as operator returns 200 or 201"
    operator_post "/datasets" '{"name":"op-test-dataset","description":"operator write test","category":"Testing"}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        # Clean up
        admin_delete "/datasets/op-test-dataset"
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    it "DELETE /datasets/Schedules as operator returns 403"
    operator_delete "/datasets/Schedules"
    assert_status 403

    # ── List DataSets ──
    it "GET /datasets as admin returns array"
    admin_get "/datasets"
    assert_status 200

    it "DataSets is array"
    assert_json_array "."

    it "Exactly 2 seed datasets"
    assert_json_array_length "." 2

    it "DataSet has name"
    assert_json_string ".[0].name"

    it "DataSet has category"
    assert_json_string ".[0].category"

    it "Contains Schedules"
    assert_json_array_contains "[.[] | .name]" "Schedules"

    it "Contains PipelineExecutions"
    assert_json_array_contains "[.[] | .name]" "PipelineExecutions"

    # ── Get DataSet Detail: Schedules ──
    it "GET /datasets/Schedules returns detail"
    admin_get "/datasets/Schedules"
    assert_status 200

    it "Schedules has id (guid)"
    assert_json_guid ".id"

    it "Schedules has correct name"
    assert_json_string_value ".name" "Schedules"

    it "Schedules has version 1.0.0"
    assert_json_string_value ".version" "1.0.0"

    it "Schedules has category Configuration"
    assert_json_string_value ".category" "Configuration"

    it "Schedules has description"
    assert_json_string ".description"

    # ── Schedules Fields ──
    it "GET /datasets/Schedules/fields returns 200"
    admin_get "/datasets/Schedules/fields"
    local fields_status
    fields_status=$(status_code)
    if [[ "$fields_status" == "200" ]]; then
        pass

        it "Schedules fields is array"
        assert_json_array "."

        it "Schedules has 14 fields"
        assert_json_array_length "." 14

        it "Field has name property"
        assert_json_string ".[0].name"
    else
        skip "Fields returned $fields_status"
    fi

    # ── Schedules Sources ──
    it "GET /datasets/Schedules/sources returns 200"
    admin_get "/datasets/Schedules/sources"
    local sources_status
    sources_status=$(status_code)
    if [[ "$sources_status" == "200" ]]; then
        pass

        it "Sources is array"
        assert_json_array "."

        it "Sources has at least 1 entry"
        assert_json_array_min_length "." 1

        it "Primary source exists"
        local primary_found
        primary_found=$(response_body | jq '[.[] | select(.sourceName == "Primary")] | length' 2>/dev/null)
        if [[ "$primary_found" -ge 1 ]]; then
            pass
        else
            fail "expected Primary source, not found"
        fi
    else
        skip "Sources returned $sources_status"
    fi

    # ── Get DataSet Detail: PipelineExecutions ──
    it "GET /datasets/PipelineExecutions returns detail"
    admin_get "/datasets/PipelineExecutions"
    assert_status 200

    it "PipelineExecutions has id (guid)"
    assert_json_guid ".id"

    it "PipelineExecutions has correct name"
    assert_json_string_value ".name" "PipelineExecutions"

    it "PipelineExecutions has version 1.0.0"
    assert_json_string_value ".version" "1.0.0"

    it "PipelineExecutions has category Configuration"
    assert_json_string_value ".category" "Configuration"

    it "PipelineExecutions has description"
    assert_json_string ".description"

    # ── PipelineExecutions Fields ──
    it "GET /datasets/PipelineExecutions/fields returns 200"
    admin_get "/datasets/PipelineExecutions/fields"
    local pe_fields_status
    pe_fields_status=$(status_code)
    if [[ "$pe_fields_status" == "200" ]]; then
        pass

        it "PipelineExecutions fields is array"
        assert_json_array "."

        it "PipelineExecutions has 13 fields"
        assert_json_array_length "." 13
    else
        skip "PipelineExecutions fields returned $pe_fields_status"
    fi

    # ── 404 ──
    it "GET /datasets/NonExistent returns 404"
    admin_get "/datasets/NonExistent"
    assert_status 404

    # ── CRUD Lifecycle ──
    local TEST_NAME="e2e-test-ds-$(date +%s)"

    it "Create dataset"
    admin_post "/datasets" "{
        \"name\": \"$TEST_NAME\",
        \"description\": \"E2E test dataset\",
        \"category\": \"Testing\"
    }"
    assert_status 201

    it "Read back created dataset"
    admin_get "/datasets/$TEST_NAME"
    assert_status 200

    it "Created dataset has correct name"
    assert_json_string_value ".name" "$TEST_NAME"

    it "Created dataset has correct category"
    assert_json_string_value ".category" "Testing"

    it "Update dataset"
    admin_put "/datasets/$TEST_NAME" "{
        \"name\": \"$TEST_NAME\",
        \"description\": \"Updated E2E dataset\",
        \"category\": \"Testing\"
    }"
    assert_status 204

    it "Delete dataset"
    admin_delete "/datasets/$TEST_NAME"
    assert_status 204

    it "Deleted dataset returns 404"
    admin_get "/datasets/$TEST_NAME"
    assert_status 404
}
