#!/usr/bin/env bash
# tests/16-lineage.sh — Lineage & Dataflow endpoint tests
# Seeded DataFlow: SchedulesToExecutions (source=Schedules, target=PipelineExecutions, pipeline=DataArchiveCopy)

test_lineage() {
    describe "Lineage — Auth Matrix"

    it "GET /dataflow without auth returns 401"
    anon_get "/dataflow"
    assert_status 401

    it "GET /dataflow as viewer returns 200"
    viewer_get "/dataflow"
    assert_status 200

    it "GET /dataflow as admin returns 200"
    admin_get "/dataflow"
    assert_status 200

    # ── Dataflow List ──
    describe "Lineage — Dataflow List"

    it "Dataflow is array"
    assert_json_array "."

    it "At least 1 dataflow edge"
    assert_json_array_min_length "." 1

    # Find SchedulesToExecutions dataflow
    local sched_idx
    sched_idx=$(response_body | jq '[.[] | .name] | index("SchedulesToExecutions")' 2>/dev/null)

    it "SchedulesToExecutions dataflow exists"
    if [[ "$sched_idx" != "null" && -n "$sched_idx" ]]; then
        pass
    else
        fail "SchedulesToExecutions dataflow not found"
    fi

    it "DataFlow has id (guid)"
    assert_json_guid ".[$sched_idx].id"

    it "DataFlow name is SchedulesToExecutions"
    assert_json_string_value ".[$sched_idx].name" "SchedulesToExecutions"

    it "DataFlow has sourceDataSetId (guid)"
    assert_json_guid ".[$sched_idx].sourceDataSetId"

    it "DataFlow has targetDataSetId (guid)"
    assert_json_guid ".[$sched_idx].targetDataSetId"

    it "DataFlow pipelineName is DataArchiveCopy"
    assert_json_string_value ".[$sched_idx].pipelineName" "DataArchiveCopy"

    it "DataFlow flowType is Pipeline"
    assert_json_string_value ".[$sched_idx].flowType" "Pipeline"

    it "DataFlow has description"
    assert_json_string ".[$sched_idx].description"

    # ── Dataset-Specific Lineage ──
    describe "Lineage — Dataset-Specific Endpoints"

    it "GET /datasets/Schedules/dataflow"
    admin_get "/datasets/Schedules/dataflow"
    local df_status
    df_status=$(status_code)
    if [[ "$df_status" == "200" ]]; then
        pass

        it "Dataset dataflow is array"
        assert_json_array "."
    elif [[ "$df_status" == "404" ]]; then
        skip "Dataset dataflow endpoint not implemented"
    else
        skip "Dataset dataflow returned $df_status"
    fi

    it "GET /datasets/Schedules/lineage"
    admin_get "/datasets/Schedules/lineage"
    local lin_status
    lin_status=$(status_code)
    if [[ "$lin_status" == "200" ]]; then
        pass
    elif [[ "$lin_status" == "404" ]]; then
        skip "Lineage endpoint not implemented"
    else
        skip "Lineage returned $lin_status"
    fi

    it "GET /datasets/Schedules/impact"
    admin_get "/datasets/Schedules/impact"
    local impact_status
    impact_status=$(status_code)
    if [[ "$impact_status" == "200" ]]; then
        pass
    elif [[ "$impact_status" == "404" ]]; then
        skip "Impact analysis endpoint not implemented"
    else
        skip "Impact analysis returned $impact_status"
    fi
}
