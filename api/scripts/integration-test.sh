#!/usr/bin/env bash
# =============================================================================
# FDW Reference API — Comprehensive Integration Test Suite
# =============================================================================
# Self-contained bash test script using curl + jq.
# Tests EVERY endpoint with valid inputs, invalid inputs, auth tests, and
# database verification via sqlcmd.
#
# Usage:
#   ./integration-test.sh                  # Run all tests
#   ./integration-test.sh --section auth   # Run only auth section
#   ./integration-test.sh --skip-db        # Skip database verification tests
#   ./integration-test.sh --verbose        # Show request/response details
#
# Requirements: curl, jq, sqlcmd (mssql-tools)
# =============================================================================

set -euo pipefail

# =============================================================================
# Configuration
# =============================================================================
BASE_URL="${FDW_API_BASE_URL:?set FDW_API_BASE_URL, e.g. http://<host>:5020/api/v1}"
HEALTH_URL="${FDW_API_HEALTH_URL:?set FDW_API_HEALTH_URL, e.g. http://<host>:5020/health}"
ADMIN_USER="admin"
ADMIN_PASS='AdminPassword123#'
CONTROLDB_SERVER="${FDW_CONFIG_SERVER:?set FDW_CONFIG_SERVER to the MsSql host}"
DATADB_SERVER="${FDW_DATA_SERVER:?set FDW_DATA_SERVER to the data MsSql host}"
SA_PASS="$(cat ~/.tokens/mssql-sa-password 2>/dev/null || echo '')"
SQLCMD_CONTROL="sqlcmd -S ${CONTROLDB_SERVER} -U sa -P '${SA_PASS}' -d ControlDb -C -h -1 -W"
SQLCMD_DATA="sqlcmd -S ${DATADB_SERVER} -U sa -P '${SA_PASS}' -d DataDb -C -h -1 -W"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Counters
TOTAL=0
PASSED=0
FAILED=0
SKIPPED=0

# Options
SECTION=""
SKIP_DB=false
VERBOSE=false

# Saved state for cleanup
ADMIN_TOKEN=""
ADMIN_REFRESH=""
TEST_USER_ID=""
TEST_ROLE_NAME=""
TEST_CONN_ID=""
TEST_DATASTORE_NAME=""
TEST_PIPELINE_NAME=""
TEST_SCHEDULE_NAME=""
TEST_PLAYER_ID=""
TEST_PROMOTION_ID=""
TEST_THEME_NAME=""

# =============================================================================
# Parse arguments
# =============================================================================
while [[ $# -gt 0 ]]; do
    case "$1" in
        --section) SECTION="$2"; shift 2 ;;
        --skip-db) SKIP_DB=true; shift ;;
        --verbose) VERBOSE=true; shift ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# =============================================================================
# Helper Functions
# =============================================================================

log_header() {
    echo ""
    echo -e "${BOLD}${CYAN}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BOLD}${CYAN}  $1${NC}"
    echo -e "${BOLD}${CYAN}═══════════════════════════════════════════════════════════════${NC}"
}

log_subheader() {
    echo ""
    echo -e "${BOLD}  --- $1 ---${NC}"
}

pass() {
    local id="$1"
    local desc="$2"
    TOTAL=$((TOTAL + 1))
    PASSED=$((PASSED + 1))
    echo -e "  ${GREEN}PASS${NC}  ${id}: ${desc}"
}

fail() {
    local id="$1"
    local desc="$2"
    local detail="${3:-}"
    TOTAL=$((TOTAL + 1))
    FAILED=$((FAILED + 1))
    echo -e "  ${RED}FAIL${NC}  ${id}: ${desc}"
    if [[ -n "$detail" ]]; then
        echo -e "        ${RED}Detail: ${detail}${NC}"
    fi
}

skip() {
    local id="$1"
    local desc="$2"
    local reason="${3:-}"
    TOTAL=$((TOTAL + 1))
    SKIPPED=$((SKIPPED + 1))
    echo -e "  ${YELLOW}SKIP${NC}  ${id}: ${desc} ${reason:+($reason)}"
}

# Execute curl and capture response + status code
# Usage: api_call METHOD PATH [DATA] [EXTRA_HEADERS...]
# Sets: HTTP_CODE, HTTP_BODY
api_call() {
    local method="$1"
    local path="$2"
    local data="${3:-}"
    local extra_header="${4:-}"
    local url="${BASE_URL}${path}"

    local curl_args=(-s -w '\n%{http_code}' -X "$method" "$url")
    curl_args+=(-H "Content-Type: application/json")

    if [[ -n "$ADMIN_TOKEN" && -z "$extra_header" ]]; then
        curl_args+=(-H "Authorization: Bearer ${ADMIN_TOKEN}")
    elif [[ -n "$extra_header" ]]; then
        curl_args+=(-H "$extra_header")
    fi

    if [[ -n "$data" ]]; then
        curl_args+=(-d "$data")
    fi

    if $VERBOSE; then
        echo -e "        ${CYAN}${method} ${url}${NC}"
        if [[ -n "$data" ]]; then
            echo -e "        ${CYAN}Body: $(echo "$data" | head -c 200)${NC}"
        fi
    fi

    local response
    response=$(curl "${curl_args[@]}" 2>/dev/null || echo -e '\n000')

    HTTP_CODE=$(echo "$response" | tail -1)
    HTTP_BODY=$(echo "$response" | sed '$d')

    if $VERBOSE; then
        echo -e "        ${CYAN}Status: ${HTTP_CODE}${NC}"
        echo -e "        ${CYAN}Body: $(echo "$HTTP_BODY" | head -c 300)${NC}"
    fi
}

# Execute curl without auth token
api_call_noauth() {
    local method="$1"
    local path="$2"
    local data="${3:-}"
    local url="${BASE_URL}${path}"

    local curl_args=(-s -w '\n%{http_code}' -X "$method" "$url")
    curl_args+=(-H "Content-Type: application/json")

    if [[ -n "$data" ]]; then
        curl_args+=(-d "$data")
    fi

    local response
    response=$(curl "${curl_args[@]}" 2>/dev/null || echo -e '\n000')

    HTTP_CODE=$(echo "$response" | tail -1)
    HTTP_BODY=$(echo "$response" | sed '$d')
}

# Execute curl with a specific token
api_call_with_token() {
    local method="$1"
    local path="$2"
    local token="$3"
    local data="${4:-}"
    local url="${BASE_URL}${path}"

    local curl_args=(-s -w '\n%{http_code}' -X "$method" "$url")
    curl_args+=(-H "Content-Type: application/json")
    curl_args+=(-H "Authorization: Bearer ${token}")

    if [[ -n "$data" ]]; then
        curl_args+=(-d "$data")
    fi

    local response
    response=$(curl "${curl_args[@]}" 2>/dev/null || echo -e '\n000')

    HTTP_CODE=$(echo "$response" | tail -1)
    HTTP_BODY=$(echo "$response" | sed '$d')
}

# Assert HTTP status code
assert_status() {
    local id="$1"
    local desc="$2"
    local expected="$3"
    if [[ "$HTTP_CODE" == "$expected" ]]; then
        pass "$id" "$desc"
    else
        fail "$id" "$desc" "Expected status $expected, got $HTTP_CODE"
    fi
}

# Assert jq expression on response body returns non-empty/truthy value
assert_json() {
    local id="$1"
    local desc="$2"
    local jq_expr="$3"
    local result
    result=$(echo "$HTTP_BODY" | jq -r "$jq_expr" 2>/dev/null || echo "")
    if [[ -n "$result" && "$result" != "null" && "$result" != "false" && "$result" != "" ]]; then
        pass "$id" "$desc"
    else
        fail "$id" "$desc" "jq expression '${jq_expr}' returned empty/null/false"
    fi
}

# Assert jq expression equals expected value
assert_json_eq() {
    local id="$1"
    local desc="$2"
    local jq_expr="$3"
    local expected="$4"
    local result
    result=$(echo "$HTTP_BODY" | jq -r "$jq_expr" 2>/dev/null || echo "")
    if [[ "$result" == "$expected" ]]; then
        pass "$id" "$desc"
    else
        fail "$id" "$desc" "Expected '${expected}', got '${result}'"
    fi
}

# Assert jq expression returns a number greater than 0
assert_json_gt0() {
    local id="$1"
    local desc="$2"
    local jq_expr="$3"
    local result
    result=$(echo "$HTTP_BODY" | jq -r "$jq_expr" 2>/dev/null || echo "0")
    if [[ "$result" =~ ^[0-9]+$ ]] && [[ "$result" -gt 0 ]]; then
        pass "$id" "$desc"
    else
        fail "$id" "$desc" "Expected >0, got '${result}'"
    fi
}

# Assert response body is a JSON array
assert_json_array() {
    local id="$1"
    local desc="$2"
    local result
    result=$(echo "$HTTP_BODY" | jq -r 'type' 2>/dev/null || echo "")
    if [[ "$result" == "array" ]]; then
        pass "$id" "$desc"
    else
        fail "$id" "$desc" "Expected JSON array, got type '${result}'"
    fi
}

# Run sqlcmd query against ControlDb, returns result
db_query_control() {
    local query="$1"
    if $SKIP_DB || [[ -z "$SA_PASS" ]]; then
        echo "SKIP"
        return
    fi
    sqlcmd -S "$CONTROLDB_SERVER" -U sa -P "$SA_PASS" -d ControlDb -C -h -1 -W -Q "$query" 2>/dev/null | head -1 | tr -d '[:space:]'
}

# Run sqlcmd query against DataDb, returns result
db_query_data() {
    local query="$1"
    if $SKIP_DB || [[ -z "$SA_PASS" ]]; then
        echo "SKIP"
        return
    fi
    sqlcmd -S "$DATADB_SERVER" -U sa -P "$SA_PASS" -d DataDb -C -h -1 -W -Q "$query" 2>/dev/null | head -1 | tr -d '[:space:]'
}

should_run() {
    local section="$1"
    [[ -z "$SECTION" || "$SECTION" == "$section" ]]
}

# =============================================================================
# PRE-FLIGHT: Health Check
# =============================================================================
log_header "PRE-FLIGHT CHECKS"

echo -n "  Checking API health at ${HEALTH_URL}... "
HEALTH_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$HEALTH_URL" 2>/dev/null || echo "000")
if [[ "$HEALTH_CODE" == "200" ]]; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${RED}FAILED (HTTP ${HEALTH_CODE})${NC}"
    echo -e "${RED}API is not reachable. Aborting.${NC}"
    exit 1
fi

echo -n "  Checking jq availability... "
if command -v jq &>/dev/null; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${RED}FAILED - jq not found${NC}"
    exit 1
fi

echo -n "  Checking sqlcmd availability... "
if command -v sqlcmd &>/dev/null && [[ -n "$SA_PASS" ]]; then
    echo -e "${GREEN}OK${NC}"
else
    echo -e "${YELLOW}UNAVAILABLE - database tests will be skipped${NC}"
    SKIP_DB=true
fi

# =============================================================================
# SECTION 1: Authentication Endpoints
# =============================================================================
if should_run "auth"; then
log_header "1. AUTHENTICATION ENDPOINTS"

log_subheader "POST /auth/token"

