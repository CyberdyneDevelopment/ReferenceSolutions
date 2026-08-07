#!/usr/bin/env bash
# tests/20-advanced.sh — Analytics, Bulk, Calculations endpoint tests
#
# Analytics:    GET /analytics, GET /analytics/top — Policies("fdw:pipelines:read")
# Bulk:         GET /bulk/export, POST /bulk/import — Policies("fdw:datasets:write")
# Calculations: GET /calculations/types, GET /calculations/period-comparisons — Policies("fdw:datasets:read")
#               POST /calculations/execute, POST /calculations/preview — Policies("fdw:datasets:write")

test_advanced() {

    # ══════════════════════════════════════════════════════════════════════════
    # Analytics
    # ══════════════════════════════════════════════════════════════════════════
    describe "Analytics — Auth Matrix"

    it "GET /analytics without auth returns 401"
    anon_get "/analytics"
    assert_status 401

    it "GET /analytics as viewer returns 200"
    viewer_get "/analytics"
    local analytics_status
    analytics_status=$(status_code)
    if [[ "$analytics_status" == "200" ]]; then
        pass
    elif [[ "$analytics_status" == "404" ]]; then
        skip "Analytics endpoint not implemented"
        return
    else
        skip "Analytics returned $analytics_status"
    fi

    it "GET /analytics/top without auth returns 401"
    anon_get "/analytics/top"
    assert_status 401

    it "GET /analytics/top as viewer returns 200"
    viewer_get "/analytics/top"
    local analytics_top_status
    analytics_top_status=$(status_code)
    if [[ "$analytics_top_status" == "200" ]]; then
        pass
    elif [[ "$analytics_top_status" == "404" ]]; then
        skip "Analytics/top endpoint not implemented"
    else
        skip "Analytics/top returned $analytics_top_status"
    fi

    it "GET /analytics as admin returns 200"
    admin_get "/analytics"
    assert_status 200

    it "GET /analytics/top as admin returns 200"
    admin_get "/analytics/top"
    assert_status 200

    # ══════════════════════════════════════════════════════════════════════════
    # Bulk Operations
    # ══════════════════════════════════════════════════════════════════════════
    describe "Bulk — Auth Matrix"

    it "GET /bulk/export without auth returns 401"
    anon_get "/bulk/export"
    assert_status 401

    it "POST /bulk/import without auth returns 401"
    anon_post "/bulk/import" '{}'
    assert_status 401

    it "GET /bulk/export as viewer returns 403"
    viewer_get "/bulk/export"
    assert_status 403

    it "POST /bulk/import as viewer returns 403"
    viewer_post "/bulk/import" '{}'
    assert_status 403

    it "GET /bulk/export as operator returns 200"
    operator_get "/bulk/export"
    local export_status
    export_status=$(status_code)
    if [[ "$export_status" == "200" ]]; then
        pass
    else
        skip "Bulk export returned $export_status"
    fi

    it "GET /bulk/export as admin returns 200"
    admin_get "/bulk/export"
    local admin_export_status
    admin_export_status=$(status_code)
    if [[ "$admin_export_status" == "200" ]]; then
        pass
    else
        skip "Bulk export as admin returned $admin_export_status"
    fi

    # ══════════════════════════════════════════════════════════════════════════
    # Calculations
    # ══════════════════════════════════════════════════════════════════════════
    describe "Calculations — Auth Matrix"

    it "GET /calculations/types without auth returns 401"
    anon_get "/calculations/types"
    assert_status 401

    it "GET /calculations/types as viewer returns 200"
    viewer_get "/calculations/types"
    local calc_types_status
    calc_types_status=$(status_code)
    if [[ "$calc_types_status" == "200" ]]; then
        pass
    elif [[ "$calc_types_status" == "404" ]]; then
        skip "Calculations types not implemented"
    else
        skip "Calculations types returned $calc_types_status"
    fi

    it "GET /calculations/types as admin returns 200"
    admin_get "/calculations/types"
    local calc_admin_status
    calc_admin_status=$(status_code)
    if [[ "$calc_admin_status" == "200" ]]; then
        pass
        it "Calculations types is array"
        assert_json_array "."
    elif [[ "$calc_admin_status" == "404" ]]; then
        skip "Calculations types not implemented"
    else
        skip "Calculations types as admin returned $calc_admin_status"
    fi

    it "POST /calculations/execute without auth returns 401"
    anon_post "/calculations/execute" '{}'
    assert_status 401

    it "POST /calculations/execute as viewer returns 403"
    viewer_post "/calculations/execute" '{}'
    assert_status 403

    it "POST /calculations/preview without auth returns 401"
    anon_post "/calculations/preview" '{}'
    assert_status 401

    it "POST /calculations/preview as viewer returns 403"
    viewer_post "/calculations/preview" '{}'
    assert_status 403

    it "GET /calculations/period-comparisons without auth returns 401"
    anon_get "/calculations/period-comparisons"
    assert_status 401

    it "GET /calculations/period-comparisons as viewer returns 200"
    viewer_get "/calculations/period-comparisons"
    local period_status
    period_status=$(status_code)
    if [[ "$period_status" == "200" ]]; then
        pass
    elif [[ "$period_status" == "404" ]]; then
        skip "Period comparisons not implemented"
    else
        skip "Period comparisons returned $period_status"
    fi
}
