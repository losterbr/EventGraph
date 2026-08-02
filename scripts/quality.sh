#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "Running quality checks..."

dotnet test EventGraph.Tests/EventGraph.Tests.csproj

dotnet build EventGraph/EventGraph.csproj

echo "Quality checks completed successfully."
