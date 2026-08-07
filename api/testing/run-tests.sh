#!/usr/bin/env bash
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/../docker" && pwd)"
API_PROJECT="$API_DIR/src/Reference.Api/Reference.Api.csproj"
API_PID=""
API_LOG="/tmp/e2e-api-stdout.log"
HEALTH_TIMEOUT=60

# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"
# shellcheck source=lib/assertions.sh
source "$SCRIPT_DIR/lib/assertions.sh"
# shellcheck source=lib/auth.sh
source "$SCRIPT_DIR/lib/auth.sh"

# ── Usage ──
usage() {
    echo "Usage: $0 [--reset-db] [--no-reset] [domain|all]"
    echo ""
    echo "Options:"
    echo "  --reset-db    Drop and recreate database with seed data before tests (default)"
    echo "  --no-reset    Skip database reset"
    echo ""
    echo "Domains: all health auth users roles permissions tenants connections"
    echo "         datastores datasets pipelines schedules catalog quality"
    echo "         promotions themes lineage configuration proxy security advanced nfl"
    exit 1
}

# ── Parse args ──
RESET_DB=true
FILTER="all"

for arg in "$@"; do
    case "$arg" in
        --reset-db)  RESET_DB=true ;;
        --no-reset)  RESET_DB=false ;;
        --help|-h)   usage ;;
        -*)          echo "Unknown option: $arg"; usage ;;
        *)           FILTER="$arg" ;;
    esac
done

# ── Database Reset ──
export SA_PASSWORD="${SA_PASSWORD:-ApiSolution123#}"

reset_database() {
    echo -e "${BOLD}Resetting database...${NC}"

    if [[ ! -f "$DOCKER_DIR/mssql/reset-database.sh" ]]; then
        echo -e "${RED}FATAL: reset-database.sh not found at $DOCKER_DIR/mssql/reset-database.sh${NC}"
        exit 1
    fi

    # Run the existing reset script
    bash "$DOCKER_DIR/mssql/reset-database.sh"
    local rc=$?
    if [[ $rc -ne 0 ]]; then
        echo -e "${RED}FATAL: Database reset failed (exit code $rc)${NC}"
        exit 1
    fi

    # Run supplemental E2E seed data
    if [[ -f "$SCRIPT_DIR/data/e2e-seed.sql" ]]; then
        echo -e "  Applying E2E supplemental seed data..."
        local container_name="fdw-mssql"
        docker cp "$SCRIPT_DIR/data/e2e-seed.sql" "$container_name:/tmp/e2e-seed.sql"
        docker exec -e SQLCMDPASSWORD="${SA_PASSWORD:?SA_PASSWORD required}" "$container_name" \
            /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U sa -C -i /tmp/e2e-seed.sql
        local seed_rc=$?
        if [[ $seed_rc -ne 0 ]]; then
            echo -e "${YELLOW}WARNING: E2E seed data failed (exit code $seed_rc)${NC}"
        fi
    fi

    echo -e "${GREEN}Database reset complete.${NC}"
    echo ""
}

