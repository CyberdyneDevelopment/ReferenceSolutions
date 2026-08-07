#!/usr/bin/env bash
# tests/18-proxy.sh — Inter-service proxy endpoint tests
# Proxy ETL: fdw:pipelines:execute → Operator+ (Viewer 403)
# Proxy Schedules GET: fdw:schedules:read → Viewer+ (200)
# Proxy Schedules POST: fdw:schedules:write → Operator+
# 502/503 acceptable when upstream services are not running.

test_proxy() {
    describe "Proxy — ETL Auth Matrix"

    # POST /proxy/etl/trigger: anon→401, viewer→403, operator→200/502
    it "POST /proxy/etl/trigger without auth returns 401"
    anon_post "/proxy/etl/trigger" '{"pipelineName":"DataArchiveCopy"}'
    assert_status 401

    it "POST /proxy/etl/trigger as viewer returns 403"
    viewer_post "/proxy/etl/trigger" '{"pipelineName":"DataArchiveCopy"}'
    assert_status 403

    it "POST /proxy/etl/trigger as operator"
    operator_post "/proxy/etl/trigger" '{"pipelineName":"DataArchiveCopy"}'
    local etl_op_status
    etl_op_status=$(status_code)
    if [[ "$etl_op_status" == "200" || "$etl_op_status" == "202" ]]; then
        pass
    elif [[ "$etl_op_status" == "502" || "$etl_op_status" == "503" ]]; then
        skip "ETL server unavailable ($etl_op_status) — auth passed"
    else
        fail "ETL trigger as operator returned $etl_op_status"
    fi

    it "POST /proxy/etl/trigger as admin"
    admin_post "/proxy/etl/trigger" '{"pipelineName":"DataArchiveCopy"}'
    local etl_admin_status
    etl_admin_status=$(status_code)
    if [[ "$etl_admin_status" == "200" || "$etl_admin_status" == "202" ]]; then
        pass
    elif [[ "$etl_admin_status" == "502" || "$etl_admin_status" == "503" ]]; then
        skip "ETL server unavailable ($etl_admin_status) — auth passed"
    else
        fail "ETL trigger as admin returned $etl_admin_status"
    fi

    # ── Proxy Schedules GET ──
    describe "Proxy — Schedules GET Auth Matrix"

    # GET /proxy/schedules: anon→401, viewer→200/502, operator→200/502
    it "GET /proxy/schedules without auth returns 401"
    anon_get "/proxy/schedules"
    assert_status 401

    it "GET /proxy/schedules as viewer"
    viewer_get "/proxy/schedules"
    local sched_viewer_status
    sched_viewer_status=$(status_code)
    if [[ "$sched_viewer_status" == "200" ]]; then
        pass
    elif [[ "$sched_viewer_status" == "502" || "$sched_viewer_status" == "503" ]]; then
        skip "Scheduler server unavailable ($sched_viewer_status) — auth passed"
    else
        fail "Proxy schedules GET as viewer returned $sched_viewer_status"
    fi

    it "GET /proxy/schedules as operator"
    operator_get "/proxy/schedules"
    local sched_op_status
    sched_op_status=$(status_code)
    if [[ "$sched_op_status" == "200" ]]; then
        pass
    elif [[ "$sched_op_status" == "502" || "$sched_op_status" == "503" ]]; then
        skip "Scheduler server unavailable ($sched_op_status) — auth passed"
    else
        fail "Proxy schedules GET as operator returned $sched_op_status"
    fi

    it "GET /proxy/schedules as admin"
    admin_get "/proxy/schedules"
    local sched_admin_status
    sched_admin_status=$(status_code)
    if [[ "$sched_admin_status" == "200" ]]; then
        pass
    elif [[ "$sched_admin_status" == "502" || "$sched_admin_status" == "503" ]]; then
        skip "Scheduler server unavailable ($sched_admin_status) — auth passed"
    else
        fail "Proxy schedules GET as admin returned $sched_admin_status"
    fi

    # ── Proxy Schedules POST ──
    describe "Proxy — Schedules POST Auth Matrix"

    # POST /proxy/schedules: anon→401, viewer→403, operator→200/502
    it "POST /proxy/schedules without auth returns 401"
    anon_post "/proxy/schedules" '{"name":"e2e-test"}'
    assert_status 401

    it "POST /proxy/schedules as viewer returns 403"
    viewer_post "/proxy/schedules" '{"name":"e2e-test"}'
    assert_status 403

    it "POST /proxy/schedules as operator"
    operator_post "/proxy/schedules" '{"name":"e2e-test"}'
    local sched_post_op_status
    sched_post_op_status=$(status_code)
    if [[ "$sched_post_op_status" == "200" || "$sched_post_op_status" == "201" || "$sched_post_op_status" == "202" ]]; then
        pass
    elif [[ "$sched_post_op_status" == "502" || "$sched_post_op_status" == "503" ]]; then
        skip "Scheduler server unavailable ($sched_post_op_status) — auth passed"
    else
        fail "Proxy schedules POST as operator returned $sched_post_op_status"
    fi

    it "POST /proxy/schedules as admin"
    admin_post "/proxy/schedules" '{"name":"e2e-test"}'
    local sched_post_admin_status
    sched_post_admin_status=$(status_code)
    if [[ "$sched_post_admin_status" == "200" || "$sched_post_admin_status" == "201" || "$sched_post_admin_status" == "202" ]]; then
        pass
    elif [[ "$sched_post_admin_status" == "502" || "$sched_post_admin_status" == "503" ]]; then
        skip "Scheduler server unavailable ($sched_post_admin_status) — auth passed"
    else
        fail "Proxy schedules POST as admin returned $sched_post_admin_status"
    fi
}
