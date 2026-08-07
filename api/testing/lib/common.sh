#!/usr/bin/env bash
# lib/common.sh — E2E test framework: colors, counters, HTTP helpers, results output

# ── Configuration ──
BASE_URL="${API_BASE_URL:-http://localhost:5000/api/v1}"
TMPDIR="${TMPDIR:-/tmp}"
E2E_STATUS="$TMPDIR/e2e_status"
E2E_BODY="$TMPDIR/e2e_body"
E2E_HEADERS="$TMPDIR/e2e_headers"

# ── Results Directory ──
RESULTS_DIR="${RESULTS_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/results/$(date +%Y%m%d-%H%M%S)}"
CURRENT_DOMAIN=""
CURRENT_ENDPOINT=""
CURRENT_METHOD=""

_init_results() {
    mkdir -p "$RESULTS_DIR"
    echo "{\"started\":\"$(date -Iseconds)\",\"baseUrl\":\"$BASE_URL\"}" > "$RESULTS_DIR/run.json"
}

# ── Colors ──
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# ── Counters ──
PASS_COUNT=0
FAIL_COUNT=0
SKIP_COUNT=0
TESTS_RUN=0
FAILURES=()
CURRENT_TEST=""

# ── Results Writer ──
_write_result() {
    local status="$1"  # pass, fail, skip
    local test_name="$2"
    local detail="${3:-}"
    local http_status http_body

    http_status=$(status_code 2>/dev/null || echo "")
    http_body=$(response_body 2>/dev/null | head -c 2000 || echo "")

    # Build path: results/<METHOD>/<endpoint-path>/<status>.<test-name>.json
    # e.g. results/GET/users/me/pass.admin-returns-200.json
    local safe_test endpoint_path result_dir
    safe_test=$(echo "$test_name" | tr ' /' '_-' | tr -cd 'a-zA-Z0-9_-' | head -c 120)

    if [[ -n "$CURRENT_METHOD" && -n "$CURRENT_ENDPOINT" ]]; then
        # Strip leading slash, use endpoint path as directory structure
        endpoint_path="${CURRENT_METHOD}${CURRENT_ENDPOINT}"
        # Remove query string for folder path
        endpoint_path="${endpoint_path%%\?*}"
    else
        # Fallback to domain-based path
        endpoint_path=$(echo "$CURRENT_DOMAIN" | tr ' /' '_-' | tr -cd 'a-zA-Z0-9_-')
    fi

    result_dir="$RESULTS_DIR/$endpoint_path"
    mkdir -p "$result_dir"

    # Write JSON result
    jq -n \
        --arg status "$status" \
        --arg test "$test_name" \
        --arg domain "$CURRENT_DOMAIN" \
        --arg endpoint "$CURRENT_ENDPOINT" \
        --arg method "$CURRENT_METHOD" \
        --arg detail "$detail" \
        --arg httpStatus "$http_status" \
        --arg body "$http_body" \
        --arg ts "$(date -Iseconds)" \
        '{status:$status,test:$test,domain:$domain,method:$method,endpoint:$endpoint,detail:$detail,httpStatus:$httpStatus,body:$body,timestamp:$ts}' \
        > "$result_dir/${status}.${safe_test}.json" 2>/dev/null || true
}

# ── Output Helpers ──
describe() {
    CURRENT_DOMAIN="$1"
    echo ""
    echo -e "${BOLD}${BLUE}━━━ $1 ━━━${NC}"
}

it() {
    CURRENT_TEST="$1"
    TESTS_RUN=$((TESTS_RUN + 1))
    echo -n -e "  ${CYAN}TEST${NC} $1 ... "
}

pass() {
    local msg="${1:-$CURRENT_TEST}"
    PASS_COUNT=$((PASS_COUNT + 1))
    echo -e "${GREEN}PASS${NC}"
    _write_result "pass" "$CURRENT_TEST"
}

fail() {
    local msg="${1:-$CURRENT_TEST}"
    FAIL_COUNT=$((FAIL_COUNT + 1))
    FAILURES+=("$msg")
    echo -e "${RED}FAIL${NC} — $msg"
    _write_result "fail" "$CURRENT_TEST" "$msg"
}

skip() {
    local msg="${1:-$CURRENT_TEST}"
    SKIP_COUNT=$((SKIP_COUNT + 1))
    echo -e "${YELLOW}SKIP${NC} — $msg"
    _write_result "skip" "$CURRENT_TEST" "$msg"
}

# ── HTTP Helpers ──
_curl_common() {
    local method="$1"
    local url="$2"
    local body="$3"
    local token="$4"

    local -a curl_args=(
        -s -S
        -w '%{http_code}'
        -o "$E2E_BODY"
        -D "$E2E_HEADERS"
        -X "$method"
    )

    if [[ -n "$token" ]]; then
        curl_args+=(-H "Authorization: Bearer $token")
    fi

    if [[ -n "$body" ]]; then
        curl_args+=(-H "Content-Type: application/json" -d "$body")
    fi

    # Track method + endpoint for results folder structure
    CURRENT_METHOD="$method"
    CURRENT_ENDPOINT="$url"

    local status
    status=$(curl "${curl_args[@]}" "${BASE_URL}${url}" 2>/dev/null) || true
    echo "$status" > "$E2E_STATUS"
}

http_get() {
    local url="$1"
    local token="${2:-}"
    _curl_common "GET" "$url" "" "$token"
}

http_post() {
    local url="$1"
    local body="${2:-}"
    local token="${3:-}"
    _curl_common "POST" "$url" "$body" "$token"
}

http_put() {
    local url="$1"
    local body="${2:-}"
    local token="${3:-}"
    _curl_common "PUT" "$url" "$body" "$token"
}