# ── API Lifecycle ──
start_api() {
    echo -e "${BOLD}Building API...${NC}"
    if [[ ! -f "$API_PROJECT" ]]; then
        echo -e "${RED}FATAL: API project not found at $API_PROJECT${NC}"
        exit 1
    fi

    dotnet build "$API_PROJECT" -c Debug --verbosity quiet
    echo -e "${GREEN}Build succeeded.${NC}"

    # Set FDW secret environment variables (passwords match reset-database.sh logins)
    export FDW_SECRET_CONFIG_PASSWORD="${FDW_SECRET_CONFIG_PASSWORD:-FdwConfigPassword#}"
    export FDW_SECRET_AUTH_PASSWORD="${FDW_SECRET_AUTH_PASSWORD:-FdwAuthPassword#}"
    export FDW_SECRET_TENANT_PASSWORD="${FDW_SECRET_TENANT_PASSWORD:-FdwTenantPassword#}"
    export FDW_SECRET_ETL_PASSWORD="${FDW_SECRET_ETL_PASSWORD:-FdwEtlPassword#}"
    export FDW_SECRET_SCHED_PASSWORD="${FDW_SECRET_SCHED_PASSWORD:-FdwSchedPassword#}"
    export FDW_SECRET_OPS_PASSWORD="${FDW_SECRET_OPS_PASSWORD:-FdwOpsPassword#}"
    export FDW_SECRET_NFL_PASSWORD="${FDW_SECRET_NFL_PASSWORD:-FdwNflPassword#}"
    export FDW_SECRET_CONFIG_RO_PASSWORD="${FDW_SECRET_CONFIG_RO_PASSWORD:-FdwConfigRoPassword#}"
    export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

    # Kill any existing process on port 5000
    local existing_pid
    existing_pid=$(lsof -ti:5000 2>/dev/null || true)
    if [[ -n "$existing_pid" ]]; then
        echo -e "${YELLOW}Killing existing process on port 5000 (PID: ${existing_pid})...${NC}"
        kill -9 $existing_pid 2>/dev/null || true
        sleep 1
    fi

    echo -e "${BOLD}Starting API...${NC}"
    dotnet run --project "$API_PROJECT" --no-build -c Debug \
        > "$API_LOG" 2>&1 &
    API_PID=$!
    echo "  API PID: $API_PID"

    # Wait for health endpoint
    echo -e "Waiting for /health (timeout ${HEALTH_TIMEOUT}s)..."
    local elapsed=0
    while (( elapsed < HEALTH_TIMEOUT )); do
        local health_status
        health_status=$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/health" 2>/dev/null) || true
        if [[ "$health_status" == "200" ]]; then
            echo -e "  ${GREEN}API is healthy (${elapsed}s)${NC}"
            echo ""
            return 0
        fi
        sleep 1
        elapsed=$((elapsed + 1))
    done

    echo -e "${RED}FATAL: API did not become healthy within ${HEALTH_TIMEOUT}s${NC}"
    echo "Last 20 lines of API output:"
    tail -20 "$API_LOG" 2>/dev/null || true
    kill "$API_PID" 2>/dev/null || true
    exit 1
}

stop_api() {
    if [[ -n "$API_PID" ]] && kill -0 "$API_PID" 2>/dev/null; then
        echo -e "${YELLOW}Stopping API (PID ${API_PID})...${NC}"
        kill "$API_PID" 2>/dev/null || true
        wait "$API_PID" 2>/dev/null || true
    fi
}
trap stop_api EXIT

# Source all test files (defines functions, doesn't execute)
for test_file in "$SCRIPT_DIR/tests/"*.sh; do
    # shellcheck source=/dev/null
    source "$test_file"
done

# Reset DB if requested
if [[ "$RESET_DB" == "true" ]]; then
    reset_database
fi

# Initialize results directory
_init_results

# Start API and wait for health
start_api

# Authenticate all test users
init_tokens

# Run test suites
run_all_tests() {
    test_health
    test_auth
    test_users
    test_roles
    test_permissions
    test_tenants
    test_connections
    test_datastores
    test_datasets
    test_pipelines
    test_schedules
    test_catalog
    test_quality
    test_promotions
    test_themes
    test_lineage
    test_configuration
    test_proxy
    test_advanced
    test_nfl_data
    test_security_matrix
}

case "$FILTER" in
    all)            run_all_tests ;;
    health)         test_health ;;
    auth)           test_auth ;;
    users)          test_users ;;
    roles)          test_roles ;;
    permissions)    test_permissions ;;
    tenants)        test_tenants ;;
    connections)    test_connections ;;
    datastores)     test_datastores ;;
    datasets)       test_datasets ;;
    pipelines)      test_pipelines ;;
    schedules)      test_schedules ;;
    catalog)        test_catalog ;;
    quality)        test_quality ;;
    promotions)     test_promotions ;;
    themes)         test_themes ;;
    lineage)        test_lineage ;;
    configuration)  test_configuration ;;
    proxy)          test_proxy ;;
    security)       test_security_matrix ;;
    advanced)       test_advanced ;;
    nfl)            test_nfl_data ;;
    *)
        echo "Unknown filter: $FILTER"
        usage
        ;;
esac

summary
exit $([ "$FAIL_COUNT" -eq 0 ] && echo 0 || echo 1)
