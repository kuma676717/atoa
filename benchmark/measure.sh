#!/bin/bash
set -euo pipefail

# Usage: ./measure.sh <ENV> <BASE_URL>
# ENV = azure | aws-manual | aws-transform

if [ $# -lt 2 ]; then
  echo "Usage: $0 <ENV> <BASE_URL>"
  echo "  ENV: azure | aws-manual | aws-transform"
  echo "  BASE_URL: e.g. https://myapp.azurewebsites.net"
  exit 1
fi

ENV="$1"
BASE_URL="$2"

case "$ENV" in
  azure|aws-manual|aws-transform) ;;
  *)
    echo "ERROR: ENV must be azure, aws-manual, or aws-transform"
    exit 1
    ;;
esac

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
RESULTS_DIR=$(cd "$SCRIPT_DIR/../results" && pwd)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

echo "=== Latency Benchmark: $ENV ==="
echo "BASE_URL: $BASE_URL"
echo "Results dir: $RESULTS_DIR"
echo ""

# Test bodies for cases 1–6 (sequential latency phase)
BODIES=(
  '{"workloadName":"legacy-erp","description":"Legacy ERP system","techStack":["Java","Oracle"],"yearsInProduction":15}'
  '{"workloadName":"monolith-api","description":"Ruby on Rails monolith","techStack":["Ruby","PostgreSQL"],"yearsInProduction":8}'
  '{"workloadName":"cobol-batch","description":"COBOL batch processing system","techStack":["COBOL","DB2"],"yearsInProduction":25}'
  '{"workloadName":"dotnet-webapi","description":".NET Web API backend","techStack":[".NET","SQL Server"],"yearsInProduction":6}'
  '{"workloadName":"micro-service","description":"Go microservice on Kubernetes","techStack":["Go","PostgreSQL","Kubernetes"],"yearsInProduction":1}'
  '{"workloadName":"serverless-func","description":"C# serverless function","techStack":["C#",".NET 8"],"yearsInProduction":2}'
)
LABELS=(legacy-erp monolith-api cobol-batch dotnet-webapi micro-service serverless-func)

LATENCIES=()

echo "--- Sequential latency phase (10 requests per case) ---"
for i in "${!BODIES[@]}"; do
  BODY="${BODIES[$i]}"
  LABEL="${LABELS[$i]}"
  echo "  Running case: $LABEL"
  for j in $(seq 1 10); do
    T=$(curl -s -o /dev/null -w "%{time_total}" \
      -X POST \
      -H "Content-Type: application/json" \
      -d "$BODY" \
      "$BASE_URL/api/assess" 2>/dev/null || echo "0")
    LATENCIES+=("$T")
  done
done

TOTAL_LATENCY_REQUESTS=${#LATENCIES[@]}
echo "  Collected $TOTAL_LATENCY_REQUESTS latency samples"

# Compute p50/p95 using awk
P50_P95=$(printf '%s\n' "${LATENCIES[@]}" | awk '
BEGIN { n = 0 }
{ a[++n] = $1 + 0 }
END {
  # simple insertion sort
  for (i = 2; i <= n; i++) {
    key = a[i]
    j = i - 1
    while (j >= 1 && a[j] > key) {
      a[j+1] = a[j]
      j--
    }
    a[j+1] = key
  }
  p50_idx = int(n * 0.50)
  p95_idx = int(n * 0.95)
  if (p50_idx < 1) p50_idx = 1
  if (p95_idx < 1) p95_idx = 1
  printf "%.6f %.6f\n", a[p50_idx], a[p95_idx]
}')

P50_SEC=$(echo "$P50_P95" | awk '{print $1}')
P95_SEC=$(echo "$P50_P95" | awk '{print $2}')

# Convert seconds to milliseconds
P50_MS=$(echo "$P50_SEC" | awk '{printf "%.2f", $1 * 1000}')
P95_MS=$(echo "$P95_SEC" | awk '{printf "%.2f", $1 * 1000}')

echo "  p50: ${P50_MS} ms"
echo "  p95: ${P95_MS} ms"

# Throughput phase: 300 concurrent requests (10 parallel workers)
echo ""
echo "--- Throughput phase (300 requests, 10 parallel) ---"
THROUGHPUT_BODY='{"workloadName":"throughput-test","description":"Throughput benchmark","yearsInProduction":1}'

START_NS=$(date +%s%N)
seq 300 | xargs -P 10 -I{} curl -s -o /dev/null \
  -X POST \
  -H "Content-Type: application/json" \
  -d "$THROUGHPUT_BODY" \
  "$BASE_URL/api/assess" 2>/dev/null || true
END_NS=$(date +%s%N)

ELAPSED_S=$(echo "$START_NS $END_NS" | awk '{printf "%.3f", ($2 - $1) / 1000000000}')
THROUGHPUT_RPS=$(echo "$ELAPSED_S" | awk '{printf "%.2f", 300 / $1}')
echo "  Elapsed: ${ELAPSED_S}s"
echo "  Throughput: ${THROUGHPUT_RPS} req/s"

# Write raw latencies JSON using python3
RAW_JSON="$RESULTS_DIR/${ENV}-raw.json"
python3 - <<PYEOF
import json, os

latencies_str = """${LATENCIES[*]}"""
latencies = [float(x) for x in latencies_str.split() if x]

data = {
    "env": "$ENV",
    "base_url": "$BASE_URL",
    "timestamp": "$TIMESTAMP",
    "latencies_sec": latencies,
    "latencies_ms": [round(x * 1000, 3) for x in latencies],
    "total_samples": len(latencies)
}

with open("$RAW_JSON", "w") as f:
    json.dump(data, f, indent=2)
print(f"  Wrote raw latencies: $RAW_JSON")
PYEOF

# Write summary JSON using python3
SUMMARY_JSON="$RESULTS_DIR/${ENV}-summary.json"
python3 - <<PYEOF
import json

data = {
    "env": "$ENV",
    "base_url": "$BASE_URL",
    "timestamp": "$TIMESTAMP",
    "p50_ms": float("$P50_MS"),
    "p95_ms": float("$P95_MS"),
    "throughput_rps": float("$THROUGHPUT_RPS"),
    "total_latency_requests": int("$TOTAL_LATENCY_REQUESTS"),
    "throughput_requests": 300,
    "throughput_elapsed_s": float("$ELAPSED_S"),
    "measured": True
}

with open("$SUMMARY_JSON", "w") as f:
    json.dump(data, f, indent=2)
print(f"  Wrote summary: $SUMMARY_JSON")
PYEOF

echo ""
echo "=== Done: $ENV benchmark complete ==="
echo "  p50: ${P50_MS} ms  |  p95: ${P95_MS} ms  |  throughput: ${THROUGHPUT_RPS} req/s"
