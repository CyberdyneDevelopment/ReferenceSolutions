#!/usr/bin/env bash
# tests/06-tenants.sh — Tenant endpoint tests
#
# Seed tenants:
#   AFC — id: 11111111-1111-1111-1111-111111111111, slug: afc, displayName: American Football Conference
#   NFC — id: 22222222-2222-2222-2222-222222222222, slug: nfc, displayName: National Football Conference
#
# Tenant endpoints have AllowAnonymous() in Debug/Develop builds.
# Any authenticated user can access them. Do NOT test for 403.

test_tenants() {
    describe "Tenants — List"

    it "GET /tenants as viewer returns 200"
    viewer_get "/tenants"
    assert_status 200

    it "GET /tenants as operator returns 200"
    operator_get "/tenants"
    assert_status 200

    it "GET /tenants as admin returns 200"
    admin_get "/tenants"
    assert_status 200

    it "tenants is array"
    assert_json_array "."

    it "tenants has at least 2 entries (AFC, NFC)"
    assert_json_array_min_length "." 2

    it "first tenant has id (GUID)"
    assert_json_guid ".[0].id"

    it "first tenant has slug (string)"
    assert_json_string ".[0].slug"

    it "first tenant has isActive (bool)"
    assert_json_bool ".[0].isActive"

    # Verify AFC tenant exists in list
    it "tenants list contains AFC (slug=afc)"
    local has_afc
    has_afc=$(response_body | jq '[.[] | select(.slug == "afc")] | length' 2>/dev/null)
    if [[ "$has_afc" -ge 1 ]]; then
        pass
    else
        fail "expected tenants to contain slug=afc"
    fi

    # Verify NFC tenant exists in list
    it "tenants list contains NFC (slug=nfc)"
    local has_nfc
    has_nfc=$(response_body | jq '[.[] | select(.slug == "nfc")] | length' 2>/dev/null)
    if [[ "$has_nfc" -ge 1 ]]; then
        pass
    else
        fail "expected tenants to contain slug=nfc"
    fi

    # ── Get AFC Tenant by Known ID ──
    describe "Tenants — AFC Detail"

    it "GET /tenants/11111111-... returns AFC tenant"
    admin_get "/tenants/11111111-1111-1111-1111-111111111111"
    assert_status 200

    it "AFC tenant id matches"
    assert_json_string_value ".id" "11111111-1111-1111-1111-111111111111"

    it "AFC tenant slug is afc"
    assert_json_string_value ".slug" "afc"

    it "AFC tenant displayName contains American Football"
    assert_json_string_contains ".displayName" "American Football"

    it "AFC tenant isActive is true"
    assert_json_bool ".isActive" "true"

    # ── Get NFC Tenant by Known ID ──
    describe "Tenants — NFC Detail"

    it "GET /tenants/22222222-... returns NFC tenant"
    admin_get "/tenants/22222222-2222-2222-2222-222222222222"
    assert_status 200

    it "NFC tenant id matches"
    assert_json_string_value ".id" "22222222-2222-2222-2222-222222222222"

    it "NFC tenant slug is nfc"
    assert_json_string_value ".slug" "nfc"

    it "NFC tenant displayName contains National Football"
    assert_json_string_contains ".displayName" "National Football"

    # ── 404 ──
    it "GET /tenants/00000000-... returns 404"
    admin_get "/tenants/00000000-0000-0000-0000-000000000000"
    assert_status 404

    # ── Current Tenant ──
    describe "Tenants — Current"

    it "GET /tenants/current without tenant claim returns 404"
    admin_get "/tenants/current"
    assert_status 404

    # ── Switch Tenant ──
    describe "Tenants — Switch"

    it "POST /tenants/switch to AFC returns 200"
    admin_post "/tenants/switch" '{"tenantId":"11111111-1111-1111-1111-111111111111"}'
    assert_status 200

    it "switch response has success=true"
    assert_json_bool ".success" "true"

    it "switch response has accessToken"
    assert_json_string ".accessToken"

    it "switch response has refreshToken"
    assert_json_string ".refreshToken"

    it "switch response has expiresIn"
    assert_json_number ".expiresIn"

    it "switch response has message"
    assert_json_string ".message"

    it "switch response has tenant object"
    assert_json_object ".tenant"

    # Use the tenant-scoped token to check current tenant
    local tenant_token
    tenant_token=$(response_body | jq -r '.accessToken')

    if [[ -n "$tenant_token" && "$tenant_token" != "null" ]]; then
        it "GET /tenants/current with tenant token returns 200"
        http_get "/tenants/current" "$tenant_token"
        assert_status 200

        it "current tenant slug is afc"
        assert_json_string_value ".slug" "afc"
    else
        skip "switch did not return accessToken, skipping current tenant check"
    fi
}
