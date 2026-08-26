#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "Running quality checks..."

mkdir -p ./coverage

dotnet test EventGraph.Tests/EventGraph.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage

dotnet build EventGraph/EventGraph.csproj

COVERAGE_FILE=$(find ./coverage -name "coverage.cobertura.xml" -print0 2>/dev/null | xargs -0 ls -t 2>/dev/null | head -n 1)
if [[ -z "$COVERAGE_FILE" ]]; then
  echo "Coverage report was not generated." >&2
  exit 1
fi

COVERAGE=$(python3 - <<'PY' "$COVERAGE_FILE"
import sys
import xml.etree.ElementTree as ET
path = sys.argv[1]
root = ET.parse(path).getroot()
line_rate = float(root.attrib.get('line-rate', '0'))
print(f"{line_rate * 100:.2f}")
PY
)

echo "Line coverage: ${COVERAGE}%"

python3 - "$COVERAGE" <<'PY'
import sys
coverage = float(sys.argv[1])
if coverage < 97.0:
  raise SystemExit("Coverage below 97% threshold.")
PY

echo "Quality checks completed successfully."