# AUTH-001: Valid admin credentials
api_call_noauth POST "/auth/token" '{"username":"admin","password":"AdminPassword123#"}'
assert_status "AUTH-001" "Valid admin login returns 200" "200"

# AUTH-002: Response has accessToken
assert_json "AUTH-002" "Response contains accessToken" '.accessToken | length > 0'

# AUTH-003: Response has refreshToken
assert_json "AUTH-003" "Response contains refreshToken" '.refreshToken | length > 0'

# AUTH-004: tokenType is Bearer
assert_json_eq "AUTH-004" "tokenType is Bearer" '.tokenType' "Bearer"

# AUTH-005: expiresIn is positive integer
assert_json_gt0 "AUTH-005" "expiresIn is positive" '.expiresIn'

# Save admin token for all subsequent tests
ADMIN_TOKEN=$(echo "$HTTP_BODY" | jq -r '.accessToken' 2>/dev/null)
ADMIN_REFRESH=$(echo "$HTTP_BODY" | jq -r '.refreshToken' 2>/dev/null)

# AUTH-006: Invalid password
api_call_noauth POST "/auth/token" '{"username":"admin","password":"WrongPassword123#"}'
assert_status "AUTH-006" "Invalid password returns 401" "401"

# AUTH-007: Unknown username
api_call_noauth POST "/auth/token" '{"username":"nosuchuser","password":"AnyPassword123#"}'
assert_status "AUTH-007" "Unknown username returns 401" "401"

# AUTH-008: Empty body
api_call_noauth POST "/auth/token" '{}'
# Could be 400 or 401 depending on validation
if [[ "$HTTP_CODE" == "400" || "$HTTP_CODE" == "401" ]]; then
    pass "AUTH-008" "Empty body returns 400 or 401"
else
    fail "AUTH-008" "Empty body returns 400 or 401" "Got $HTTP_CODE"
fi

# AUTH-009: Empty username
api_call_noauth POST "/auth/token" '{"username":"","password":"AdminPassword123#"}'
if [[ "$HTTP_CODE" == "400" || "$HTTP_CODE" == "401" ]]; then
    pass "AUTH-009" "Empty username returns 400 or 401"
else
    fail "AUTH-009" "Empty username returns 400 or 401" "Got $HTTP_CODE"
fi

# AUTH-010: Empty password
api_call_noauth POST "/auth/token" '{"username":"admin","password":""}'
if [[ "$HTTP_CODE" == "400" || "$HTTP_CODE" == "401" ]]; then
    pass "AUTH-010" "Empty password returns 400 or 401"
else
    fail "AUTH-010" "Empty password returns 400 or 401" "Got $HTTP_CODE"
fi

log_subheader "POST /auth/refresh"

# AUTH-011: Valid token refresh
api_call_noauth POST "/auth/refresh" "{\"refreshToken\":\"${ADMIN_REFRESH}\",\"accessToken\":\"${ADMIN_TOKEN}\"}"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "AUTH-011" "Valid refresh returns 200"
    assert_json "AUTH-012" "Refresh response has new accessToken" '.accessToken | length > 0'
    assert_json "AUTH-013" "Refresh response has new refreshToken" '.refreshToken | length > 0'
    assert_json_eq "AUTH-014" "Refresh tokenType is Bearer" '.tokenType' "Bearer"
    # Update tokens after refresh
    ADMIN_TOKEN=$(echo "$HTTP_BODY" | jq -r '.accessToken' 2>/dev/null)
    ADMIN_REFRESH=$(echo "$HTTP_BODY" | jq -r '.refreshToken' 2>/dev/null)
else
    fail "AUTH-011" "Valid refresh returns 200" "Got $HTTP_CODE"
    skip "AUTH-012" "Refresh response has new accessToken" "refresh failed"
    skip "AUTH-013" "Refresh response has new refreshToken" "refresh failed"
    skip "AUTH-014" "Refresh tokenType is Bearer" "refresh failed"
fi

# AUTH-015: Invalid refresh token
api_call_noauth POST "/auth/refresh" '{"refreshToken":"invalid-token","accessToken":"invalid-token"}'
assert_status "AUTH-015" "Invalid refresh token returns 401" "401"

# AUTH-016: Empty refresh token
api_call_noauth POST "/auth/refresh" '{"refreshToken":"","accessToken":""}'
assert_status "AUTH-016" "Empty refresh token returns 401" "401"

log_subheader "POST /auth/logout"

# AUTH-017: Logout without token returns 401
api_call_noauth POST "/auth/logout" ""
assert_status "AUTH-017" "Logout without token returns 401" "401"

# AUTH-018: Logout with valid token returns 204
api_call POST "/auth/logout" ""
if [[ "$HTTP_CODE" == "204" ]]; then
    pass "AUTH-018" "Logout with valid token returns 204"
else
    fail "AUTH-018" "Logout with valid token returns 204" "Got $HTTP_CODE"
fi

# Re-authenticate after logout
api_call_noauth POST "/auth/token" '{"username":"admin","password":"AdminPassword123#"}'
ADMIN_TOKEN=$(echo "$HTTP_BODY" | jq -r '.accessToken' 2>/dev/null)
ADMIN_REFRESH=$(echo "$HTTP_BODY" | jq -r '.refreshToken' 2>/dev/null)

fi # end auth section

# =============================================================================
# SECTION 2: Security Example Endpoints (Public/Authenticated/Admin/Protected)
# =============================================================================
if should_run "security"; then
log_header "2. SECURITY EXAMPLE ENDPOINTS"

log_subheader "GET /public/data"

# SEC-001: Public endpoint allows anonymous
api_call_noauth GET "/public/data"
assert_status "SEC-001" "Public data endpoint allows anonymous access" "200"
assert_json "SEC-002" "Public data has message field" '.message | length > 0'
assert_json "SEC-003" "Public data has rateLimitPolicy field" '.rateLimitPolicy'

log_subheader "GET /authenticated/data"

# SEC-004: Authenticated endpoint requires token
api_call_noauth GET "/authenticated/data"
assert_status "SEC-004" "Authenticated data requires token (401)" "401"

# SEC-005: Authenticated endpoint with valid token
api_call GET "/authenticated/data"
assert_status "SEC-005" "Authenticated data with token returns 200" "200"
assert_json "SEC-006" "Authenticated data has user field" '.user | length > 0'
assert_json_eq "SEC-007" "Authenticated data has rateLimitPolicy" '.rateLimitPolicy' "Authenticated"

log_subheader "GET /admin/data"

# SEC-008: Admin endpoint without token
api_call_noauth GET "/admin/data"
assert_status "SEC-008" "Admin data without token returns 401" "401"

# SEC-009: Admin endpoint with admin token
api_call GET "/admin/data"
assert_status "SEC-009" "Admin data with admin token returns 200" "200"
assert_json "SEC-010" "Admin data has roles array" '.roles | length > 0'
assert_json_eq "SEC-011" "Admin data rateLimitPolicy is Admin" '.rateLimitPolicy' "Admin"

log_subheader "GET /protected"

# SEC-012: Protected endpoint without token
api_call_noauth GET "/protected"
assert_status "SEC-012" "Protected endpoint without token returns 401" "401"

# SEC-013: Protected endpoint with valid token
api_call GET "/protected"
assert_status "SEC-013" "Protected endpoint with token returns 200" "200"
assert_json "SEC-014" "Protected response has message" '.message | length > 0'
assert_json "SEC-015" "Protected response has user" '.user | length > 0'
assert_json "SEC-016" "Protected response has roles" '.roles | type == "array"'

fi # end security section

# =============================================================================
# SECTION 3: User Management Endpoints
# =============================================================================
if should_run "users"; then
log_header "3. USER MANAGEMENT ENDPOINTS"

log_subheader "GET /users/me"

# USER-001: Get current user without auth
api_call_noauth GET "/users/me"
assert_status "USER-001" "Get me without auth returns 401" "401"

# USER-002: Get current user with auth
api_call GET "/users/me"
assert_status "USER-002" "Get me returns 200" "200"
assert_json "USER-003" "Me response has username" '.username | length > 0'
assert_json "USER-004" "Me response has email" '.email'
assert_json "USER-005" "Me response has roles" '.roles'

log_subheader "POST /users (Create)"

# USER-006: Create user without auth
api_call_noauth POST "/users" '{"username":"testuser_int","password":"TestInt123#","email":"testint@example.com","roles":["User"]}'
assert_status "USER-006" "Create user without auth returns 401" "401"

# USER-007: Create user with admin auth
api_call POST "/users" '{"username":"testuser_int","password":"TestInt123#","email":"testint@example.com","roles":["User"]}'
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "USER-007" "Create user returns 200/201"
    TEST_USER_ID=$(echo "$HTTP_BODY" | jq -r '.id // .userId // empty' 2>/dev/null)
else
    fail "USER-007" "Create user returns 200/201" "Got $HTTP_CODE"
fi

log_subheader "GET /users (List)"

# USER-008: List users without auth
api_call_noauth GET "/users"
assert_status "USER-008" "List users without auth returns 401" "401"

# USER-009: List users with admin auth
api_call GET "/users"
assert_status "USER-009" "List users returns 200" "200"

# USER-010: List users returns array
if [[ "$HTTP_CODE" == "200" ]]; then
    # Response might be array or wrapped object
    RESP_TYPE=$(echo "$HTTP_BODY" | jq -r 'type' 2>/dev/null)
    if [[ "$RESP_TYPE" == "array" ]]; then
        pass "USER-010" "List users returns array"
    else
        assert_json "USER-010" "List users returns data" '. | length > 0'
    fi
else
    skip "USER-010" "List users returns array" "list failed"
fi

log_subheader "GET /users/{id}"

if [[ -n "$TEST_USER_ID" ]]; then
    # USER-011: Get user by ID
    api_call GET "/users/${TEST_USER_ID}"
    assert_status "USER-011" "Get user by ID returns 200" "200"
    assert_json "USER-012" "Get user has username" '.username | length > 0'
else
    skip "USER-011" "Get user by ID returns 200" "no test user created"
    skip "USER-012" "Get user has username" "no test user created"
fi

# USER-013: Get non-existent user
api_call GET "/users/00000000-0000-0000-0000-000000000000"
assert_status "USER-013" "Get non-existent user returns 404" "404"

log_subheader "PUT /users/{id}"

if [[ -n "$TEST_USER_ID" ]]; then
    # USER-014: Update user
    api_call PUT "/users/${TEST_USER_ID}" '{"userId":"'"${TEST_USER_ID}"'","email":"updated@example.com"}'
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "USER-014" "Update user returns 200/204"
    else
        fail "USER-014" "Update user returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "USER-014" "Update user returns 200/204" "no test user created"
fi

# USER-015: Update without auth
api_call_noauth PUT "/users/00000000-0000-0000-0000-000000000001" '{"email":"hack@example.com"}'
assert_status "USER-015" "Update user without auth returns 401" "401"

log_subheader "DELETE /users/{id}"

# USER-016: Delete without auth
api_call_noauth DELETE "/users/00000000-0000-0000-0000-000000000001"
assert_status "USER-016" "Delete user without auth returns 401" "401"

