#!/bin/bash
set -euo pipefail

BASE_URL=${BASE_URL:-http://localhost:7071}
SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
TEST_CASES="$SCRIPT_DIR/test-cases.json"

echo "=== Azure Functions Test Runner ==="
echo "BASE_URL: $BASE_URL"
echo ""

export TEST_CASES_PATH="$TEST_CASES"
export BASE_URL_PY="$BASE_URL"

python3 <<'PYEOF'
import json, subprocess, sys, os

test_cases_path = os.environ['TEST_CASES_PATH']
base_url = os.environ['BASE_URL_PY']

with open(test_cases_path) as f:
    cases = json.load(f)

results = []
for case in cases:
    method = case['method']
    endpoint = case.get('endpoint', '/api/assess')
    url = base_url + endpoint
    expected = case['expectedStatus']
    label = case['label']

    if method == 'GET':
        cmd = ['curl', '-s', '-o', '/tmp/resp.json', '-w', '%{http_code}', url]
    else:
        body = json.dumps(case.get('body', {}))
        cmd = ['curl', '-s', '-o', '/tmp/resp.json', '-w', '%{http_code}',
               '-X', 'POST',
               '-H', 'Content-Type: application/json',
               '-d', body,
               url]

    result = subprocess.run(cmd, capture_output=True, text=True)
    actual_str = result.stdout.strip()
    actual_status = int(actual_str) if actual_str.isdigit() else 0

    status_ok = (actual_status == expected)
    status_str = 'PASS' if status_ok else 'FAIL'
    results.append(status_ok)
    print(f"[{status_str}] case{case['id']:02d} {label}: expected={expected} actual={actual_status}")

passed = sum(results)
failed = len(results) - passed
print(f"\nResults: {passed}/{len(results)} passed")
if failed > 0:
    sys.exit(1)
PYEOF
