#!/usr/bin/env bash
# lib/token-harness.sh — Reusable token harness library for CI/automation.
#
# Exports:
#   get_token(username, password, server)     — obtain access_token via ROPC password grant
#   verify_token(token, server)               — validate via GET /users/me
#   introspect_token(token, server)           — call /connect/introspect (requires client creds)
#
# Why this file exists: authenticate() in auth.sh is integrated with the E2E test framework
# (BASE_URL, colour output, ADMIN_TOKEN globals). CI/automation needs a standalone library
# that returns raw JSON, writes nothing to stdout except results, and returns explicit exit codes.
#
# All functions:
#   - write informational messages to stderr only
#   - write result JSON to stdout
#   - return exit code 0 (success), 1 (auth/permission failure), 2 (network/parse failure)
#
# Source this file to use its functions:
#   source "$(dirname "${BASH_SOURCE[0]}")/token-harness.sh"

# ── Default server ──
TOKEN_HARNESS_DEFAULT_SERVER="${TOKEN_HARNESS_DEFAULT_SERVER:-https://localhost:5007}"

# ── Internal helper ──
_th_err() { echo "[token-harness] ERROR: $*" >&2; }
_th_info() { echo "[token-harness] INFO:  $*" >&2; }

# ── get_token ──
# Obtains a bearer token via ROPC password grant against /connect/token.
#
# Arguments:
#   $1  username
#   $2  password
#   $3  server (optional, default: TOKEN_HARNESS_DEFAULT_SERVER)
#
# Stdout: JSON object { accessToken, refreshToken, expiresIn, tokenType }
# Exit:   0 success | 1 auth failure | 2 network/parse failure
get_token() {
    local username="$1"
    local password="$2"
    local server="${3:-${TOKEN_HARNESS_DEFAULT_SERVER}}"

    if [[ -z "${username}" || -z "${password}" ]]; then
        _th_err "get_token: username and password are required"
        return 2
    fi

    _th_info "get_token: POST ${server}/connect/token (user=${username})"

    local response
    response=$(curl \
        --silent \
        --show-error \
        -k \
        -X POST \
        -H "Content-Type: application/x-www-form-urlencoded" \
        --data-urlencode "grant_type=password" \
        --data-urlencode "username=${username}" \
        --data-urlencode "password=${password}" \
        --data-urlencode "client_id=reference-client" \
        --data-urlencode "scope=fdw.api offline_access" \
        "${server}/connect/token" 2>&1) || {
        _th_err "get_token: curl failed — ${server} unreachable or TLS error"
        return 2
    }

    if ! echo "${response}" | jq . >/dev/null 2>&1; then
        _th_err "get_token: response is not valid JSON"
        echo "${response}" >&2
        return 2
    fi

    local oauth_error
    oauth_error=$(echo "${response}" | jq -r '.error // empty')
    if [[ -n "${oauth_error}" ]]; then
        local oauth_desc
        oauth_desc=$(echo "${response}" | jq -r '.error_description // "no description"')
        _th_err "get_token: OAuth error — ${oauth_error}: ${oauth_desc}"
        echo "${response}"
        return 1
    fi

    local access_token
    access_token=$(echo "${response}" | jq -r '.access_token // empty')
    if [[ -z "${access_token}" ]]; then
        _th_err "get_token: response missing access_token"
        echo "${response}" >&2
        return 1
    fi

    _th_info "get_token: token granted (expires_in=$(echo "${response}" | jq -r '.expires_in // "unknown"')s)"

    # Normalise field names to camelCase for consistent consumer contract
    echo "${response}" | jq '{
        accessToken:  .access_token,
        refreshToken: (.refresh_token // null),
        expiresIn:    (.expires_in   // 0),
        tokenType:    (.token_type   // "Bearer")
    }'
}

# ── verify_token ──
# Verifies a bearer token is valid by calling GET /users/me.
#
# Arguments:
#   $1  access_token
#   $2  server (optional, default: TOKEN_HARNESS_DEFAULT_SERVER)
#
# Stdout: JSON object { valid, httpStatus, username?, email?, roles? }
# Exit:   0 success (token accepted — 200 or 403)
#         1 rejected (401 / unexpected 4xx)
#         2 network/parse failure
verify_token() {
    local token="$1"
    local server="${2:-${TOKEN_HARNESS_DEFAULT_SERVER}}"

    if [[ -z "${token}" ]]; then
        _th_err "verify_token: token is required"
        return 2
    fi

    _th_info "verify_token: GET ${server}/users/me"

    local http_status body
    http_status=$(curl \
        --silent \
        --show-error \
        -k \
        -o /tmp/token-harness-me.json \
        -w '%{http_code}' \
        -H "Authorization: Bearer ${token}" \
        "${server}/users/me" 2>/dev/null) || {
        _th_err "verify_token: curl failed — ${server} unreachable or TLS error"
        return 2
    }

    body=$(cat /tmp/token-harness-me.json 2>/dev/null || echo "{}")

    _th_info "verify_token: /users/me returned HTTP ${http_status}"

    case "${http_status}" in
        200)
            echo "${body}" | jq --argjson status "${http_status}" '{
                valid:      true,
                httpStatus: $status,
                username:   (.username // .userName // null),
                email:      (.email    // null),
                roles:      (.roles    // [])
            }'
            return 0
            ;;
        403)
            # Why: 403 means the token is valid (OpenIddict accepted it) but the user lacks
            # users:read permission. The token itself is good — report valid=true so callers
            # can distinguish "bad token" (401) from "token OK, permission missing" (403).
            jq -n --argjson status "${http_status}" '{valid: true, httpStatus: $status, note: "token valid but lacks users:read permission"}'
            return 0
            ;;
        401)
            jq -n --argjson status "${http_status}" '{valid: false, httpStatus: $status, note: "token rejected (expired, malformed, or wrong audience)"}'
            return 1
            ;;
        *)
            _th_err "verify_token: unexpected HTTP ${http_status}"
            jq -n --argjson status "${http_status}" '{valid: false, httpStatus: $status}'
            return 1
            ;;
    esac
}