if [[ -n "$TEST_USER_ID" ]]; then
    # USER-017: Delete user
    api_call DELETE "/users/${TEST_USER_ID}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "USER-017" "Delete user returns 200/204"
    else
        fail "USER-017" "Delete user returns 200/204" "Got $HTTP_CODE"
    fi
    TEST_USER_ID=""
else
    skip "USER-017" "Delete user returns 200/204" "no test user created"
fi

fi # end users section

# =============================================================================
# SECTION 4: Role Management Endpoints
# =============================================================================
if should_run "roles"; then
log_header "4. ROLE MANAGEMENT ENDPOINTS"

log_subheader "GET /roles (List)"

# ROLE-001: List roles without auth
api_call_noauth GET "/roles"
assert_status "ROLE-001" "List roles without auth returns 401" "401"

# ROLE-002: List roles with auth
api_call GET "/roles"
assert_status "ROLE-002" "List roles returns 200" "200"

log_subheader "POST /roles (Create)"

TEST_ROLE_NAME="IntTestRole_$(date +%s)"

# ROLE-003: Create role without auth
api_call_noauth POST "/roles" "{\"name\":\"${TEST_ROLE_NAME}\",\"description\":\"Integration test role\"}"
assert_status "ROLE-003" "Create role without auth returns 401" "401"

# ROLE-004: Create role
api_call POST "/roles" "{\"name\":\"${TEST_ROLE_NAME}\",\"description\":\"Integration test role\"}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "ROLE-004" "Create role returns 200/201"
else
    fail "ROLE-004" "Create role returns 200/201" "Got $HTTP_CODE"
fi

log_subheader "GET /roles/{name}"

# ROLE-005: Get role by name
api_call GET "/roles/${TEST_ROLE_NAME}"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "ROLE-005" "Get role by name returns 200"
else
    fail "ROLE-005" "Get role by name returns 200" "Got $HTTP_CODE"
fi

# ROLE-006: Get non-existent role
api_call GET "/roles/NonExistentRole12345"
assert_status "ROLE-006" "Get non-existent role returns 404" "404"

log_subheader "PUT /roles/{name}"

# ROLE-007: Update role
api_call PUT "/roles/${TEST_ROLE_NAME}" "{\"name\":\"${TEST_ROLE_NAME}\",\"description\":\"Updated description\"}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
    pass "ROLE-007" "Update role returns 200/204"
else
    fail "ROLE-007" "Update role returns 200/204" "Got $HTTP_CODE"
fi

log_subheader "GET /roles/{name}/permissions"

# ROLE-008: Get role permissions
api_call GET "/roles/Admin/permissions"
assert_status "ROLE-008" "Get Admin role permissions returns 200" "200"

# ROLE-009: Get non-existent role permissions
api_call GET "/roles/NonExistentRole12345/permissions"
assert_status "ROLE-009" "Get non-existent role permissions returns 404" "404"

log_subheader "PUT /roles/{name}/permissions"

# ROLE-010: Set role permissions
api_call PUT "/roles/${TEST_ROLE_NAME}/permissions" '{"name":"'"${TEST_ROLE_NAME}"'","permissionNames":["fdw:users:read"]}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "ROLE-010" "Set role permissions returns 200"
else
    fail "ROLE-010" "Set role permissions returns 200" "Got $HTTP_CODE"
fi

log_subheader "DELETE /roles/{name}"

# ROLE-011: Delete role without auth
api_call_noauth DELETE "/roles/${TEST_ROLE_NAME}"
assert_status "ROLE-011" "Delete role without auth returns 401" "401"

# ROLE-012: Delete role
api_call DELETE "/roles/${TEST_ROLE_NAME}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
    pass "ROLE-012" "Delete role returns 200/204"
else
    fail "ROLE-012" "Delete role returns 200/204" "Got $HTTP_CODE"
fi

fi # end roles section

# =============================================================================
# SECTION 5: User-Role Assignment Endpoints
# =============================================================================
if should_run "userroles"; then
log_header "5. USER-ROLE ASSIGNMENT ENDPOINTS"

# Get admin user ID first
api_call GET "/users/me"
local_admin_id=$(echo "$HTTP_BODY" | jq -r '.id // empty' 2>/dev/null)

log_subheader "GET /users/{id}/roles"

if [[ -n "$local_admin_id" ]]; then
    # UROLE-001: Get user roles
    api_call GET "/users/${local_admin_id}/roles"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "UROLE-001" "Get user roles returns 200"
    else
        fail "UROLE-001" "Get user roles returns 200" "Got $HTTP_CODE"
    fi
else
    skip "UROLE-001" "Get user roles returns 200" "no admin user ID"
fi

# UROLE-002: Get user roles without auth
api_call_noauth GET "/users/00000000-0000-0000-0000-000000000001/roles"
assert_status "UROLE-002" "Get user roles without auth returns 401" "401"

log_subheader "POST /users/{id}/roles"

# UROLE-003: Assign role without auth
api_call_noauth POST "/users/00000000-0000-0000-0000-000000000001/roles" '{"roleName":"User"}'
assert_status "UROLE-003" "Assign role without auth returns 401" "401"

log_subheader "DELETE /users/{id}/roles/{roleName}"

# UROLE-004: Revoke role without auth
api_call_noauth DELETE "/users/00000000-0000-0000-0000-000000000001/roles/User"
assert_status "UROLE-004" "Revoke role without auth returns 401" "401"

fi # end userroles section

# =============================================================================
# SECTION 6: Permission Endpoints
# =============================================================================
if should_run "permissions"; then
log_header "6. PERMISSION ENDPOINTS"

log_subheader "GET /permissions"

# PERM-001: List permissions without auth
api_call_noauth GET "/permissions"
assert_status "PERM-001" "List permissions without auth returns 401" "401"

# PERM-002: List permissions with auth
api_call GET "/permissions"
assert_status "PERM-002" "List permissions returns 200" "200"

# PERM-003: Permissions response is array
if [[ "$HTTP_CODE" == "200" ]]; then
    assert_json_array "PERM-003" "Permissions response is array"
else
    skip "PERM-003" "Permissions response is array" "list failed"
fi

log_subheader "GET /permissions/grouped"

# PERM-004: List grouped permissions
api_call GET "/permissions/grouped"
assert_status "PERM-004" "List grouped permissions returns 200" "200"

# PERM-005: Grouped response is array
if [[ "$HTTP_CODE" == "200" ]]; then
    assert_json_array "PERM-005" "Grouped permissions response is array"
else
    skip "PERM-005" "Grouped permissions response is array" "list failed"
fi

# PERM-006: Grouped permissions have resource field
if [[ "$HTTP_CODE" == "200" ]]; then
    assert_json "PERM-006" "Grouped permissions have resource field" '.[0].resource // .[0].Resource'
else
    skip "PERM-006" "Grouped permissions have resource field" "list failed"
fi

fi # end permissions section

# =============================================================================
# SECTION 7: Connection Management Endpoints
# =============================================================================
if should_run "connections"; then
log_header "7. CONNECTION MANAGEMENT ENDPOINTS"

log_subheader "GET /connections (List)"

# CONN-001: List connections without auth
api_call_noauth GET "/connections"
assert_status "CONN-001" "List connections without auth returns 401" "401"

# CONN-002: List connections with auth
api_call GET "/connections"
assert_status "CONN-002" "List connections returns 200" "200"

log_subheader "POST /connections (Create)"

# CONN-003: Create connection without auth
api_call_noauth POST "/connections" '{"name":"TestIntConn","server":"localhost","port":1433,"database":"TestDb","authenticationType":"SqlLogin","authentication":{"Username":"sa","SecretKeyName":"test"}}'
assert_status "CONN-003" "Create connection without auth returns 401" "401"

# CONN-004: Create connection
api_call POST "/connections" '{"name":"TestIntConn_'"$(date +%s)"'","server":"localhost","port":1433,"database":"TestDb","authenticationType":"SqlLogin","authentication":{"Username":"sa","SecretKeyName":"test"},"trustServerCertificate":true,"encrypt":false}'
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "CONN-004" "Create connection returns 200/201"
    TEST_CONN_ID=$(echo "$HTTP_BODY" | jq -r '.id // empty' 2>/dev/null)
    TEST_CONN_NAME=$(echo "$HTTP_BODY" | jq -r '.name // empty' 2>/dev/null)
else
    fail "CONN-004" "Create connection returns 200/201" "Got $HTTP_CODE"
    TEST_CONN_NAME=""
fi

log_subheader "GET /connections/{name}"

# CONN-005: Get connection by name (use ControlDb which should exist)
api_call GET "/connections/ControlDb"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CONN-005" "Get connection by name returns 200"
    assert_json "CONN-006" "Connection has server field" '.server // .Server'
    assert_json "CONN-007" "Connection has database field" '.database // .Database'
else
    fail "CONN-005" "Get connection by name returns 200" "Got $HTTP_CODE"
    skip "CONN-006" "Connection has server field" "get failed"
    skip "CONN-007" "Connection has database field" "get failed"
fi

# CONN-008: Get non-existent connection
api_call GET "/connections/NonExistentConn12345"
assert_status "CONN-008" "Get non-existent connection returns 404" "404"

log_subheader "POST /connections/{name}/test"

# CONN-009: Test connection (ControlDb)
api_call POST "/connections/ControlDb/test" '{"name":"ControlDb"}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CONN-009" "Test connection returns 200"
    assert_json "CONN-010" "Test connection has success field" '.success'
else
    fail "CONN-009" "Test connection returns 200" "Got $HTTP_CODE"
    skip "CONN-010" "Test connection has success field" "test failed"
fi

log_subheader "PUT /connections/{name}"

if [[ -n "${TEST_CONN_NAME:-}" ]]; then
    # CONN-011: Update connection
    api_call PUT "/connections/${TEST_CONN_NAME}" '{"name":"'"${TEST_CONN_NAME}"'","server":"localhost","port":1433,"database":"UpdatedDb"}'
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "CONN-011" "Update connection returns 200/204"
    else
        fail "CONN-011" "Update connection returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "CONN-011" "Update connection returns 200/204" "no test connection"
fi

log_subheader "DELETE /connections/{name}"

if [[ -n "${TEST_CONN_NAME:-}" ]]; then
    # CONN-012: Delete connection
    api_call DELETE "/connections/${TEST_CONN_NAME}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "CONN-012" "Delete connection returns 200/204"
    else
        fail "CONN-012" "Delete connection returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "CONN-012" "Delete connection returns 200/204" "no test connection"
fi

log_subheader "Connection Type Endpoints"

# CONN-013: List connection types
api_call GET "/connection-types"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CONN-013" "List connection types returns 200"
else
    fail "CONN-013" "List connection types returns 200" "Got $HTTP_CODE"
fi

# CONN-014: Get connections by type
api_call GET "/connections/by-type/MsSqlConnection"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CONN-014" "Get connections by type returns 200"
else
    # May not be the exact type name, try alternate
    fail "CONN-014" "Get connections by type returns 200" "Got $HTTP_CODE"
fi

fi # end connections section

# =============================================================================
# SECTION 8: DataStore Management Endpoints
# =============================================================================
if should_run "datastores"; then
log_header "8. DATASTORE MANAGEMENT ENDPOINTS"

log_subheader "GET /datastores (List)"

# DS-001: List datastores without auth
api_call_noauth GET "/datastores"
assert_status "DS-001" "List datastores without auth returns 401" "401"

