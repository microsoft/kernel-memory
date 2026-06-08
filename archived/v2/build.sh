#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $ROOT

echo "======================================="
echo "  Building Kernel Memory Solution"
echo "======================================="
echo ""

# Clean previous build artifacts
echo "🔨 Cleaning previous build artifacts..."
dotnet clean --nologo --verbosity quiet
echo "✅ Clean complete"
echo ""

# Restore dependencies
echo "🔨 Restoring dependencies..."
dotnet restore --nologo
echo "✅ Restore complete"
echo ""

# Build solution with strict settings
echo "🔨 Building solution..."
echo ""

# Build with:
# - TreatWarningsAsErrors: Fail on any warnings (compliance requirement)
# - EnforceCodeStyleInBuild: Enforce code style during build
# - NoWarn: Empty (don't suppress any warnings)
dotnet build \
  --no-restore \
  --configuration Release \
  /p:TreatWarningsAsErrors=true \
  /p:EnforceCodeStyleInBuild=true \
  /warnaserror

BUILD_RESULT=$?

echo ""

if [ $BUILD_RESULT -eq 0 ]; then
  echo "======================================="
  echo "✅ Build Successful"
  echo "======================================="
  echo ""
  echo "All projects built successfully with zero warnings."
  exit 0
else
  echo "======================================="
  echo "❌ Build Failed"
  echo "======================================="
  echo ""
  echo "Build failed with errors or warnings."
  echo "Review the output above for details."
  echo ""
  echo "Reminder: This project has zero-tolerance for warnings."
  exit 1
fi
