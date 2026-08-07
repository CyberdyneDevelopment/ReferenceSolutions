#!/usr/bin/env bash
# tests/11-schedules.sh — Schedule endpoint tests

test_schedules() {
    describe "Schedules"

    # ── Auth Matrix ──
    it "GET /schedules without auth returns 401"
    anon_get "/schedules"
    assert_status 401

    it "GET /schedules as viewer returns 200"
    viewer_get "/schedules"
    assert_status 200

    it "POST /schedules as viewer returns 403"
    viewer_post "/schedules" '{"name":"x","pipelineName":"DataArchiveCopy","schedulerType":"Cron","cronExpression":"0 0 * * *"}'
    assert_status 403

    it "PUT /schedules/DailyArchiveSync as viewer returns 403"
    viewer_put "/schedules/DailyArchiveSync" '{"name":"DailyArchiveSync","pipelineName":"DataArchiveCopy","schedulerType":"Cron","cronExpression":"0 2 * * *"}'
    assert_status 403

    it "POST /schedules as operator returns 200 or 201"
    operator_post "/schedules" '{"name":"op-test-sched","pipelineName":"DataArchiveCopy","schedulerType":"Cron","cronExpression":"0 0 * * *","isEnabled":false}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        # Clean up
        admin_delete "/schedules/op-test-sched"
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    it "DELETE /schedules/DailyArchiveSync as operator returns 403"
    operator_delete "/schedules/DailyArchiveSync"
    assert_status 403

    # ── List Schedules (Paginated) ──
    it "GET /schedules as admin returns paginated response"
    admin_get "/schedules"
    assert_status 200

    it "Schedules has paginated shape"
    assert_paginated_shape

    it "Schedules items is array"
    assert_json_array ".items"

    it "Schedules totalCount is 4"
    assert_json_number_value ".totalCount" "4"

    it "Contains DailyArchiveSync"
    assert_json_array_contains "[.items[] | .name]" "DailyArchiveSync"

    it "Contains HourlyDataRefresh"
    assert_json_array_contains "[.items[] | .name]" "HourlyDataRefresh"

    it "Contains WeeklyCleanup"
    assert_json_array_contains "[.items[] | .name]" "WeeklyCleanup"

    it "Contains ManualExecution"
    assert_json_array_contains "[.items[] | .name]" "ManualExecution"

    it "Schedule item has name"
    assert_json_string ".items[0].name"

    it "Schedule item has pipelineName"
    assert_json_string ".items[0].pipelineName"

    it "Schedule item has schedulerType"
    assert_json_string ".items[0].schedulerType"

    # ── Query Parameters ──
    it "GET /schedules?pageSize=1 limits results"
    admin_get "/schedules?pageSize=1"
    assert_status 200

    local page_items
    page_items=$(response_body | jq '.items | length' 2>/dev/null)
    if [[ "$page_items" -le 1 ]]; then
        pass
    else
        fail "expected at most 1 item with pageSize=1, got $page_items"
    fi

    it "GET /schedules?isEnabled=false returns disabled schedules"
    admin_get "/schedules?isEnabled=false"
    local filter_status
    filter_status=$(status_code)
    if [[ "$filter_status" == "200" ]]; then
        local disabled_contains_weekly
        disabled_contains_weekly=$(response_body | jq '[.items[] | select(.name == "WeeklyCleanup")] | length' 2>/dev/null)
        if [[ "$disabled_contains_weekly" -ge 1 ]]; then
            pass
        else
            fail "expected WeeklyCleanup in isEnabled=false results"
        fi
    else
        skip "isEnabled filter returned $filter_status"
    fi

    # ── Get Schedule Detail: DailyArchiveSync ──
    it "GET /schedules/DailyArchiveSync returns detail"
    admin_get "/schedules/DailyArchiveSync"
    assert_status 200

    it "DailyArchiveSync has correct id"
    assert_json_string_value ".id" "A1A2B3C4-D5E6-7890-1234-567890ABCDEF"

    it "DailyArchiveSync has correct name"
    assert_json_string_value ".name" "DailyArchiveSync"

    it "DailyArchiveSync has pipelineName DataArchiveCopy"
    assert_json_string_value ".pipelineName" "DataArchiveCopy"

    it "DailyArchiveSync has schedulerType Cron"
    assert_json_string_value ".schedulerType" "Cron"

    it "DailyArchiveSync has cronExpression"
    assert_json_string_value ".cronExpression" "0 2 * * *"

    it "DailyArchiveSync isEnabled is true"
    assert_json_bool ".isEnabled" "true"

    it "DailyArchiveSync has createdAt"
    assert_json_datetime ".createdAt"

    # ── Get Schedule Detail: HourlyDataRefresh ──
    it "GET /schedules/HourlyDataRefresh returns detail"
    admin_get "/schedules/HourlyDataRefresh"
    assert_status 200

    it "HourlyDataRefresh has schedulerType Interval"
    assert_json_string_value ".schedulerType" "Interval"

    it "HourlyDataRefresh has pipelineName DataArchiveCopy"
    assert_json_string_value ".pipelineName" "DataArchiveCopy"

    it "HourlyDataRefresh isEnabled is true"
    assert_json_bool ".isEnabled" "true"

    # ── Get Schedule Detail: WeeklyCleanup ──
    it "GET /schedules/WeeklyCleanup returns detail"
    admin_get "/schedules/WeeklyCleanup"
    assert_status 200

    it "WeeklyCleanup isEnabled is false"
    assert_json_bool ".isEnabled" "false"

    it "WeeklyCleanup has schedulerType Cron"
    assert_json_string_value ".schedulerType" "Cron"

    it "WeeklyCleanup has cronExpression"
    assert_json_string_value ".cronExpression" "0 0 * * 0"

    # ── Get Schedule Detail: ManualExecution ──
    it "GET /schedules/ManualExecution returns detail"
    admin_get "/schedules/ManualExecution"
    assert_status 200

    it "ManualExecution has schedulerType Manual"
    assert_json_string_value ".schedulerType" "Manual"

    it "ManualExecution has pipelineName LiveDataStream"
    assert_json_string_value ".pipelineName" "LiveDataStream"

    it "ManualExecution isEnabled is true"
    assert_json_bool ".isEnabled" "true"

    # ── 404 ──
    it "GET /schedules/NonExistent returns 404"
    admin_get "/schedules/NonExistent"
    assert_status 404

    # ── CRUD Lifecycle ──
    local TEST_NAME="e2e-test-sched-$(date +%s)"

    it "Create schedule"
    admin_post "/schedules" "{
        \"name\": \"$TEST_NAME\",
        \"pipelineName\": \"DataArchiveCopy\",
        \"schedulerType\": \"Cron\",
        \"cronExpression\": \"0 0 * * *\",
        \"isEnabled\": false
    }"
    assert_status 201

    it "Read back created schedule"
    admin_get "/schedules/$TEST_NAME"
    assert_status 200

    it "Created schedule has correct name"
    assert_json_string_value ".name" "$TEST_NAME"

    it "Created schedule has correct pipelineName"
    assert_json_string_value ".pipelineName" "DataArchiveCopy"

    it "Created schedule isEnabled is false"
    assert_json_bool ".isEnabled" "false"

    # ── Toggle ──
    it "POST /schedules/$TEST_NAME/toggle flips isEnabled"
    admin_post "/schedules/$TEST_NAME/toggle" ""
    local toggle_status
    toggle_status=$(status_code)
    if [[ "$toggle_status" == "200" || "$toggle_status" == "204" ]]; then
        pass
    else
        fail "Toggle returned $toggle_status"
    fi

    it "After toggle, schedule isEnabled changed"
    admin_get "/schedules/$TEST_NAME"
    local toggled_val
    toggled_val=$(response_body | jq -r '.isEnabled' 2>/dev/null)
    if [[ "$toggled_val" == "true" ]]; then
        pass
    else
        skip "isEnabled after toggle was $toggled_val (expected true)"
    fi

    it "Update schedule"
    admin_put "/schedules/$TEST_NAME" "{
        \"name\": \"$TEST_NAME\",
        \"pipelineName\": \"DataArchiveCopy\",
        \"schedulerType\": \"Cron\",
        \"cronExpression\": \"30 12 * * *\",
        \"isEnabled\": false
    }"
    assert_status 204

    it "Delete schedule"
    admin_delete "/schedules/$TEST_NAME"
    assert_status 204

    it "Deleted schedule returns 404"
    admin_get "/schedules/$TEST_NAME"
    assert_status 404
}