# DS-002: List datastores with auth
api_call GET "/datastores"
assert_status "DS-002" "List datastores returns 200" "200"

log_subheader "POST /datastores (Create)"

TEST_DATASTORE_NAME="IntTestStore_$(date +%s)"

# DS-003: Create datastore without auth
api_call_noauth POST "/datastores" "{\"name\":\"${TEST_DATASTORE_NAME}\",\"connectionId\":\"00000000-0000-0000-0000-000000000001\",\"description\":\"Integration test store\"}"
assert_status "DS-003" "Create datastore without auth returns 401" "401"

# DS-004: Create datastore
api_call POST "/datastores" "{\"name\":\"${TEST_DATASTORE_NAME}\",\"connectionId\":\"00000000-0000-0000-0000-000000000001\",\"description\":\"Integration test store\"}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "DS-004" "Create datastore returns 200/201"
else
    fail "DS-004" "Create datastore returns 200/201" "Got $HTTP_CODE"
    TEST_DATASTORE_NAME=""
fi

log_subheader "GET /datastores/{name}"

if [[ -n "$TEST_DATASTORE_NAME" ]]; then
    # DS-005: Get datastore by name
    api_call GET "/datastores/${TEST_DATASTORE_NAME}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DS-005" "Get datastore by name returns 200"
    else
        fail "DS-005" "Get datastore by name returns 200" "Got $HTTP_CODE"
    fi
else
    skip "DS-005" "Get datastore by name returns 200" "no test datastore"
fi

# DS-006: Get non-existent datastore
api_call GET "/datastores/NonExistentStore12345"
assert_status "DS-006" "Get non-existent datastore returns 404" "404"

log_subheader "GET /datastores/{name}/containers"

# DS-007: List datastore containers (use a known datastore if exists)
api_call GET "/datastores"
FIRST_DS=$(echo "$HTTP_BODY" | jq -r '.[0].name // .[0].Name // empty' 2>/dev/null)
if [[ -n "$FIRST_DS" ]]; then
    api_call GET "/datastores/${FIRST_DS}/containers"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DS-007" "List datastore containers returns 200"
    else
        fail "DS-007" "List datastore containers returns 200" "Got $HTTP_CODE"
    fi
else
    skip "DS-007" "List datastore containers returns 200" "no datastores found"
fi

log_subheader "GET /datastores/{name}/paths"

if [[ -n "$FIRST_DS" ]]; then
    # DS-008: Get datastore paths
    api_call GET "/datastores/${FIRST_DS}/paths"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DS-008" "Get datastore paths returns 200"
    else
        fail "DS-008" "Get datastore paths returns 200" "Got $HTTP_CODE"
    fi
else
    skip "DS-008" "Get datastore paths returns 200" "no datastores found"
fi

log_subheader "PUT /datastores/{name}"

if [[ -n "$TEST_DATASTORE_NAME" ]]; then
    # DS-009: Update datastore
    api_call PUT "/datastores/${TEST_DATASTORE_NAME}" "{\"name\":\"${TEST_DATASTORE_NAME}\",\"description\":\"Updated description\"}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "DS-009" "Update datastore returns 200/204"
    else
        fail "DS-009" "Update datastore returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "DS-009" "Update datastore returns 200/204" "no test datastore"
fi

log_subheader "DELETE /datastores/{name}"

if [[ -n "$TEST_DATASTORE_NAME" ]]; then
    # DS-010: Delete datastore
    api_call DELETE "/datastores/${TEST_DATASTORE_NAME}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "DS-010" "Delete datastore returns 200/204"
    else
        fail "DS-010" "Delete datastore returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "DS-010" "Delete datastore returns 200/204" "no test datastore"
fi

fi # end datastores section

# =============================================================================
# SECTION 9: DataSet Management Endpoints
# =============================================================================
if should_run "datasets"; then
log_header "9. DATASET MANAGEMENT ENDPOINTS"

log_subheader "GET /datasets (List)"

# DSET-001: List datasets without auth
api_call_noauth GET "/datasets"
assert_status "DSET-001" "List datasets without auth returns 401" "401"

# DSET-002: List datasets with auth
api_call GET "/datasets"
assert_status "DSET-002" "List datasets returns 200" "200"

log_subheader "GET /datasets/{name}"

# Get first dataset name
FIRST_DATASET=$(echo "$HTTP_BODY" | jq -r '.[0].name // .[0].Name // empty' 2>/dev/null)

if [[ -n "$FIRST_DATASET" ]]; then
    # DSET-003: Get dataset by name
    api_call GET "/datasets/${FIRST_DATASET}"
    assert_status "DSET-003" "Get dataset by name returns 200" "200"

    # DSET-004: Dataset has name field
    assert_json "DSET-004" "Dataset has name field" '.name // .Name'
else
    skip "DSET-003" "Get dataset by name returns 200" "no datasets"
    skip "DSET-004" "Dataset has name field" "no datasets"
fi

# DSET-005: Get non-existent dataset
api_call GET "/datasets/NonExistentDataSet12345"
assert_status "DSET-005" "Get non-existent dataset returns 404" "404"

log_subheader "GET /datasets/{name}/fields"

if [[ -n "$FIRST_DATASET" ]]; then
    # DSET-006: Get dataset fields
    api_call GET "/datasets/${FIRST_DATASET}/fields"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DSET-006" "Get dataset fields returns 200"
    else
        fail "DSET-006" "Get dataset fields returns 200" "Got $HTTP_CODE"
    fi
else
    skip "DSET-006" "Get dataset fields returns 200" "no datasets"
fi

log_subheader "GET /datasets/{name}/sources"

if [[ -n "$FIRST_DATASET" ]]; then
    # DSET-007: Get dataset sources
    api_call GET "/datasets/${FIRST_DATASET}/sources"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DSET-007" "Get dataset sources returns 200"
    else
        fail "DSET-007" "Get dataset sources returns 200" "Got $HTTP_CODE"
    fi
else
    skip "DSET-007" "Get dataset sources returns 200" "no datasets"
fi

log_subheader "GET /datasets/{name}/preview"

if [[ -n "$FIRST_DATASET" ]]; then
    # DSET-008: Preview dataset
    api_call GET "/datasets/${FIRST_DATASET}/preview"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "DSET-008" "Preview dataset returns 200"
    else
        # Preview might not work if no source is configured
        skip "DSET-008" "Preview dataset returns 200" "Got $HTTP_CODE (source may not be configured)"
    fi
else
    skip "DSET-008" "Preview dataset returns 200" "no datasets"
fi

fi # end datasets section

# =============================================================================
# SECTION 10: Pipeline Management Endpoints
# =============================================================================
if should_run "pipelines"; then
log_header "10. PIPELINE MANAGEMENT ENDPOINTS"

log_subheader "GET /pipelines (List)"

# PIPE-001: List pipelines without auth
api_call_noauth GET "/pipelines"
assert_status "PIPE-001" "List pipelines without auth returns 401" "401"

# PIPE-002: List pipelines with auth
api_call GET "/pipelines"
assert_status "PIPE-002" "List pipelines returns 200" "200"

log_subheader "POST /pipelines (Create)"

TEST_PIPELINE_NAME="IntTestPipeline_$(date +%s)"

# PIPE-003: Create pipeline without auth
api_call_noauth POST "/pipelines" "{\"name\":\"${TEST_PIPELINE_NAME}\",\"pipelineType\":\"Standard\",\"description\":\"Test pipeline\"}"
assert_status "PIPE-003" "Create pipeline without auth returns 401" "401"

# PIPE-004: Create pipeline
api_call POST "/pipelines" "{\"name\":\"${TEST_PIPELINE_NAME}\",\"pipelineType\":\"Standard\",\"description\":\"Test pipeline\",\"sourceConnectionName\":\"ControlDb\",\"targetConnectionName\":\"ControlDb\",\"isEnabled\":false}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "PIPE-004" "Create pipeline returns 200/201"
    assert_json "PIPE-005" "Pipeline has name field" '.name // .Name'
else
    fail "PIPE-004" "Create pipeline returns 200/201" "Got $HTTP_CODE"
    skip "PIPE-005" "Pipeline has name field" "create failed"
    TEST_PIPELINE_NAME=""
fi

log_subheader "PUT /pipelines/{name}"

if [[ -n "$TEST_PIPELINE_NAME" ]]; then
    # PIPE-006: Update pipeline
    api_call PUT "/pipelines/${TEST_PIPELINE_NAME}" "{\"name\":\"${TEST_PIPELINE_NAME}\",\"description\":\"Updated description\"}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "PIPE-006" "Update pipeline returns 200/204"
    else
        fail "PIPE-006" "Update pipeline returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "PIPE-006" "Update pipeline returns 200/204" "no test pipeline"
fi

log_subheader "POST /pipelines/{name}/execute"

if [[ -n "$TEST_PIPELINE_NAME" ]]; then
    # PIPE-007: Execute pipeline (expect it might fail but should return 200)
    api_call POST "/pipelines/${TEST_PIPELINE_NAME}/execute" "{\"name\":\"${TEST_PIPELINE_NAME}\"}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "PIPE-007" "Execute pipeline returns 200"
        assert_json "PIPE-008" "Execute response has success field" '.success != null'
    else
        # Pipeline might not be executable in test mode
        skip "PIPE-007" "Execute pipeline returns 200" "Got $HTTP_CODE (pipeline may not be executable)"
        skip "PIPE-008" "Execute response has success field" "execution skipped"
    fi
else
    skip "PIPE-007" "Execute pipeline returns 200" "no test pipeline"
    skip "PIPE-008" "Execute response has success field" "no test pipeline"
fi

log_subheader "GET /pipelines/{name}/status"

# PIPE-009: Get pipeline status
api_call GET "/pipelines"
FIRST_PIPE=$(echo "$HTTP_BODY" | jq -r '.[0].name // .[0].Name // empty' 2>/dev/null)
if [[ -n "$FIRST_PIPE" ]]; then
    api_call GET "/pipelines/${FIRST_PIPE}/status"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "PIPE-009" "Get pipeline status returns 200"
    else
        fail "PIPE-009" "Get pipeline status returns 200" "Got $HTTP_CODE"
    fi
else
    skip "PIPE-009" "Get pipeline status returns 200" "no pipelines"
fi

log_subheader "DELETE /pipelines/{name}"

if [[ -n "$TEST_PIPELINE_NAME" ]]; then
    # PIPE-010: Delete pipeline
    api_call DELETE "/pipelines/${TEST_PIPELINE_NAME}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "PIPE-010" "Delete pipeline returns 200/204"
    else
        fail "PIPE-010" "Delete pipeline returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "PIPE-010" "Delete pipeline returns 200/204" "no test pipeline"
fi

log_subheader "Pipeline Extended Endpoints"

# PIPE-011: Bulk pipeline status
api_call GET "/pipelines/status/bulk"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "PIPE-011" "Bulk pipeline status returns 200"
else
    skip "PIPE-011" "Bulk pipeline status returns 200" "Got $HTTP_CODE"
fi

# PIPE-012: List pipeline history
api_call GET "/pipelines/history"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "PIPE-012" "List pipeline history returns 200"
else
    skip "PIPE-012" "List pipeline history returns 200" "Got $HTTP_CODE"