http_patch() {
    local url="$1"
    local body="${2:-}"
    local token="${3:-}"
    _curl_common "PATCH" "$url" "$body" "$token"
}

http_delete() {
    local url="$1"
    local token="${2:-}"
    _curl_common "DELETE" "$url" "" "$token"
}

status_code() {
    cat "$E2E_STATUS" 2>/dev/null || echo ""
}

response_body() {
    cat "$E2E_BODY" 2>/dev/null || echo ""
}

response_headers() {
    cat "$E2E_HEADERS" 2>/dev/null || echo ""
}

# ── Cleanup Helper ──
cleanup_entity() {
    local method="$1"
    local url="$2"
    local token="$3"
    _curl_common "$method" "$url" "" "$token" 2>/dev/null || true
}

# ── Pre-flight Check ──
preflight_check() {
    echo -e "${BOLD}Pre-flight check: ${BASE_URL}/health${NC}"
    local status
    status=$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/health" 2>/dev/null) || true
    if [[ "$status" != "200" ]]; then
        echo -e "${RED}FATAL: API not reachable at ${BASE_URL} (status: ${status})${NC}"
        echo "Start the API first."
        exit 1
    fi
    echo -e "${GREEN}API is reachable.${NC}"
}

# ── Summary ──
summary() {
    echo ""
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BOLD}  Test Summary${NC}"
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "  Total:   ${TESTS_RUN}"
    echo -e "  ${GREEN}Passed:  ${PASS_COUNT}${NC}"
    echo -e "  ${RED}Failed:  ${FAIL_COUNT}${NC}"
    echo -e "  ${YELLOW}Skipped: ${SKIP_COUNT}${NC}"
    echo -e "  Results: ${RESULTS_DIR}"
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    # Per-endpoint pass/fail rates
    echo ""
    echo -e "${BOLD}  Endpoint Results:${NC}"
    printf "  ${CYAN}%-8s %-35s %5s %5s %5s %7s${NC}\n" "METHOD" "ENDPOINT" "PASS" "FAIL" "SKIP" "RATE"

    local endpoints_json="[]"
    # Scan result dirs for JSON files, aggregate by method+endpoint
    if [[ -d "$RESULTS_DIR" ]]; then
        local -A ep_pass ep_fail ep_skip
        while IFS= read -r -d '' result_file; do
            local r_method r_endpoint r_status
            r_method=$(jq -r '.method // ""' "$result_file" 2>/dev/null) || continue
            r_endpoint=$(jq -r '.endpoint // ""' "$result_file" 2>/dev/null) || continue
            r_status=$(jq -r '.status // ""' "$result_file" 2>/dev/null) || continue
            [[ -z "$r_method" || -z "$r_endpoint" ]] && continue
            local key="${r_method} ${r_endpoint}"
            case "$r_status" in
                pass) ep_pass["$key"]=$(( ${ep_pass["$key"]:-0} + 1 )) ;;
                fail) ep_fail["$key"]=$(( ${ep_fail["$key"]:-0} + 1 )) ;;
                skip) ep_skip["$key"]=$(( ${ep_skip["$key"]:-0} + 1 )) ;;
            esac
        done < <(find "$RESULTS_DIR" -name '*.json' ! -name 'run.json' ! -name 'summary.json' -print0 2>/dev/null)

        # Sort and print
        local sorted_keys
        sorted_keys=$(printf '%s\n' "${!ep_pass[@]}" "${!ep_fail[@]}" "${!ep_skip[@]}" | sort -u)
        while IFS= read -r key; do
            [[ -z "$key" ]] && continue
            local m="${key%% *}"
            local ep="${key#* }"
            local p=${ep_pass["$key"]:-0}
            local f=${ep_fail["$key"]:-0}
            local s=${ep_skip["$key"]:-0}
            local total_ep=$((p + f + s))
            local rate="—"
            if [[ $total_ep -gt 0 ]]; then
                rate="$(( p * 100 / total_ep ))%"
            fi
            local color="$GREEN"
            [[ $f -gt 0 ]] && color="$RED"
            [[ $total_ep -eq $s ]] && color="$YELLOW"
            printf "  ${color}%-8s %-35s %5d %5d %5d %7s${NC}\n" "$m" "$ep" "$p" "$f" "$s" "$rate"

            # Build JSON array entry
            endpoints_json=$(echo "$endpoints_json" | jq \
                --arg method "$m" --arg endpoint "$ep" \
                --argjson pass "$p" --argjson fail "$f" --argjson skip "$s" \
                --arg rate "$rate" \
                '. += [{"method":$method,"endpoint":$endpoint,"pass":$pass,"fail":$fail,"skip":$skip,"rate":$rate}]') || true
        done <<< "$sorted_keys"
    fi

    if [[ ${#FAILURES[@]} -gt 0 ]]; then
        echo ""
        echo -e "${RED}${BOLD}  Failures:${NC}"
        for f in "${FAILURES[@]}"; do
            echo -e "    ${RED}x${NC} $f"
        done
    fi
    echo ""

    # Write summary JSON with endpoint breakdown
    jq -n \
        --argjson total "$TESTS_RUN" \
        --argjson passed "$PASS_COUNT" \
        --argjson failed "$FAIL_COUNT" \
        --argjson skipped "$SKIP_COUNT" \
        --arg completed "$(date -Iseconds)" \
        --argjson endpoints "$endpoints_json" \
        '{total:$total,passed:$passed,failed:$failed,skipped:$skipped,completed:$completed,endpoints:$endpoints}' \
        > "$RESULTS_DIR/summary.json" 2>/dev/null || true
}
