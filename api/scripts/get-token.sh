#!/usr/bin/env bash
# get-token.sh — Standalone ROPC/password-grant token harness for UI-CTC / API-CTC live verification.
#
# Usage:
#   ./get-token.sh <username> <password> [server] [scope]
#
# Defaults:
#   server  — https://localhost:5007
#   scope   — fdw.api offline_access
#
# Exit codes:
#   0 — success: token returned and /users/me validated
#   1 — auth failure (wrong credentials, grant denied, 4xx)
#   2 — HTTP/network failure (server unreachable, TLS error, non-JSON response)
#
# Why /connect/token: canonical OpenIddict endpoint (the /auth/token-switch proxy works too,
# but /connect/token is discoverable via OpenID configuration and preferred for scripted use).
# Why -k: reference-api uses a self-signed cert in dev; callers may override with
# CURL_CA_BUNDLE pointing to a trusted CA bundle.

set -euo pipefail

# ── Arguments ──
USERNAME="${1:-}"
PASSWORD="${2:-}"
SERVER="${3:-https://localhost:5007}"
SCOPE="${4:-fdw.api offline_access}"

if [[ -z "$USERNAME" || -z "$PASSWORD" ]]; then
    echo "Usage: $0 <username> <password> [server] [scope]" >&2
    exit 2
fi

# ── Helpers ──
_err() { echo "[ERROR] $*" >&2; }
_info() { echo "[INFO]  $*" >&2; }

# ── Step 1: Request token via ROPC password grant ──
_info "Requesting token for '${USERNAME}' from ${SERVER}/connect/token"

TOKEN_RESPONSE=$(curl \
    --silent \
    --show-error \
    --fail-with-body \
    -k \
    -X POST \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "grant_type=password" \
    --data-urlencode "username=${USERNAME}" \
    --data-urlencode "password=${PASSWORD}" \
    --data-urlencode "client_id=reference-client" \
    --data-urlencode "scope=${SCOPE}" \
    "${SERVER}/connect/token" 2>&1) || {
    _err "curl failed — server unreachable or TLS error. Is ${SERVER} running?"
    exit 2
}

# Validate JSON response
if ! echo "${TOKEN_RESPONSE}" | jq . >/dev/null 2>&1; then
    _err "Response is not valid JSON. Raw response:"
    echo "${TOKEN_RESPONSE}" >&2
    exit 2
fi

# Check for OAuth error response
OAUTH_ERROR=$(echo "${TOKEN_RESPONSE}" | jq -r '.error // empty')
if [[ -n "${OAUTH_ERROR}" ]]; then
    OAUTH_DESC=$(echo "${TOKEN_RESPONSE}" | jq -r '.error_description // "no description"')
    _err "Auth failed: ${OAUTH_ERROR} — ${OAUTH_DESC}"
    exit 1
fi

# Extract token fields
ACCESS_TOKEN=$(echo "${TOKEN_RESPONSE}" | jq -r '.access_token // empty')
REFRESH_TOKEN=$(echo "${TOKEN_RESPONSE}" | jq -r '.refresh_token // empty')
EXPIRES_IN=$(echo "${TOKEN_RESPONSE}" | jq -r '.expires_in // empty')
TOKEN_TYPE=$(echo "${TOKEN_RESPONSE}" | jq -r '.token_type // empty')

if [[ -z "${ACCESS_TOKEN}" ]]; then
    _err "Token response missing access_token. Full response:"
    echo "${TOKEN_RESPONSE}" >&2
    exit 1
fi

_info "Token granted. expires_in=${EXPIRES_IN}s token_type=${TOKEN_TYPE}"

# ── Step 2: Verify token via GET /users/me ──
_info "Verifying token via GET ${SERVER}/users/me"

ME_HTTP_STATUS=$(curl \
    --silent \
    --show-error \
    -k \
    -o /tmp/get-token-me-body.json \
    -w '%{http_code}' \
    -H "Authorization: Bearer ${ACCESS_TOKEN}" \
    "${SERVER}/users/me" 2>/dev/null) || {
    _err "curl failed on /users/me — server error or network failure"
    exit 2
}

ME_BODY=$(cat /tmp/get-token-me-body.json 2>/dev/null || echo "{}")

if [[ "${ME_HTTP_STATUS}" == "200" ]]; then
    ME_USERNAME=$(echo "${ME_BODY}" | jq -r '.username // .userName // empty' 2>/dev/null)
    _info "/users/me returned 200 (username=${ME_USERNAME})"
elif [[ "${ME_HTTP_STATUS}" == "403" ]]; then
    _info "/users/me returned 403 — token valid but user lacks users:read permission"
else
    _err "/users/me returned ${ME_HTTP_STATUS}"
    echo "${ME_BODY}" >&2
    exit 1
fi

# ── Step 3: Output result JSON to stdout ──
jq -n \
    --arg accessToken "${ACCESS_TOKEN}" \
    --arg refreshToken "${REFRESH_TOKEN}" \
    --argjson expiresIn "${EXPIRES_IN:-0}" \
    --arg tokenType "${TOKEN_TYPE}" \
    --arg username "${USERNAME}" \
    --arg server "${SERVER}" \
    --arg meStatus "${ME_HTTP_STATUS}" \
    '{
        accessToken: $accessToken,
        refreshToken: $refreshToken,
        expiresIn: $expiresIn,
        tokenType: $tokenType,
        username: $username,
        server: $server,
        usersMe: { status: ($meStatus | tonumber) }
    }'

exit 0