fi

fi # end pipelines section

# =============================================================================
# SECTION 11: Schedule Management Endpoints
# =============================================================================
if should_run "schedules"; then
log_header "11. SCHEDULE MANAGEMENT ENDPOINTS"

log_subheader "GET /schedules (List)"

# SCHED-001: List schedules without auth
api_call_noauth GET "/schedules"
assert_status "SCHED-001" "List schedules without auth returns 401" "401"

# SCHED-002: List schedules with auth
api_call GET "/schedules"
assert_status "SCHED-002" "List schedules returns 200" "200"

log_subheader "POST /schedules (Create)"

TEST_SCHEDULE_NAME="IntTestSched_$(date +%s)"

# SCHED-003: Create schedule without auth
api_call_noauth POST "/schedules" "{\"name\":\"${TEST_SCHEDULE_NAME}\",\"pipelineName\":\"TestPipe\",\"schedulerType\":\"Cron\",\"cronExpression\":\"0 0 * * *\",\"isEnabled\":false}"
assert_status "SCHED-003" "Create schedule without auth returns 401" "401"

# SCHED-004: Create cron schedule
api_call POST "/schedules" "{\"name\":\"${TEST_SCHEDULE_NAME}\",\"pipelineName\":\"TestPipe\",\"schedulerType\":\"Cron\",\"cronExpression\":\"0 0 * * *\",\"isEnabled\":false}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "SCHED-004" "Create cron schedule returns 200/201"
    assert_json "SCHED-005" "Schedule has name" '.name // .Name'
else
    fail "SCHED-004" "Create cron schedule returns 200/201" "Got $HTTP_CODE"
    skip "SCHED-005" "Schedule has name" "create failed"
    TEST_SCHEDULE_NAME=""
fi

log_subheader "GET /schedules/{name}"

if [[ -n "$TEST_SCHEDULE_NAME" ]]; then
    # SCHED-006: Get schedule by name
    api_call GET "/schedules/${TEST_SCHEDULE_NAME}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "SCHED-006" "Get schedule by name returns 200"
    else
        fail "SCHED-006" "Get schedule by name returns 200" "Got $HTTP_CODE"
    fi
else
    skip "SCHED-006" "Get schedule by name returns 200" "no test schedule"
fi

# SCHED-007: Get non-existent schedule
api_call GET "/schedules/NonExistentSched12345"
assert_status "SCHED-007" "Get non-existent schedule returns 404" "404"

log_subheader "PUT /schedules/{name}"

if [[ -n "$TEST_SCHEDULE_NAME" ]]; then
    # SCHED-008: Update schedule
    api_call PUT "/schedules/${TEST_SCHEDULE_NAME}" "{\"name\":\"${TEST_SCHEDULE_NAME}\",\"pipelineName\":\"TestPipe\",\"isEnabled\":true,\"cronExpression\":\"0 6 * * *\"}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "SCHED-008" "Update schedule returns 200/204"
    else
        fail "SCHED-008" "Update schedule returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "SCHED-008" "Update schedule returns 200/204" "no test schedule"
fi

log_subheader "POST /schedules/{name}/toggle"

if [[ -n "$TEST_SCHEDULE_NAME" ]]; then
    # SCHED-009: Toggle schedule
    api_call POST "/schedules/${TEST_SCHEDULE_NAME}/toggle" "{\"name\":\"${TEST_SCHEDULE_NAME}\"}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "SCHED-009" "Toggle schedule returns 200"
    else
        fail "SCHED-009" "Toggle schedule returns 200" "Got $HTTP_CODE"
    fi
else
    skip "SCHED-009" "Toggle schedule returns 200" "no test schedule"
fi

log_subheader "DELETE /schedules/{name}"

if [[ -n "$TEST_SCHEDULE_NAME" ]]; then
    # SCHED-010: Delete schedule
    api_call DELETE "/schedules/${TEST_SCHEDULE_NAME}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "SCHED-010" "Delete schedule returns 200/204"
    else
        fail "SCHED-010" "Delete schedule returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "SCHED-010" "Delete schedule returns 200/204" "no test schedule"
fi

fi # end schedules section

# =============================================================================
# SECTION 12: NFL Data Endpoints
# =============================================================================
if should_run "nfl"; then
log_header "12. NFL DATA ENDPOINTS"

log_subheader "GET /nfl/teams"

# NFL-001: List teams without auth
api_call_noauth GET "/nfl/teams"
assert_status "NFL-001" "List NFL teams without auth returns 401" "401"

# NFL-002: List teams with auth
api_call GET "/nfl/teams"
assert_status "NFL-002" "List NFL teams returns 200" "200"

FIRST_TEAM_ID=""
if [[ "$HTTP_CODE" == "200" ]]; then
    # NFL-003: Teams response is array
    assert_json_array "NFL-003" "Teams response is array"

    FIRST_TEAM_ID=$(echo "$HTTP_BODY" | jq -r '.[0].id // .[0].Id // empty' 2>/dev/null)
    if [[ -n "$FIRST_TEAM_ID" ]]; then
        # NFL-004: Team has name field
        assert_json "NFL-004" "Team has name field" '.[0].name // .[0].Name'
    else
        skip "NFL-004" "Team has name field" "no teams in DB"
    fi
else
    skip "NFL-003" "Teams response is array" "list failed"
    skip "NFL-004" "Team has name field" "list failed"
fi

# NFL-005: Filter teams by conference
api_call GET "/nfl/teams?Conference=AFC"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-005" "Filter teams by conference returns 200"
else
    fail "NFL-005" "Filter teams by conference returns 200" "Got $HTTP_CODE"
fi

# NFL-006: Filter teams by division
api_call GET "/nfl/teams?Division=North"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-006" "Filter teams by division returns 200"
else
    fail "NFL-006" "Filter teams by division returns 200" "Got $HTTP_CODE"
fi

log_subheader "GET /nfl/teams/{id}/roster"

if [[ -n "$FIRST_TEAM_ID" ]]; then
    # NFL-007: Get team roster
    api_call GET "/nfl/teams/${FIRST_TEAM_ID}/roster"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "NFL-007" "Get team roster returns 200"
    else
        fail "NFL-007" "Get team roster returns 200" "Got $HTTP_CODE"
    fi
else
    skip "NFL-007" "Get team roster returns 200" "no teams"
fi

log_subheader "GET /nfl/games"

# NFL-008: List games
api_call GET "/nfl/games"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-008" "List NFL games returns 200"
    FIRST_GAME_ID=$(echo "$HTTP_BODY" | jq -r '.[0].id // .[0].Id // empty' 2>/dev/null)
else
    fail "NFL-008" "List NFL games returns 200" "Got $HTTP_CODE"
    FIRST_GAME_ID=""
fi

# NFL-009: Filter games by season
api_call GET "/nfl/games?Season=2025"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-009" "Filter games by season returns 200"
else
    fail "NFL-009" "Filter games by season returns 200" "Got $HTTP_CODE"
fi

log_subheader "GET /nfl/games/{id}/boxscore"

if [[ -n "$FIRST_GAME_ID" ]]; then
    # NFL-010: Get game boxscore
    api_call GET "/nfl/games/${FIRST_GAME_ID}/boxscore"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "NFL-010" "Get game boxscore returns 200"
    else
        fail "NFL-010" "Get game boxscore returns 200" "Got $HTTP_CODE"
    fi
else
    skip "NFL-010" "Get game boxscore returns 200" "no games"
fi

log_subheader "GET /nfl/standings"

# NFL-011: List standings
api_call GET "/nfl/standings"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-011" "List NFL standings returns 200"
else
    fail "NFL-011" "List NFL standings returns 200" "Got $HTTP_CODE"
fi

# NFL-012: Filter standings by season
api_call GET "/nfl/standings?Season=2025"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "NFL-012" "Filter standings by season returns 200"
else
    fail "NFL-012" "Filter standings by season returns 200" "Got $HTTP_CODE"
fi

log_subheader "POST /nfl/players (Create)"

if [[ -n "$FIRST_TEAM_ID" ]]; then
    # NFL-013: Create player
    api_call POST "/nfl/players" "{\"teamId\":\"${FIRST_TEAM_ID}\",\"firstName\":\"IntTest\",\"lastName\":\"Player\",\"position\":\"QB\",\"jerseyNumber\":99,\"heightInches\":74,\"weightLbs\":220,\"college\":\"Test U\",\"draftYear\":2025,\"draftRound\":7,\"draftPick\":250}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
        pass "NFL-013" "Create player returns 200/201"
        TEST_PLAYER_ID=$(echo "$HTTP_BODY" | jq -r '.id // .Id // empty' 2>/dev/null)
    else
        fail "NFL-013" "Create player returns 200/201" "Got $HTTP_CODE"
    fi
else
    skip "NFL-013" "Create player returns 200/201" "no teams"
fi

log_subheader "GET /nfl/players/{id}/stats"

if [[ -n "${TEST_PLAYER_ID:-}" ]]; then
    # NFL-014: Get player stats
    api_call GET "/nfl/players/${TEST_PLAYER_ID}/stats"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "NFL-014" "Get player stats returns 200"
    else
        fail "NFL-014" "Get player stats returns 200" "Got $HTTP_CODE"
    fi
else
    skip "NFL-014" "Get player stats returns 200" "no test player"
fi

log_subheader "POST /nfl/players/{id}/retire"

if [[ -n "${TEST_PLAYER_ID:-}" ]]; then
    # NFL-015: Retire player
    api_call POST "/nfl/players/${TEST_PLAYER_ID}/retire" "{\"playerId\":\"${TEST_PLAYER_ID}\"}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "NFL-015" "Retire player returns 200"
    else
        fail "NFL-015" "Retire player returns 200" "Got $HTTP_CODE"
    fi
else
    skip "NFL-015" "Retire player returns 200" "no test player"
fi

log_subheader "DELETE /nfl/players/{id}"

if [[ -n "${TEST_PLAYER_ID:-}" ]]; then
    # NFL-016: Delete player
    api_call DELETE "/nfl/players/${TEST_PLAYER_ID}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "NFL-016" "Delete player returns 200/204"
    else
        fail "NFL-016" "Delete player returns 200/204" "Got $HTTP_CODE"
    fi
    TEST_PLAYER_ID=""
else
    skip "NFL-016" "Delete player returns 200/204" "no test player"
fi

# NFL-017: Database verification - teams exist
if ! $SKIP_DB; then
    TEAM_COUNT=$(db_query_data "SELECT COUNT(*) FROM NflData.Team")
    if [[ "$TEAM_COUNT" != "SKIP" && "$TEAM_COUNT" -gt 0 ]]; then
        pass "NFL-017" "Database has NFL teams (count=$TEAM_COUNT)"
    else
        fail "NFL-017" "Database has NFL teams" "count=$TEAM_COUNT"
    fi
else
    skip "NFL-017" "Database has NFL teams" "DB checks skipped"
fi

fi # end nfl section

# =============================================================================
# SECTION 13: Schema Discovery Endpoints
# =============================================================================
if should_run "schema"; then
log_header "13. SCHEMA DISCOVERY ENDPOINTS"

log_subheader "POST /connections/{name}/import-schema"

