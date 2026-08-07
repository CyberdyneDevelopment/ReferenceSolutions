#!/usr/bin/env bash
# tests/19-security-matrix.sh — Comprehensive 401/403 security sweep
#
# Permission model (from seed data):
#   Viewer (7 perms):  connections:read, datastores:read, datasets:read,
#                      pipelines:read, schedules:read, users:read, configurations:read
#   Operator (14 perms): all Viewer + connections:write, datastores:write, datasets:write,
#                        pipelines:write, pipelines:execute, schedules:write, users:write
#   Admin (21 perms):   all Operator + all :delete (6) + configurations:write
#
# User IDs:
#   admin    = BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB
#   testuser = AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA

test_security_matrix() {

    # ══════════════════════════════════════════════════════════════════════════
    # Section 1: Unauthenticated → 401 for ALL protected endpoints
    # ══════════════════════════════════════════════════════════════════════════
    describe "Security Matrix — Unauthenticated GET (401)"

    local -a protected_gets=(
        "/users"
        "/users/me"
        "/roles"
        "/roles/Admin"
        "/permissions"
        "/permissions/grouped"
        "/connections"
        "/connections/ControlDb"
        "/datastores"
        "/datastores/ControlDb"
        "/datasets"
        "/datasets/Schedules"
        "/pipelines"
        "/pipelines/DataArchiveCopy"
        "/schedules"
        "/schedules/DailyArchiveSync"
        "/connection-types"
        "/quality"
        "/promotion/environments"
        "/promotion"
        "/themes"
        "/themes/default"
        "/dataflow"
        "/connections/schema-capable"
    )

    for endpoint in "${protected_gets[@]}"; do
        it "GET $endpoint without auth returns 401"
        anon_get "$endpoint"
        assert_status 401
    done

    describe "Security Matrix — Unauthenticated POST (401)"

    local -a protected_posts=(
        "/users"
        "/roles"
        "/connections"
        "/datastores"
        "/datasets"
        "/pipelines"
        "/schedules"
    )

    for endpoint in "${protected_posts[@]}"; do
        it "POST $endpoint without auth returns 401"
        anon_post "$endpoint" '{"name":"security-test"}'
        assert_status 401
    done

    # ══════════════════════════════════════════════════════════════════════════
    # Section 2: Viewer 403 on WRITE operations
    # Viewer has ONLY :read permissions (7 total), no :write/:delete/:execute
    # ══════════════════════════════════════════════════════════════════════════
    describe "Security Matrix — Viewer Forbidden on POST (403)"

    local -a viewer_forbidden_posts=(
        "/users"
        "/roles"
        "/connections"
        "/datastores"
        "/datasets"
        "/pipelines"
        "/schedules"
    )

    for endpoint in "${viewer_forbidden_posts[@]}"; do
        it "POST $endpoint as viewer returns 403"
        viewer_post "$endpoint" '{"name":"security-test"}'
        assert_status 403
    done

    describe "Security Matrix — Viewer Forbidden on PUT (403)"

    local -a viewer_forbidden_puts=(
        "/users/AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"
        "/roles/Admin"
        "/connections/ControlDb"
        "/datastores/ControlDb"
        "/datasets/Schedules"
        "/pipelines/DataArchiveCopy"
    )

    for endpoint in "${viewer_forbidden_puts[@]}"; do
        it "PUT $endpoint as viewer returns 403"
        viewer_put "$endpoint" '{"name":"security-test"}'
        assert_status 403
    done

    describe "Security Matrix — Viewer Forbidden on DELETE (403)"

    local -a viewer_forbidden_deletes=(
        "/users/AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"
        "/roles/Admin"
        "/connections/ControlDb"
        "/datastores/ControlDb"
        "/datasets/Schedules"
        "/pipelines/DataArchiveCopy"
    )

    for endpoint in "${viewer_forbidden_deletes[@]}"; do
        it "DELETE $endpoint as viewer returns 403"
        viewer_delete "$endpoint"
        assert_status 403
    done

    # ══════════════════════════════════════════════════════════════════════════
    # Section 3: Operator 403 on DELETE and configurations:write
    # Operator has :read + :write + :execute but NOT :delete, NOT configurations:write
    # ══════════════════════════════════════════════════════════════════════════
    describe "Security Matrix — Operator Forbidden on DELETE (403)"

    local -a operator_forbidden_deletes=(
        "/users/AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"
        "/roles/Viewer"
        "/connections/ControlDb"
        "/datastores/ControlDb"
        "/datasets/Schedules"
        "/pipelines/DataArchiveCopy"
    )

    for endpoint in "${operator_forbidden_deletes[@]}"; do
        it "DELETE $endpoint as operator returns 403"
        operator_delete "$endpoint"
        assert_status 403
    done

    describe "Security Matrix — Operator Forbidden on configurations:write (403)"

    it "POST /themes as operator returns 403 (configurations:write)"
    operator_post "/themes" '{"name":"security-test","primaryColor":"#000000"}'
    assert_status 403

    # ══════════════════════════════════════════════════════════════════════════
    # Section 4: Admin 200 on ALL read endpoints
    # ══════════════════════════════════════════════════════════════════════════
    describe "Security Matrix — Admin Read Access (200)"

    local -a admin_gets=(
        "/users"
        "/users/me"
        "/roles"
        "/roles/Admin"
        "/permissions"
        "/permissions/grouped"
        "/connections"
        "/connections/ControlDb"
        "/datastores"
        "/datastores/ControlDb"
        "/datasets"
        "/datasets/Schedules"
        "/pipelines"
        "/pipelines/DataArchiveCopy"
        "/schedules"
        "/schedules/DailyArchiveSync"
        "/connection-types"
        "/quality"
        "/promotion/environments"
        "/promotion"
        "/themes"
        "/themes/default"
        "/dataflow"
        "/connections/schema-capable"
    )

    for endpoint in "${admin_gets[@]}"; do
        it "GET $endpoint as admin returns 200"
        admin_get "$endpoint"
        assert_status 200
    done
}
