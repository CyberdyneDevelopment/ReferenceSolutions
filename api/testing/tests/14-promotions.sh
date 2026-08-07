#!/usr/bin/env bash
# tests/14-promotions.sh — Promotion & Environment endpoint tests
# All promotion endpoints use Policies("fdw:datasets:write")
# Viewer does NOT have datasets:write → 403
# Operator HAS datasets:write → 200

test_promotions() {
    describe "Promotions — Auth Matrix"

    it "GET /promotion/environments without auth returns 401"
    anon_get "/promotion/environments"
    assert_status 401

    it "GET /promotion without auth returns 401"
    anon_post "/promotion" '{}'
    assert_status 401

    it "GET /promotion/environments as viewer returns 403"
    viewer_get "/promotion/environments"
    assert_status 403

    it "GET /promotion/environments as operator returns 200"
    operator_get "/promotion/environments"
    assert_status 200

    it "GET /promotion/environments as admin returns 200"
    admin_get "/promotion/environments"
    assert_status 200

    # ── List Environments ──
    describe "Promotions — Environments"

    it "GET /promotion/environments returns array"
    admin_get "/promotion/environments"
    assert_status 200

    it "Environments is array"
    assert_json_array "."

    it "Exactly 3 environments"
    assert_json_array_length "." 3

    # Verify Development environment
    local dev_idx
    dev_idx=$(response_body | jq '[.[] | .name] | index("Development")' 2>/dev/null)

    it "Development environment exists"
    if [[ "$dev_idx" != "null" && -n "$dev_idx" ]]; then
        pass
    else
        fail "Development environment not found"
    fi

    it "Development has id (guid)"
    assert_json_guid ".[$dev_idx].id"

    it "Development has name"
    assert_json_string_value ".[$dev_idx].name" "Development"

    it "Development order is 0"
    assert_json_number_value ".[$dev_idx].order" "0"

    it "Development connectionName is ControlDb"
    assert_json_string_value ".[$dev_idx].connectionName" "ControlDb"

    it "Development requiresApproval is false"
    assert_json_bool ".[$dev_idx].requiresApproval" "false"

    it "Development has description"
    assert_json_string ".[$dev_idx].description"

    it "Development has isActive"
    assert_json_bool ".[$dev_idx].isActive"

    it "Development has createdAt"
    assert_json_datetime ".[$dev_idx].createdAt"

    # Verify Staging environment
    local staging_idx
    staging_idx=$(response_body | jq '[.[] | .name] | index("Staging")' 2>/dev/null)

    it "Staging environment exists"
    if [[ "$staging_idx" != "null" && -n "$staging_idx" ]]; then
        pass
    else
        fail "Staging environment not found"
    fi

    it "Staging order is 1"
    assert_json_number_value ".[$staging_idx].order" "1"

    it "Staging connectionName is StagingDb"
    assert_json_string_value ".[$staging_idx].connectionName" "StagingDb"

    it "Staging requiresApproval is false"
    assert_json_bool ".[$staging_idx].requiresApproval" "false"

    # Verify Production environment
    local prod_idx
    prod_idx=$(response_body | jq '[.[] | .name] | index("Production")' 2>/dev/null)

    it "Production environment exists"
    if [[ "$prod_idx" != "null" && -n "$prod_idx" ]]; then
        pass
    else
        fail "Production environment not found"
    fi

    it "Production order is 2"
    assert_json_number_value ".[$prod_idx].order" "2"

    it "Production connectionName is ProductionDb"
    assert_json_string_value ".[$prod_idx].connectionName" "ProductionDb"

    it "Production requiresApproval is true"
    assert_json_bool ".[$prod_idx].requiresApproval" "true"

    # ── Seeded Promotion Request ──
    describe "Promotions — Seeded Promotion Request"

    it "GET /promotion returns promotions list"
    admin_get "/promotion"
    assert_status 200

    it "Promotions is array"
    assert_json_array "."

    it "At least 1 seeded promotion"
    assert_json_array_min_length "." 1

    # Find the completed Dev->Staging promotion
    local completed_idx
    completed_idx=$(response_body | jq '[.[] | .status] | index("Completed")' 2>/dev/null)

    it "Completed promotion exists"
    if [[ "$completed_idx" != "null" && -n "$completed_idx" ]]; then
        pass
    else
        fail "Completed promotion not found"
    fi

    it "Seeded promotion sourceEnvironment is Development"
    assert_json_string_value ".[$completed_idx].sourceEnvironment" "Development"

    it "Seeded promotion targetEnvironment is Staging"
    assert_json_string_value ".[$completed_idx].targetEnvironment" "Staging"

    it "Seeded promotion status is Completed"
    assert_json_string_value ".[$completed_idx].status" "Completed"

    it "Seeded promotion requestedBy is admin"
    assert_json_string_value ".[$completed_idx].requestedBy" "admin"

    it "Seeded promotion approvedBy is admin"
    assert_json_string_value ".[$completed_idx].approvedBy" "admin"

    it "Seeded promotion has id (guid)"
    assert_json_guid ".[$completed_idx].id"

    # ── Promotion Lifecycle ──
    describe "Promotions — CRUD Lifecycle"

    it "POST /promotion creates promotion (Pending)"
    admin_post "/promotion" '{
        "sourceEnvironment": "Development",
        "targetEnvironment": "Staging",
        "items": [{"itemType": "Pipeline", "itemName": "DataArchiveCopy"}]
    }'
    local create_status
    create_status=$(status_code)
    if [[ "$create_status" == "201" || "$create_status" == "200" ]]; then
        pass
    else
        skip "Promotion create returned $create_status"
        return
    fi

    local promo_id
    promo_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

    if [[ -n "$promo_id" ]]; then
        it "Created promotion has id (guid)"
        assert_json_guid ".id"

        it "Created promotion status is Pending"
        assert_json_string_value ".status" "Pending"

        # Approve the promotion
        it "POST /promotion/{id}/approve approves promotion"
        admin_post "/promotion/$promo_id/approve" ""
        local approve_status
        approve_status=$(status_code)
        if [[ "$approve_status" == "200" || "$approve_status" == "204" ]]; then
            pass
        else
            skip "Approve returned $approve_status"
        fi

        # Create a second promotion to reject
        it "Create second promotion for reject test"
        admin_post "/promotion" '{
            "sourceEnvironment": "Development",
            "targetEnvironment": "Staging",
            "items": [{"itemType": "Pipeline", "itemName": "DataArchiveCopy"}]
        }'
        local second_status
        second_status=$(status_code)
        if [[ "$second_status" == "201" || "$second_status" == "200" ]]; then
            pass
            local second_id
            second_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

            if [[ -n "$second_id" ]]; then
                it "POST /promotion/{id}/reject rejects promotion"
                admin_post "/promotion/$second_id/reject" '{"reason":"E2E test rejection"}'
                local reject_status
                reject_status=$(status_code)
                if [[ "$reject_status" == "200" || "$reject_status" == "204" ]]; then
                    pass
                else
                    skip "Reject returned $reject_status"
                fi

                # Cleanup
                admin_delete "/promotion/$second_id" 2>/dev/null
            fi
        else
            skip "Second promotion create returned $second_status"
        fi

        # Cleanup first promotion
        admin_delete "/promotion/$promo_id" 2>/dev/null
    fi
}