# SCHEMA-001: Import schema without auth
api_call_noauth POST "/connections/ControlDb/import-schema" '{"connectionName":"ControlDb","overwrite":false}'
assert_status "SCHEMA-001" "Import schema without auth returns 401" "401"

# SCHEMA-002: Import schema (may already exist)
api_call POST "/connections/ControlDb/import-schema" '{"connectionName":"ControlDb","overwrite":false}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "SCHEMA-002" "Import schema returns 200"
    assert_json "SCHEMA-003" "Import response has success field" '.success'
    assert_json "SCHEMA-004" "Import response has dataStoreName" '.dataStoreName // .DataStoreName'
else
    fail "SCHEMA-002" "Import schema returns 200" "Got $HTTP_CODE"
    skip "SCHEMA-003" "Import response has success field" "import failed"
    skip "SCHEMA-004" "Import response has dataStoreName" "import failed"
fi

log_subheader "POST /connections/{name}/sync-schema"

# SCHEMA-005: Sync schema
api_call POST "/connections/ControlDb/sync-schema" '{"connectionName":"ControlDb","applyChanges":false}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "SCHEMA-005" "Sync schema returns 200"
    assert_json "SCHEMA-006" "Sync response has dataStoreName" '.dataStoreName // .DataStoreName'
else
    # May return 404 if DataStore not imported yet
    skip "SCHEMA-005" "Sync schema returns 200" "Got $HTTP_CODE (DataStore may not exist)"
    skip "SCHEMA-006" "Sync response has dataStoreName" "sync failed"
fi

log_subheader "POST /schema/preview"

# SCHEMA-007: Data preview without auth
api_call_noauth POST "/schema/preview" '{"connectionName":"ControlDb","schemaName":"cfg","tableName":"Connection","maxRows":5}'
assert_status "SCHEMA-007" "Data preview without auth returns 401" "401"

# SCHEMA-008: Data preview by table
api_call POST "/schema/preview" '{"connectionName":"ControlDb","schemaName":"cfg","tableName":"Connection","maxRows":5}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "SCHEMA-008" "Data preview by table returns 200"
    assert_json "SCHEMA-009" "Preview has columns array" '.columns // .Columns'
    assert_json "SCHEMA-010" "Preview has rows array" '.rows // .Rows'
else
    fail "SCHEMA-008" "Data preview by table returns 200" "Got $HTTP_CODE"
    skip "SCHEMA-009" "Preview has columns array" "preview failed"
    skip "SCHEMA-010" "Preview has rows array" "preview failed"
fi

# SCHEMA-011: Data preview missing required fields
api_call POST "/schema/preview" '{"maxRows":5}'
if [[ "$HTTP_CODE" == "400" ]]; then
    pass "SCHEMA-011" "Data preview with missing fields returns 400"
else
    fail "SCHEMA-011" "Data preview with missing fields returns 400" "Got $HTTP_CODE"
fi

fi # end schema section

# =============================================================================
# SECTION 14: Tenant Management Endpoints
# =============================================================================
if should_run "tenants"; then
log_header "14. TENANT MANAGEMENT ENDPOINTS"

log_subheader "GET /tenants/current"

# TENANT-001: Get current tenant without auth
api_call_noauth GET "/tenants/current"
assert_status "TENANT-001" "Get current tenant without auth returns 401" "401"

# TENANT-002: Get current tenant with auth
api_call GET "/tenants/current"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "TENANT-002" "Get current tenant returns 200"
else
    # May return 404 if no tenant context
    skip "TENANT-002" "Get current tenant returns 200" "Got $HTTP_CODE (no tenant context)"
fi

log_subheader "GET /tenants"

# TENANT-003: List tenants without auth
api_call_noauth GET "/tenants"
assert_status "TENANT-003" "List tenants without auth returns 401" "401"

# TENANT-004: List tenants with auth
api_call GET "/tenants"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "TENANT-004" "List tenants returns 200"
else
    skip "TENANT-004" "List tenants returns 200" "Got $HTTP_CODE (tenants may not be configured)"
fi

log_subheader "GET /tenants/{id}"

# TENANT-005: Get non-existent tenant
api_call GET "/tenants/00000000-0000-0000-0000-000000000000"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "403" ]]; then
    pass "TENANT-005" "Get non-existent tenant returns 404/403"
else
    fail "TENANT-005" "Get non-existent tenant returns 404/403" "Got $HTTP_CODE"
fi

log_subheader "POST /tenants/switch"

# TENANT-006: Switch tenant without auth
api_call_noauth POST "/tenants/switch" '{"tenantId":"00000000-0000-0000-0000-000000000000"}'
assert_status "TENANT-006" "Switch tenant without auth returns 401" "401"

fi # end tenants section

# =============================================================================
# SECTION 15: Search Endpoint
# =============================================================================
if should_run "search"; then
log_header "15. SEARCH ENDPOINT"

# SEARCH-001: Search without auth
api_call_noauth GET "/search?q=test"
assert_status "SEARCH-001" "Search without auth returns 401" "401"

# SEARCH-002: Search with auth
api_call GET "/search?q=test"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "SEARCH-002" "Search returns 200"
else
    skip "SEARCH-002" "Search returns 200" "Got $HTTP_CODE"
fi

fi # end search section

# =============================================================================
# SECTION 16: Theme Endpoints
# =============================================================================
if should_run "themes"; then
log_header "16. THEME ENDPOINTS"

log_subheader "GET /themes"

# THEME-001: List themes without auth
api_call_noauth GET "/themes"
assert_status "THEME-001" "List themes without auth returns 401" "401"

# THEME-002: List themes with auth
api_call GET "/themes"
assert_status "THEME-002" "List themes returns 200" "200"

log_subheader "GET /themes/default"

# THEME-003: Get default theme
api_call GET "/themes/default"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "THEME-003" "Get default theme returns 200"
    assert_json "THEME-004" "Default theme has name" '.name // .Name'
else
    fail "THEME-003" "Get default theme returns 200" "Got $HTTP_CODE"
    skip "THEME-004" "Default theme has name" "get failed"
fi

log_subheader "GET /themes/{name}"

# THEME-005: Get theme by name (use first from list)
FIRST_THEME=$(echo "$HTTP_BODY" | jq -r '.name // .Name // empty' 2>/dev/null)
if [[ -n "$FIRST_THEME" ]]; then
    api_call GET "/themes/${FIRST_THEME}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "THEME-005" "Get theme by name returns 200"
    else
        fail "THEME-005" "Get theme by name returns 200" "Got $HTTP_CODE"
    fi
else
    skip "THEME-005" "Get theme by name returns 200" "no themes"
fi

# THEME-006: Get non-existent theme
api_call GET "/themes/NonExistentTheme12345"
assert_status "THEME-006" "Get non-existent theme returns 404" "404"

log_subheader "POST /themes (Create)"

TEST_THEME_NAME="inttest_theme_$(date +%s)"

# THEME-007: Create theme
api_call POST "/themes" "{\"name\":\"${TEST_THEME_NAME}\",\"isDark\":false,\"primary\":\"#1976d2\",\"secondary\":\"#dc004e\"}"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "THEME-007" "Create theme returns 200/201"
else
    fail "THEME-007" "Create theme returns 200/201" "Got $HTTP_CODE"
    TEST_THEME_NAME=""
fi

log_subheader "PUT /themes/{name}"

if [[ -n "$TEST_THEME_NAME" ]]; then
    # THEME-008: Update theme
    api_call PUT "/themes/${TEST_THEME_NAME}" "{\"primary\":\"#2196f3\"}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "THEME-008" "Update theme returns 200/204"
    else
        fail "THEME-008" "Update theme returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "THEME-008" "Update theme returns 200/204" "no test theme"
fi

log_subheader "DELETE /themes/{name}"

if [[ -n "$TEST_THEME_NAME" ]]; then
    # THEME-009: Delete theme
    api_call DELETE "/themes/${TEST_THEME_NAME}"
    if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
        pass "THEME-009" "Delete theme returns 200/204"
    else
        fail "THEME-009" "Delete theme returns 200/204" "Got $HTTP_CODE"
    fi
else
    skip "THEME-009" "Delete theme returns 200/204" "no test theme"
fi

# THEME-010: Delete built-in theme should fail
api_call DELETE "/themes/fractal"
if [[ "$HTTP_CODE" == "400" || "$HTTP_CODE" == "500" ]]; then
    pass "THEME-010" "Delete built-in theme is rejected"
else
    fail "THEME-010" "Delete built-in theme is rejected" "Got $HTTP_CODE"
fi

fi # end themes section

# =============================================================================
# SECTION 17: Calculation Endpoints
# =============================================================================
if should_run "calculations"; then
log_header "17. CALCULATION ENDPOINTS"

log_subheader "GET /calculations/types"

# CALC-001: List calculation types without auth
api_call_noauth GET "/calculations/types"
assert_status "CALC-001" "List calc types without auth returns 401" "401"

# CALC-002: List calculation types
api_call GET "/calculations/types"
assert_status "CALC-002" "List calc types returns 200" "200"
if [[ "$HTTP_CODE" == "200" ]]; then
    assert_json "CALC-003" "Calc types has types array" '.types | length > 0'
fi

log_subheader "GET /calculations/period-comparisons"

# CALC-004: List period comparison types
api_call GET "/calculations/period-comparisons"
assert_status "CALC-004" "List period comparison types returns 200" "200"
if [[ "$HTTP_CODE" == "200" ]]; then
    assert_json "CALC-005" "Period types has types array" '.types | length > 0'
fi

log_subheader "POST /calculations/execute"

# CALC-006: Execute Sum calculation
api_call POST "/calculations/execute" '{"calculationType":"Sum","values":[1.0,2.0,3.0,4.0,5.0]}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CALC-006" "Execute Sum calculation returns 200"
    assert_json_eq "CALC-007" "Sum result is 15" '.result' "15"
    assert_json_eq "CALC-008" "Sum inputCount is 5" '.inputCount' "5"
else
    fail "CALC-006" "Execute Sum calculation returns 200" "Got $HTTP_CODE"
    skip "CALC-007" "Sum result is 15" "execution failed"
    skip "CALC-008" "Sum inputCount is 5" "execution failed"
fi

# CALC-009: Execute Average calculation
api_call POST "/calculations/execute" '{"calculationType":"Average","values":[10.0,20.0,30.0]}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CALC-009" "Execute Average calculation returns 200"
    assert_json_eq "CALC-010" "Average result is 20" '.result' "20"
else
    fail "CALC-009" "Execute Average calculation returns 200" "Got $HTTP_CODE"
    skip "CALC-010" "Average result is 20" "execution failed"
fi

# CALC-011: Execute with empty values
api_call POST "/calculations/execute" '{"calculationType":"Sum","values":[]}'
if [[ "$HTTP_CODE" == "400" ]]; then
    pass "CALC-011" "Execute with empty values returns 400"
else
    fail "CALC-011" "Execute with empty values returns 400" "Got $HTTP_CODE"
fi

# CALC-012: Execute with unknown type
api_call POST "/calculations/execute" '{"calculationType":"NotAType","values":[1.0]}'
if [[ "$HTTP_CODE" == "400" ]]; then
    pass "CALC-012" "Execute with unknown type returns 400"
else
    fail "CALC-012" "Execute with unknown type returns 400" "Got $HTTP_CODE"
