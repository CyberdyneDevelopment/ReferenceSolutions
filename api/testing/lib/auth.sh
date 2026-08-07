#!/usr/bin/env bash
# lib/auth.sh — Token management and role-based HTTP helpers

# ── Cached Tokens ──
ADMIN_TOKEN=""
OPERATOR_TOKEN=""
VIEWER_TOKEN=""
NFC_VIEWER_TOKEN=""

# ── Authentication ──
# Why /connect/token: canonical OpenIddict endpoint; /auth/token was a proxy for browser clients
# that is not discoverable via OpenID configuration and is unsuitable for scripted harness use.
# Why form-encoded: OpenIddict's password grant requires application/x-www-form-urlencoded,
# not application/json (the old proxy translated JSON bodies for browser convenience).
authenticate() {
    local username="$1"
    local password="$2"
    local tenant="${3:-}"

    local -a curl_args=(
        -s
        -X POST
        -H "Content-Type: application/x-www-form-urlencoded"
        --data-urlencode "grant_type=password"
        --data-urlencode "username=${username}"
        --data-urlencode "password=${password}"
        --data-urlencode "client_id=reference-client"
        --data-urlencode "scope=fdw.api offline_access"
    )

    if [[ -n "$tenant" ]]; then
        curl_args+=(--data-urlencode "tenant=${tenant}")
    fi

    local response
    response=$(curl "${curl_args[@]}" "${BASE_URL}/connect/token" 2>/dev/null)

    echo "$response" | jq -r '.access_token // empty' 2>/dev/null
}

init_tokens() {
    echo -e "${BOLD}Authenticating test users...${NC}"

    ADMIN_TOKEN=$(authenticate "admin" "AdminPassword1#")
    if [[ -z "$ADMIN_TOKEN" || "$ADMIN_TOKEN" == "null" ]]; then
        echo -e "${RED}FATAL: Failed to authenticate admin user${NC}"
        exit 1
    fi
    echo -e "  ${GREEN}admin${NC} authenticated"

    OPERATOR_TOKEN=$(authenticate "testuser" "TestPassword1#")
    if [[ -z "$OPERATOR_TOKEN" || "$OPERATOR_TOKEN" == "null" ]]; then
        echo -e "${RED}FATAL: Failed to authenticate operator user${NC}"
        exit 1
    fi
    echo -e "  ${GREEN}testuser${NC} (operator) authenticated"

    VIEWER_TOKEN=$(authenticate "afcuser" "AfcPassword1#")
    if [[ -z "$VIEWER_TOKEN" || "$VIEWER_TOKEN" == "null" ]]; then
        echo -e "${RED}FATAL: Failed to authenticate viewer user${NC}"
        exit 1
    fi
    echo -e "  ${GREEN}afcuser${NC} (viewer) authenticated"

    NFC_VIEWER_TOKEN=$(authenticate "nfcuser" "NfcPassword1#")
    if [[ -z "$NFC_VIEWER_TOKEN" || "$NFC_VIEWER_TOKEN" == "null" ]]; then
        echo -e "${RED}FATAL: Failed to authenticate NFC viewer user${NC}"
        exit 1
    fi
    echo -e "  ${GREEN}nfcuser${NC} (NFC viewer) authenticated"
    echo ""
}

# ── Admin Helpers ──
admin_get()    { http_get    "$1" "$ADMIN_TOKEN"; }
admin_post()   { http_post   "$1" "$2" "$ADMIN_TOKEN"; }
admin_put()    { http_put    "$1" "$2" "$ADMIN_TOKEN"; }
admin_patch()  { http_patch  "$1" "$2" "$ADMIN_TOKEN"; }
admin_delete() { http_delete "$1" "$ADMIN_TOKEN"; }

# ── Operator Helpers ──
operator_get()    { http_get    "$1" "$OPERATOR_TOKEN"; }
operator_post()   { http_post   "$1" "$2" "$OPERATOR_TOKEN"; }
operator_put()    { http_put    "$1" "$2" "$OPERATOR_TOKEN"; }
operator_patch()  { http_patch  "$1" "$2" "$OPERATOR_TOKEN"; }
operator_delete() { http_delete "$1" "$OPERATOR_TOKEN"; }

# ── Viewer Helpers ──
viewer_get()    { http_get    "$1" "$VIEWER_TOKEN"; }
viewer_post()   { http_post   "$1" "$2" "$VIEWER_TOKEN"; }
viewer_put()    { http_put    "$1" "$2" "$VIEWER_TOKEN"; }
viewer_patch()  { http_patch  "$1" "$2" "$VIEWER_TOKEN"; }
viewer_delete() { http_delete "$1" "$VIEWER_TOKEN"; }

# ── Anonymous Helpers (no auth) ──
anon_get()    { http_get    "$1"; }
anon_post()   { http_post   "$1" "$2"; }
anon_put()    { http_put    "$1" "$2"; }
anon_patch()  { http_patch  "$1" "$2"; }
anon_delete() { http_delete "$1"; }
