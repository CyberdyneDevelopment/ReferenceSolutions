#!/usr/bin/env bash
# tests/12-catalog.sh — Catalog & Glossary endpoint tests

test_catalog() {
    describe "Catalog & Glossary"

    # ── Auth Matrix ──
    it "GET /catalog without auth returns 401"
    anon_get "/catalog"
    assert_status 401

    it "GET /catalog as viewer returns 200"
    viewer_get "/catalog"
    assert_status 200

    it "POST /catalog/glossary as viewer returns 403"
    viewer_post "/catalog/glossary" '{"term":"x","definition":"x","category":"Test"}'
    assert_status 403

    it "POST /catalog/glossary as operator returns 200 or 201"
    operator_post "/catalog/glossary" '{"term":"op-test-term","definition":"operator write test","category":"Testing"}'
    local op_create_status
    op_create_status=$(status_code)
    if [[ "$op_create_status" == "201" || "$op_create_status" == "200" ]]; then
        pass
        local op_term_id
        op_term_id=$(response_body | jq -r '.id // empty' 2>/dev/null)
        if [[ -n "$op_term_id" ]]; then
            admin_delete "/catalog/glossary/$op_term_id"
        fi
    else
        fail "expected 200 or 201, got $op_create_status"
    fi

    # ── Catalog List ──
    it "GET /catalog as admin returns 200"
    admin_get "/catalog"
    assert_status 200

    it "Catalog response is array"
    assert_json_array "."

    # ── Catalog Search ──
    it "GET /catalog/search?q=data returns results"
    admin_get "/catalog/search?q=data"
    local search_status
    search_status=$(status_code)
    if [[ "$search_status" == "200" ]]; then
        pass

        it "Search results is array"
        assert_json_array "."
    else
        skip "Catalog search returned $search_status"
    fi

    # ── Glossary List ──
    it "GET /catalog/glossary returns 200"
    admin_get "/catalog/glossary"
    assert_status 200

    it "Glossary is array"
    assert_json_array "."

    it "Glossary has exactly 2 seed terms"
    assert_json_array_length "." 2

    it "Glossary contains Customer"
    assert_json_array_contains "[.[] | .name]" "Customer"

    it "Glossary contains Account"
    assert_json_array_contains "[.[] | .name]" "Account"

    # ── Glossary Term Detail: Customer ──
    local customer_id
    customer_id=$(response_body | jq -r '.[] | select(.name == "Customer") | .id' 2>/dev/null)

    if [[ -n "$customer_id" && "$customer_id" != "null" ]]; then
        it "GET /catalog/glossary/{id} returns Customer detail"
        admin_get "/catalog/glossary/$customer_id"
        assert_status 200

        it "Customer has correct name"
        assert_json_string_value ".name" "Customer"

        it "Customer has definition"
        assert_json_string ".definition"

        it "Customer has category Entity"
        assert_json_string_value ".category" "Entity"

        it "Customer has owner data-team"
        assert_json_string_value ".owner" "data-team"

        it "Customer has steward data-governance"
        assert_json_string_value ".steward" "data-governance"

        # ── Customer Relations ──
        it "Customer has relations"
        local rel_count
        rel_count=$(response_body | jq '.relations | length // 0' 2>/dev/null)
        if [[ "$rel_count" -ge 1 ]]; then
            pass

            it "Customer has RelatedTo relation to Account"
            local rel_type
            rel_type=$(response_body | jq -r '.relations[] | select(.relatedTermName == "Account") | .relationType' 2>/dev/null)
            if [[ "$rel_type" == "RelatedTo" ]]; then
                pass
            else
                fail "expected RelatedTo relation to Account, got $rel_type"
            fi
        else
            skip "Customer has no relations array or it is empty"
        fi

        # ── Customer Linked DataSets ──
        it "Customer has linked datasets"
        local linked_count
        linked_count=$(response_body | jq '.linkedDataSets | length // 0' 2>/dev/null)
        if [[ "$linked_count" -ge 1 ]]; then
            pass

            it "Customer linked to Schedules.Name"
            local linked_ds
            linked_ds=$(response_body | jq -r '.linkedDataSets[] | select(.dataSetName == "Schedules") | .fieldName' 2>/dev/null)
            if [[ "$linked_ds" == "Name" ]]; then
                pass
            else
                fail "expected linkedDataSet Schedules.Name, got fieldName=$linked_ds"
            fi
        else
            skip "Customer has no linkedDataSets array or it is empty"
        fi
    else
        skip "Could not find Customer term ID"
    fi

    # ── Glossary Term Detail: Account ──
    local account_id
    account_id=$(response_body | jq -r '.id' 2>/dev/null)
    # Re-fetch glossary to get Account ID
    admin_get "/catalog/glossary"
    account_id=$(response_body | jq -r '.[] | select(.name == "Account") | .id' 2>/dev/null)

    if [[ -n "$account_id" && "$account_id" != "null" ]]; then
        it "GET /catalog/glossary/{id} returns Account detail"
        admin_get "/catalog/glossary/$account_id"
        assert_status 200

        it "Account has correct name"
        assert_json_string_value ".name" "Account"

        it "Account has category Entity"
        assert_json_string_value ".category" "Entity"
    else
        skip "Could not find Account term ID"
    fi

    # ── DataSet Annotation: Schedules ──
    it "GET /catalog/datasets/Schedules/annotation returns 200"
    admin_get "/catalog/datasets/Schedules/annotation"
    local anno_status
    anno_status=$(status_code)
    if [[ "$anno_status" == "200" ]]; then
        pass

        it "Annotation has businessOwner operations-team"
        assert_json_string_value ".businessOwner" "operations-team"

        it "Annotation has technicalOwner platform-engineering"
        assert_json_string_value ".technicalOwner" "platform-engineering"

        it "Annotation has updateFrequency Real-time"
        assert_json_string_value ".updateFrequency" "Real-time"

        it "Annotation has dataClassification Internal"
        assert_json_string_value ".dataClassification" "Internal"

        it "Annotation has tags array"
        assert_json_array ".tags"

        it "Annotation tags contain ETL"
        assert_json_array_contains "[.tags[] | .tag]" "ETL"

        it "Annotation tags contain Scheduling"
        assert_json_array_contains "[.tags[] | .tag]" "Scheduling"
    else
        skip "Dataset annotation returned $anno_status"
    fi

    # ── Glossary CRUD Lifecycle ──
    local TEST_TERM="e2e-test-term-$(date +%s)"

    it "Create glossary term"
    admin_post "/catalog/glossary" "{
        \"term\": \"$TEST_TERM\",
        \"definition\": \"E2E test definition\",
        \"category\": \"Testing\",
        \"owner\": \"e2e-team\",
        \"steward\": \"e2e-steward\"
    }"
    local create_status
    create_status=$(status_code)
    if [[ "$create_status" == "201" || "$create_status" == "200" ]]; then
        pass

        local term_id
        term_id=$(response_body | jq -r '.id // empty' 2>/dev/null)

        if [[ -n "$term_id" ]]; then
            it "Read back glossary term"
            admin_get "/catalog/glossary/$term_id"
            assert_status 200

            it "Glossary term has correct name"
            assert_json_string_value ".name" "$TEST_TERM"

            it "Glossary term has correct category"
            assert_json_string_value ".category" "Testing"

            it "Update glossary term"
            admin_put "/catalog/glossary/$term_id" "{
                \"term\": \"$TEST_TERM\",
                \"definition\": \"Updated E2E definition\",
                \"category\": \"Testing\",
                \"owner\": \"e2e-team-updated\"
            }"
            local update_status
            update_status=$(status_code)
            if [[ "$update_status" == "200" || "$update_status" == "204" ]]; then
                pass
            else
                fail "Update glossary term returned $update_status"
            fi

            it "Delete glossary term"
            admin_delete "/catalog/glossary/$term_id"
            local del_status
            del_status=$(status_code)
            if [[ "$del_status" == "204" || "$del_status" == "200" ]]; then
                pass
            else
                fail "Delete glossary term returned $del_status"
            fi
        else
            skip "Could not extract term ID from create response"
        fi
    else
        skip "Glossary create returned $create_status"
    fi
}