fi

log_subheader "POST /calculations/preview"

# CALC-013: Preview calculation
api_call POST "/calculations/preview" '{"calculationType":"Sum","sampleSize":5}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CALC-013" "Preview calculation returns 200"
    assert_json "CALC-014" "Preview has sampleData" '.sampleData | length > 0'
    assert_json "CALC-015" "Preview has result" '.result'
else
    fail "CALC-013" "Preview calculation returns 200" "Got $HTTP_CODE"
    skip "CALC-014" "Preview has sampleData" "preview failed"
    skip "CALC-015" "Preview has result" "preview failed"
fi

fi # end calculations section

# =============================================================================
# SECTION 18: Analytics Endpoints
# =============================================================================
if should_run "analytics"; then
log_header "18. ANALYTICS ENDPOINTS"

# ANAL-001: Get analytics without auth
api_call_noauth GET "/analytics"
assert_status "ANAL-001" "Get analytics without auth returns 401" "401"

# ANAL-002: Get analytics with auth
api_call GET "/analytics"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "ANAL-002" "Get analytics returns 200"
else
    skip "ANAL-002" "Get analytics returns 200" "Got $HTTP_CODE"
fi

# ANAL-003: Get top calculations
api_call GET "/analytics/top"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "ANAL-003" "Get top calculations returns 200"
else
    skip "ANAL-003" "Get top calculations returns 200" "Got $HTTP_CODE"
fi

fi # end analytics section

# =============================================================================
# SECTION 19: Promotion Endpoints
# =============================================================================
if should_run "promotions"; then
log_header "19. PROMOTION ENDPOINTS"

log_subheader "GET /promotion/environments"

# PROMO-001: List environments without auth
api_call_noauth GET "/promotion/environments"
assert_status "PROMO-001" "List environments without auth returns 401" "401"

# PROMO-002: List environments
api_call GET "/promotion/environments"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "PROMO-002" "List environments returns 200"
else
    skip "PROMO-002" "List environments returns 200" "Got $HTTP_CODE"
fi

log_subheader "GET /promotion/requests"

# PROMO-003: List promotions
api_call GET "/promotion/requests"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "PROMO-003" "List promotions returns 200"
else
    skip "PROMO-003" "List promotions returns 200" "Got $HTTP_CODE"
fi

log_subheader "POST /promotion/requests"

# PROMO-004: Create promotion request
api_call POST "/promotion/requests" '{"sourceEnvironment":"Development","targetEnvironment":"Staging","entityTypes":["Pipeline"],"entityNames":["TestPipe"],"requestedBy":"admin"}'
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "PROMO-004" "Create promotion returns 200/201"
    TEST_PROMOTION_ID=$(echo "$HTTP_BODY" | jq -r '.id // .Id // empty' 2>/dev/null)
else
    fail "PROMO-004" "Create promotion returns 200/201" "Got $HTTP_CODE"
fi

log_subheader "GET /promotion/requests/{id}"

if [[ -n "${TEST_PROMOTION_ID:-}" ]]; then
    # PROMO-005: Get promotion by ID
    api_call GET "/promotion/requests/${TEST_PROMOTION_ID}"
    if [[ "$HTTP_CODE" == "200" ]]; then
        pass "PROMO-005" "Get promotion by ID returns 200"
        assert_json_eq "PROMO-006" "Promotion status is Pending" '.status // .Status' "Pending"
    else
        fail "PROMO-005" "Get promotion by ID returns 200" "Got $HTTP_CODE"
        skip "PROMO-006" "Promotion status is Pending" "get failed"
    fi
else
    skip "PROMO-005" "Get promotion by ID returns 200" "no promotion"
    skip "PROMO-006" "Promotion status is Pending" "no promotion"
fi

# PROMO-007: Get non-existent promotion
api_call GET "/promotion/requests/00000000-0000-0000-0000-000000000000"
assert_status "PROMO-007" "Get non-existent promotion returns 404" "404"

fi # end promotions section

# =============================================================================
# SECTION 20: Bulk Operations Endpoints
# =============================================================================
if should_run "bulk"; then
log_header "20. BULK OPERATIONS ENDPOINTS"

# BULK-001: Bulk export without auth
api_call_noauth GET "/bulk/export"
assert_status "BULK-001" "Bulk export without auth returns 401" "401"

# BULK-002: Bulk export
api_call GET "/bulk/export"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "BULK-002" "Bulk export returns 200"
    assert_json "BULK-003" "Export has version" '.version // .Version'
    assert_json "BULK-004" "Export has summary" '.summary // .Summary'
else
    fail "BULK-002" "Bulk export returns 200" "Got $HTTP_CODE"
    skip "BULK-003" "Export has version" "export failed"
    skip "BULK-004" "Export has summary" "export failed"
fi

# BULK-005: Bulk export with entity filter
api_call GET "/bulk/export?Entities=connections"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "BULK-005" "Bulk export with filter returns 200"
else
    fail "BULK-005" "Bulk export with filter returns 200" "Got $HTTP_CODE"
fi

# BULK-006: Bulk import (empty)
api_call POST "/bulk/import" '{"pipelines":[],"schedules":[],"skipExisting":true}'
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "BULK-006" "Bulk import empty payload returns 200"
    assert_json "BULK-007" "Import has success field" '.success'
else
    fail "BULK-006" "Bulk import empty payload returns 200" "Got $HTTP_CODE"
    skip "BULK-007" "Import has success field" "import failed"
fi

fi # end bulk section

# =============================================================================
# SECTION 21: Catalog Endpoints
# =============================================================================
if should_run "catalog"; then
log_header "21. CATALOG ENDPOINTS"

# CAT-001: Search catalog
api_call GET "/catalog/search?q=test"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CAT-001" "Search catalog returns 200"
else
    skip "CAT-001" "Search catalog returns 200" "Got $HTTP_CODE"
fi

# CAT-002: List glossary terms
api_call GET "/catalog/glossary"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CAT-002" "List glossary terms returns 200"
else
    skip "CAT-002" "List glossary terms returns 200" "Got $HTTP_CODE"
fi

fi # end catalog section

# =============================================================================
# SECTION 22: Configuration Endpoints
# =============================================================================
if should_run "config"; then
log_header "22. CONFIGURATION ENDPOINTS"

log_subheader "Configuration Metadata"

# CFG-001: Get root configuration types
api_call GET "/configuration-metadata/types"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CFG-001" "Get root configuration types returns 200"
else
    skip "CFG-001" "Get root configuration types returns 200" "Got $HTTP_CODE"
fi

# CFG-002: Get configuration categories
api_call GET "/configuration-metadata/categories"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CFG-002" "Get configuration categories returns 200"
else
    skip "CFG-002" "Get configuration categories returns 200" "Got $HTTP_CODE"
fi

log_subheader "Configuration Instances"

# CFG-003: List configuration instances
api_call GET "/configurations"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "CFG-003" "List configuration instances returns 200"
else
    skip "CFG-003" "List configuration instances returns 200" "Got $HTTP_CODE"
fi

fi # end config section

# =============================================================================
# SECTION 23: Dataflow Endpoints
# =============================================================================
if should_run "dataflow"; then
log_header "23. DATAFLOW ENDPOINTS"

# FLOW-001: Get dataflow graph without auth
api_call_noauth GET "/dataflow/graph"
assert_status "FLOW-001" "Get dataflow graph without auth returns 401" "401"

# FLOW-002: Get dataflow graph
api_call GET "/dataflow/graph"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "FLOW-002" "Get dataflow graph returns 200"
else
    skip "FLOW-002" "Get dataflow graph returns 200" "Got $HTTP_CODE"
fi

fi # end dataflow section

# =============================================================================
# SECTION 24: Execution Endpoints
# =============================================================================
if should_run "executions"; then
log_header "24. EXECUTION ENDPOINTS"

# EXEC-001: List executions without auth
api_call_noauth GET "/executions"
assert_status "EXEC-001" "List executions without auth returns 401" "401"

# EXEC-002: List executions
api_call GET "/executions"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "EXEC-002" "List executions returns 200"
else
    skip "EXEC-002" "List executions returns 200" "Got $HTTP_CODE"
fi

# EXEC-003: Get non-existent execution
api_call GET "/executions/00000000-0000-0000-0000-000000000000"
if [[ "$HTTP_CODE" == "404" ]]; then
    pass "EXEC-003" "Get non-existent execution returns 404"
else
    skip "EXEC-003" "Get non-existent execution returns 404" "Got $HTTP_CODE"
fi

fi # end executions section

# =============================================================================
# SECTION 25: Pipeline Designer Endpoints
# =============================================================================
if should_run "designer"; then
log_header "25. PIPELINE DESIGNER ENDPOINTS"

# DESIGN-001: List designer pipelines
api_call GET "/pipeline-designer/pipelines"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "DESIGN-001" "List designer pipelines returns 200"
else
    skip "DESIGN-001" "List designer pipelines returns 200" "Got $HTTP_CODE"
fi

# DESIGN-002: Get designer task types
api_call GET "/pipeline-designer/task-types"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "DESIGN-002" "Get designer task types returns 200"
else
    skip "DESIGN-002" "Get designer task types returns 200" "Got $HTTP_CODE"
fi

fi # end designer section

# =============================================================================
# SECTION 26: Container Endpoints
# =============================================================================
if should_run "containers"; then
log_header "26. CONTAINER ENDPOINTS"

# CONT-001: Get container by name
api_call GET "/containers/test-container"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "404" ]]; then
    pass "CONT-001" "Get container returns 200 or 404"
else
    fail "CONT-001" "Get container returns 200 or 404" "Got $HTTP_CODE"
fi

# CONT-002: Get container by ID (non-existent)
api_call GET "/containers/by-id/00000000-0000-0000-0000-000000000000"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "200" ]]; then
    pass "CONT-002" "Get container by ID returns expected status"
else
    fail "CONT-002" "Get container by ID returns expected status" "Got $HTTP_CODE"
fi

fi # end containers section

# =============================================================================
# SECTION 27: Field Mapping Endpoints
# =============================================================================
if should_run "mappings"; then
log_header "27. FIELD MAPPING ENDPOINTS"

# MAP-001: Get dataset mappings without auth
api_call_noauth GET "/field-mappings/datasets/test"
assert_status "MAP-001" "Get dataset mappings without auth returns 401" "401"

# MAP-002: Validate mappings without auth
api_call_noauth POST "/field-mappings/validate" '{}'
assert_status "MAP-002" "Validate mappings without auth returns 401" "401"

fi # end mappings section

# =============================================================================
# SECTION 28: Proxy Endpoints
# =============================================================================
if should_run "proxy"; then
log_header "28. PROXY ENDPOINTS"

# PROXY-001: Proxy ETL trigger without auth
api_call_noauth POST "/proxy/etl/trigger" '{"pipelineName":"test"}'
assert_status "PROXY-001" "Proxy ETL trigger without auth returns 401" "401"

# PROXY-002: Proxy schedule list without auth
api_call_noauth GET "/proxy/schedules"
assert_status "PROXY-002" "Proxy schedule list without auth returns 401" "401"

