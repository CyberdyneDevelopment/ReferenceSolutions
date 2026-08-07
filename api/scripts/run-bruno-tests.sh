#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# FractalDataWorks Reference API — Bruno E2E Test Runner
# ═══════════════════════════════════════════════════════════════════════════════
#
# Runs the Bruno API test collection against the Reference API.
#
# Prerequisites:
#   npm install -g @usebruno/cli
#
# Usage:
#   ./run-bruno-tests.sh              # Run all tests against local environment
#   ./run-bruno-tests.sh --folder 02  # Run only a specific folder prefix
#
# ═══════════════════════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BRUNO_DIR="${SCRIPT_DIR}/../tests/bruno"

# Colours
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# ─── Verify bru CLI is installed ─────────────────────────────────────────────
if ! command -v bru &>/dev/null; then
    echo -e "${RED}ERROR: 'bru' CLI not found.${NC}"
    echo "Install with: npm install -g @usebruno/cli"
    exit 1
fi

# ─── Verify collection exists ────────────────────────────────────────────────
if [[ ! -f "${BRUNO_DIR}/bruno.json" ]]; then
    echo -e "${RED}ERROR: Bruno collection not found at: ${BRUNO_DIR}${NC}"
    exit 1
fi

# ─── Parse arguments ─────────────────────────────────────────────────────────
FOLDER_FILTER=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --folder)
            FOLDER_FILTER="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [--folder PREFIX]"
            echo ""
            echo "Options:"
            echo "  --folder PREFIX   Run only folders matching PREFIX (e.g., '02' for 02-auth)"
            echo "  -h, --help        Show this help"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown argument: $1${NC}"
            exit 1
            ;;
    esac
done

# ─── Run tests ───────────────────────────────────────────────────────────────
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}FractalDataWorks Reference API — Bruno E2E Tests${NC}"
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo -e "  Collection: ${BRUNO_DIR}"
echo -e "  Environment: local"
echo -e "  Started: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
echo ""

if [[ -n "$FOLDER_FILTER" ]]; then
    echo -e "${YELLOW}Running folder filter: ${FOLDER_FILTER}*${NC}"
    bru run --env local "${BRUNO_DIR}" --folder "${FOLDER_FILTER}"
else
    echo -e "${YELLOW}Running all tests...${NC}"
    bru run --env local "${BRUNO_DIR}"
fi

EXIT_CODE=$?

echo ""
if [[ $EXIT_CODE -eq 0 ]]; then
    echo -e "${GREEN}All Bruno E2E tests passed.${NC}"
else
    echo -e "${RED}Some Bruno E2E tests failed (exit code: ${EXIT_CODE}).${NC}"
fi

exit $EXIT_CODE