# ── introspect_token ──
# Calls /connect/introspect (RFC 7662) to check token metadata.
# Requires a confidential client. Uses fdw.api client credentials by default.
# Override via INTROSPECT_CLIENT_ID and INTROSPECT_CLIENT_SECRET env vars.
#
# Arguments:
#   $1  access_token
#   $2  server (optional, default: TOKEN_HARNESS_DEFAULT_SERVER)
#
# Stdout: JSON introspection response (active, sub, exp, scope, …)
# Exit:   0 success | 1 inactive token | 2 network/parse/credential failure
introspect_token() {
    local token="$1"
    local server="${2:-${TOKEN_HARNESS_DEFAULT_SERVER}}"
    local client_id="${INTROSPECT_CLIENT_ID:-fdw.api}"
    local client_secret="${INTROSPECT_CLIENT_SECRET:-}"

    if [[ -z "${token}" ]]; then
        _th_err "introspect_token: token is required"
        return 2
    fi

    if [[ -z "${client_secret}" ]]; then
        _th_err "introspect_token: INTROSPECT_CLIENT_SECRET env var is required for /connect/introspect"
        return 2
    fi

    _th_info "introspect_token: POST ${server}/connect/introspect (client=${client_id})"

    local response
    response=$(curl \
        --silent \
        --show-error \
        -k \
        -X POST \
        -H "Content-Type: application/x-www-form-urlencoded" \
        --data-urlencode "token=${token}" \
        -u "${client_id}:${client_secret}" \
        "${server}/connect/introspect" 2>&1) || {
        _th_err "introspect_token: curl failed — ${server} unreachable or TLS error"
        return 2
    }

    if ! echo "${response}" | jq . >/dev/null 2>&1; then
        _th_err "introspect_token: response is not valid JSON"
        echo "${response}" >&2
        return 2
    fi

    local active
    active=$(echo "${response}" | jq -r '.active // false')
    _th_info "introspect_token: active=${active}"

    echo "${response}"

    if [[ "${active}" == "true" ]]; then
        return 0
    else
        return 1
    fi
}
