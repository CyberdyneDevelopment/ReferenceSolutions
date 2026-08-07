#!/usr/bin/env bash
# tests/02-auth.sh — Authentication endpoint tests

test_auth() {
    describe "Authentication"

    # ── Token Endpoint ──
    it "POST /auth/token with valid admin credentials returns 200"
    anon_post "/auth/token" '{"username":"admin","password":"AdminPassword123#"}'
    assert_status 200

    it "token response has accessToken (string)"
    assert_json_string ".accessToken"

    it "token response has refreshToken (string)"
    assert_json_string ".refreshToken"

    it "token response tokenType is Bearer"
    assert_json_string_value ".tokenType" "Bearer"

    it "token response expiresIn is number"
    assert_json_number ".expiresIn"

    it "token response expiresIn is ~3600"
    assert_json_number_min ".expiresIn" 3598

    it "POST /auth/token with testuser credentials returns 200"
    anon_post "/auth/token" '{"username":"testuser","password":"TestPassword123#"}'
    assert_status 200

    it "testuser token response has accessToken"
    assert_json_string ".accessToken"

    it "POST /auth/token with bad password returns 401"
    anon_post "/auth/token" '{"username":"admin","password":"WrongPassword"}'
    assert_status 401

    it "POST /auth/token with unknown user returns 401"
    anon_post "/auth/token" '{"username":"noSuchUser","password":"anything"}'
    assert_status 401

    # ── Refresh Token ──
    # Get a fresh token pair for refresh testing
    anon_post "/auth/token" '{"username":"admin","password":"AdminPassword123#"}'
    local access_token refresh_token
    access_token=$(response_body | jq -r '.accessToken')
    refresh_token=$(response_body | jq -r '.refreshToken')

    it "POST /auth/refresh with valid tokens returns 200"
    anon_post "/auth/refresh" "{\"refreshToken\":\"$refresh_token\",\"accessToken\":\"$access_token\"}"
    assert_status 200

    it "refresh response has new accessToken"
    assert_json_string ".accessToken"

    it "refresh response has new refreshToken"
    assert_json_string ".refreshToken"

    it "refresh response tokenType is Bearer"
    assert_json_string_value ".tokenType" "Bearer"

    it "refresh response has expiresIn"
    assert_json_number ".expiresIn"

    it "POST /auth/refresh with invalid token returns 401"
    anon_post "/auth/refresh" '{"refreshToken":"invalid-garbage-token","accessToken":"invalid-garbage-token"}'
    assert_status 401

    # ── Logout ──
    # Get a fresh token for logout testing (use testuser so we don't invalidate admin)
    anon_post "/auth/token" '{"username":"testuser","password":"TestPassword123#"}'
    local logout_token
    logout_token=$(response_body | jq -r '.accessToken')

    it "POST /auth/logout with valid auth returns 204"
    http_post "/auth/logout" "" "$logout_token"
    assert_status 204

    it "POST /auth/logout without auth returns 401"
    anon_post "/auth/logout" ""
    assert_status 401
}
