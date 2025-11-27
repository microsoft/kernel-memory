#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $ROOT

# Minimum coverage threshold - can be overridden via first argument
# Default: 80% (as specified in AGENTS.md)
MIN_COVERAGE=${1:-80}

echo "Running tests with coverage collection..."
echo ""

# Clean previous results
rm -rf ./TestResults

# Run tests with coverage using coverlet.collector
# --collect:"XPlat Code Coverage" enables the collector
# --results-directory specifies output location
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

echo ""
echo "Coverage collection complete!"
echo ""

# Find all coverage files
COVERAGE_FILES=$(find ./TestResults -name "coverage.cobertura.xml" | sort)
FILE_COUNT=$(echo "$COVERAGE_FILES" | wc -l | tr -d ' ')

echo "Found $FILE_COUNT coverage report(s)"
echo ""

# Parse and display coverage for each project
declare -a COVERAGE_RATES
declare -a COVERAGE_SOURCES

while IFS= read -r COVERAGE_FILE; do
  if [ -f "$COVERAGE_FILE" ]; then
    # Extract source path and line rate
    SOURCE=$(grep -o '<source>.*</source>' "$COVERAGE_FILE" | head -1 | sed 's/<source>//;s/<\/source>//')
    LINE_RATE=$(grep -o 'line-rate="[0-9.]*"' "$COVERAGE_FILE" | head -1 | grep -o '[0-9.]*')
    
    if [ -n "$LINE_RATE" ]; then
      COVERAGE_PCT=$(awk "BEGIN {printf \"%.2f\", $LINE_RATE * 100}")
      COVERAGE_RATES+=("$COVERAGE_PCT")
      COVERAGE_SOURCES+=("$SOURCE")
      
      # Determine project name from source path
      if [[ "$SOURCE" == *"/Core/"* ]] || [[ "$SOURCE" == *"/Core" ]]; then
        PROJECT_NAME="Core"
      elif [[ "$SOURCE" == *"/Main/"* ]] || [[ "$SOURCE" == *"/Main" ]]; then
        PROJECT_NAME="Main (combined)"
      else
        PROJECT_NAME="Combined"
      fi
      
      echo "  $PROJECT_NAME: ${COVERAGE_PCT}%"
    fi
  fi
done <<< "$COVERAGE_FILES"

echo ""

# Calculate overall coverage (weighted average or use the worst case)
# For now, we'll use the minimum coverage across all projects
MIN_PROJECT_COVERAGE=""
for rate in "${COVERAGE_RATES[@]}"; do
  if [ -z "$MIN_PROJECT_COVERAGE" ] || (( $(awk "BEGIN {print ($rate < $MIN_PROJECT_COVERAGE) ? 1 : 0}") )); then
    MIN_PROJECT_COVERAGE="$rate"
  fi
done

if [ -n "$MIN_PROJECT_COVERAGE" ]; then
  echo "====================================="
  echo "  Minimum Coverage: ${MIN_PROJECT_COVERAGE}%"
  echo "  Threshold:        ${MIN_COVERAGE}%"
  echo "====================================="
  echo ""
  
  # Check if coverage meets threshold
  MEETS_THRESHOLD=$(awk "BEGIN {print ($MIN_PROJECT_COVERAGE >= $MIN_COVERAGE) ? 1 : 0}")
  
  if [ "$MEETS_THRESHOLD" -eq 0 ]; then
    echo "❌ Coverage ${MIN_PROJECT_COVERAGE}% is below minimum threshold of ${MIN_COVERAGE}%"
    echo ""
    echo "Coverage by project:"
    for i in "${!COVERAGE_RATES[@]}"; do
      echo "  - ${COVERAGE_SOURCES[$i]}: ${COVERAGE_RATES[$i]}%"
    done
    exit 1
  else
    echo "✅ All projects meet minimum threshold"
    rm -rf TestResults
  fi
else
  echo "⚠️  Could not parse coverage from reports"
  exit 1
fi
