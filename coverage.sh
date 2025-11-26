#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $ROOT

# Minimum coverage threshold - can be overridden via first argument
# Default: 80% (as specified in AGENTS.md)
MIN_COVERAGE=${1:-80}

echo "Running tests with coverage collection..."
echo ""

# Run tests with coverage using coverlet.collector
# --collect:"XPlat Code Coverage" enables the collector
# --results-directory specifies output location
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

echo ""
echo "Coverage collection complete!"
echo ""

# Find the most recent coverage file (coverlet.collector creates it in a GUID subfolder)
COVERAGE_REPORT=$(find ./TestResults -name "coverage.cobertura.xml" | head -1)

echo "Coverage report location: $COVERAGE_REPORT"
echo ""

if [ -f "$COVERAGE_REPORT" ]; then
  # Parse line coverage from cobertura XML
  LINE_RATE=$(grep -o 'line-rate="[0-9.]*"' "$COVERAGE_REPORT" | head -1 | grep -o '[0-9.]*')
  
  if [ -n "$LINE_RATE" ]; then
    # Convert to percentage
    COVERAGE_PCT=$(awk "BEGIN {printf \"%.2f\", $LINE_RATE * 100}")
    
    echo "====================================="
    echo "  Test Coverage: ${COVERAGE_PCT}%"
    echo "  Threshold:     ${MIN_COVERAGE}%"
    echo "====================================="
    echo ""
    
    # Check if coverage meets threshold
    MEETS_THRESHOLD=$(awk "BEGIN {print ($COVERAGE_PCT >= $MIN_COVERAGE) ? 1 : 0}")
    
    if [ "$MEETS_THRESHOLD" -eq 0 ]; then
      echo "❌ Coverage ${COVERAGE_PCT}% is below minimum threshold of ${MIN_COVERAGE}%"
      exit 1
    else
      echo "✅ Coverage meets minimum threshold"
      rm -rf TestResults
    fi
  else
    echo "⚠️  Could not parse coverage percentage from report"
  fi
else
  echo "⚠️  Coverage report not found at: $COVERAGE_REPORT"
fi