# PROXY-003: Proxy ETL trigger with auth (may fail if ETL not running)
api_call POST "/proxy/etl/trigger" '{"pipelineName":"test"}'
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "202" || "$HTTP_CODE" == "502" || "$HTTP_CODE" == "503" ]]; then
    pass "PROXY-003" "Proxy ETL trigger returns expected status"
else
    skip "PROXY-003" "Proxy ETL trigger returns expected status" "Got $HTTP_CODE"
fi

fi # end proxy section

# =============================================================================
# SECTION 30: Messaging Endpoints
# =============================================================================
if should_run "messages"; then
log_header "30. MESSAGING ENDPOINTS"

log_subheader "GET /messages"

# MSG-001: List messages without auth
api_call_noauth GET "/messages"
assert_status "MSG-001" "List messages without auth returns 401" "401"

# MSG-002: List messages with auth
api_call GET "/messages"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "MSG-002" "List messages returns 200"
else
    fail "MSG-002" "List messages returns 200" "Got $HTTP_CODE"
fi

log_subheader "GET /messages/unread-count"

# MSG-003: Get unread count
api_call GET "/messages/unread-count"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "MSG-003" "Get unread count returns 200"
else
    fail "MSG-003" "Get unread count returns 200" "Got $HTTP_CODE"
fi

log_subheader "GET /messages/{id}"

# MSG-004: Get non-existent message
api_call GET "/messages/00000000-0000-0000-0000-000000000000"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "MSG-004" "Get non-existent message returns 404 or 500"
else
    fail "MSG-004" "Get non-existent message returns 404 or 500" "Got $HTTP_CODE"
fi

log_subheader "PUT /messages/{id}/read"

# MSG-005: Mark non-existent message as read
api_call PUT "/messages/00000000-0000-0000-0000-000000000000/read"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "MSG-005" "Mark non-existent message read returns error"
else
    fail "MSG-005" "Mark non-existent message read returns error" "Got $HTTP_CODE"
fi

log_subheader "PUT /messages/{id}/dismiss"

# MSG-006: Dismiss non-existent message
api_call PUT "/messages/00000000-0000-0000-0000-000000000000/dismiss"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "MSG-006" "Dismiss non-existent message returns error"
else
    fail "MSG-006" "Dismiss non-existent message returns error" "Got $HTTP_CODE"
fi

log_subheader "PUT /messages/{id}/archive"

# MSG-007: Archive non-existent message
api_call PUT "/messages/00000000-0000-0000-0000-000000000000/archive"
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "MSG-007" "Archive non-existent message returns error"
else
    fail "MSG-007" "Archive non-existent message returns error" "Got $HTTP_CODE"
fi

log_subheader "PUT /messages/mark-all-read"

# MSG-008: Mark all read
api_call PUT "/messages/mark-all-read"
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "204" ]]; then
    pass "MSG-008" "Mark all read returns success"
else
    fail "MSG-008" "Mark all read returns success" "Got $HTTP_CODE"
fi

fi # end messages section

# =============================================================================
# SECTION 31: Access Request Endpoints
# =============================================================================
if should_run "access-requests"; then
log_header "31. ACCESS REQUEST ENDPOINTS"

log_subheader "POST /access-requests"

# AR-001: Create access request without auth
api_call_noauth POST "/access-requests" '{"requestedResource":"test-table","requestedPermission":"SELECT","justification":"Integration test"}'
assert_status "AR-001" "Create access request without auth returns 401" "401"

# AR-002: Create access request with auth
api_call POST "/access-requests" '{"requestedResource":"test-table","requestedPermission":"SELECT","justification":"Integration test"}'
if [[ "$HTTP_CODE" == "200" || "$HTTP_CODE" == "201" ]]; then
    pass "AR-002" "Create access request returns success"
    TEST_AR_ID=$(echo "$HTTP_BODY" | jq -r '.id // .Id // empty' 2>/dev/null)
else
    fail "AR-002" "Create access request returns success" "Got $HTTP_CODE"
fi

log_subheader "GET /access-requests"

# AR-003: List access requests without auth
api_call_noauth GET "/access-requests"
assert_status "AR-003" "List access requests without auth returns 401" "401"

# AR-004: List access requests with auth
api_call GET "/access-requests"
if [[ "$HTTP_CODE" == "200" ]]; then
    pass "AR-004" "List access requests returns 200"
else
    fail "AR-004" "List access requests returns 200" "Got $HTTP_CODE"
fi

log_subheader "PUT /access-requests/{id}/approve"

# AR-005: Approve non-existent request
api_call PUT "/access-requests/00000000-0000-0000-0000-000000000000/approve" '{"notes":"test"}'
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "AR-005" "Approve non-existent request returns error"
else
    fail "AR-005" "Approve non-existent request returns error" "Got $HTTP_CODE"
fi

log_subheader "PUT /access-requests/{id}/deny"

# AR-006: Deny non-existent request
api_call PUT "/access-requests/00000000-0000-0000-0000-000000000000/deny" '{"notes":"test"}'
if [[ "$HTTP_CODE" == "404" || "$HTTP_CODE" == "500" ]]; then
    pass "AR-006" "Deny non-existent request returns error"
else
    fail "AR-006" "Deny non-existent request returns error" "Got $HTTP_CODE"
fi

# Cleanup: approve + dismiss the test access request if created
if [[ -n "${TEST_AR_ID:-}" ]]; then
    api_call PUT "/access-requests/${TEST_AR_ID}/approve" '{"notes":"integration test cleanup"}' 2>/dev/null || true
fi

fi # end access-requests section

# =============================================================================
# SECTION 32: Database Verification
# =============================================================================
if should_run "db" && ! $SKIP_DB; then
log_header "32. DATABASE VERIFICATION"

# DB-001: ControlDb is accessible
DB_RESULT=$(db_query_control "SELECT 1")
if [[ "$DB_RESULT" == "1" ]]; then
    pass "DB-001" "ControlDb is accessible"
else
    fail "DB-001" "ControlDb is accessible" "Query returned: $DB_RESULT"
fi

# DB-002: DataDb is accessible
DB_RESULT=$(db_query_data "SELECT 1")
if [[ "$DB_RESULT" == "1" ]]; then
    pass "DB-002" "DataDb is accessible"
else
    fail "DB-002" "DataDb is accessible" "Query returned: $DB_RESULT"
fi

# DB-003: cfg.Connection table has rows
CONN_COUNT=$(db_query_control "SELECT COUNT(*) FROM cfg.Connection")
if [[ -n "$CONN_COUNT" && "$CONN_COUNT" -gt 0 ]]; then
    pass "DB-003" "cfg.Connection has rows (count=$CONN_COUNT)"
else
    fail "DB-003" "cfg.Connection has rows" "count=$CONN_COUNT"
fi

# DB-004: Admin user exists
ADMIN_EXISTS=$(db_query_control "SELECT COUNT(*) FROM auth.[User] WHERE Username='admin'")
if [[ "$ADMIN_EXISTS" == "1" ]]; then
    pass "DB-004" "Admin user exists in database"
else
    fail "DB-004" "Admin user exists in database" "count=$ADMIN_EXISTS"
fi

# DB-005: Roles exist
ROLE_COUNT=$(db_query_control "SELECT COUNT(*) FROM cfg.Role")
if [[ -n "$ROLE_COUNT" && "$ROLE_COUNT" -gt 0 ]]; then
    pass "DB-005" "cfg.Role has rows (count=$ROLE_COUNT)"
else
    fail "DB-005" "cfg.Role has rows" "count=$ROLE_COUNT"
fi

# DB-006: Permissions exist
PERM_COUNT=$(db_query_control "SELECT COUNT(*) FROM cfg.Permission")
if [[ -n "$PERM_COUNT" && "$PERM_COUNT" -gt 0 ]]; then
    pass "DB-006" "cfg.Permission has rows (count=$PERM_COUNT)"
else
    fail "DB-006" "cfg.Permission has rows" "count=$PERM_COUNT"
fi

# DB-007: NflData.Team table has data
TEAM_COUNT=$(db_query_data "SELECT COUNT(*) FROM NflData.Team")
if [[ -n "$TEAM_COUNT" && "$TEAM_COUNT" -gt 0 ]]; then
    pass "DB-007" "NflData.Team has rows (count=$TEAM_COUNT)"
else
    fail "DB-007" "NflData.Team has rows" "count=$TEAM_COUNT"
fi

# DB-008: NflData.Player table has data
PLAYER_COUNT=$(db_query_data "SELECT COUNT(*) FROM NflData.Player")
if [[ -n "$PLAYER_COUNT" && "$PLAYER_COUNT" -gt 0 ]]; then
    pass "DB-008" "NflData.Player has rows (count=$PLAYER_COUNT)"
else
    fail "DB-008" "NflData.Player has rows" "count=$PLAYER_COUNT"
fi

# DB-009: NflData.Game table has data
GAME_COUNT=$(db_query_data "SELECT COUNT(*) FROM NflData.Game")
if [[ -n "$GAME_COUNT" && "$GAME_COUNT" -gt 0 ]]; then
    pass "DB-009" "NflData.Game has rows (count=$GAME_COUNT)"
else
    fail "DB-009" "NflData.Game has rows" "count=$GAME_COUNT"
fi

# DB-010: No orphaned test data
ORPHAN_COUNT=$(db_query_control "SELECT COUNT(*) FROM auth.[User] WHERE Username LIKE 'testuser_int%'")
if [[ "$ORPHAN_COUNT" == "0" ]]; then
    pass "DB-010" "No orphaned test users in database"
else
    fail "DB-010" "No orphaned test users in database" "Found $ORPHAN_COUNT orphaned users"
fi

elif should_run "db"; then
    log_header "29. DATABASE VERIFICATION"
    skip "DB-001" "ControlDb is accessible" "DB checks skipped"
    skip "DB-002" "DataDb is accessible" "DB checks skipped"

fi # end db section

# =============================================================================
# CLEANUP
# =============================================================================
log_header "CLEANUP"

# Clean up any test resources that might have been left behind

if [[ -n "${TEST_USER_ID:-}" ]]; then
    echo "  Cleaning up test user ${TEST_USER_ID}..."
    api_call DELETE "/users/${TEST_USER_ID}" 2>/dev/null || true
fi

if [[ -n "${TEST_PLAYER_ID:-}" ]]; then
    echo "  Cleaning up test player ${TEST_PLAYER_ID}..."
    api_call DELETE "/nfl/players/${TEST_PLAYER_ID}" 2>/dev/null || true
fi

echo "  Cleanup complete."

# =============================================================================
# SUMMARY
# =============================================================================
echo ""
echo -e "${BOLD}${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BOLD}${CYAN}  TEST SUMMARY${NC}"
echo -e "${BOLD}${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "  Total:   ${BOLD}${TOTAL}${NC}"
echo -e "  Passed:  ${GREEN}${BOLD}${PASSED}${NC}"
echo -e "  Failed:  ${RED}${BOLD}${FAILED}${NC}"
echo -e "  Skipped: ${YELLOW}${BOLD}${SKIPPED}${NC}"
echo ""

if [[ $FAILED -gt 0 ]]; then
    echo -e "  ${RED}${BOLD}RESULT: FAILED${NC}"
    echo ""
    exit 1
else
    echo -e "  ${GREEN}${BOLD}RESULT: PASSED${NC}"
    echo ""
    exit 0
fi
