#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$ROOT"

echo "======================================="
echo "  Running E2E Tests"
echo "======================================="
echo ""

# Choose build configuration (default Release to align with build.sh)
CONFIGURATION="${CONFIGURATION:-Release}"
KM_BIN="$ROOT/src/Main/bin/$CONFIGURATION/net10.0/KernelMemory.Main.dll"

# Ensure km binary is built at the selected configuration
if [ ! -f "$KM_BIN" ]; then
    echo "km binary not found at $KM_BIN. Building ($CONFIGURATION)..."
    dotnet build src/Main/Main.csproj -c "$CONFIGURATION"
fi

if [ ! -f "$KM_BIN" ]; then
    echo "❌ km binary still not found at $KM_BIN after build. Set KM_BIN to a valid path."
    exit 1
fi

export KM_BIN

FAILED=0
PASSED=0

# Run each test file
for test_file in tests/e2e/test_*.py; do
    if [ -f "$test_file" ]; then
        echo ""
        echo "Running: $(basename "$test_file")"
        echo "---------------------------------------"

        if python3 "$test_file"; then
            PASSED=$((PASSED + 1))
        else
            FAILED=$((FAILED + 1))
        fi
    fi
done

echo ""
echo "======================================="
echo "  E2E Test Results"
echo "======================================="
echo "Passed: $PASSED"
echo "Failed: $FAILED"
echo "======================================="

if [ $FAILED -gt 0 ]; then
    exit 1
fi

exit 0
