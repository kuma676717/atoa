#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
RESULTS_DIR=$(cd "$SCRIPT_DIR/../results" && pwd)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

echo "=== Cloud Migration Fortune API — Comparison Report Generator ==="
echo "Results dir: $RESULTS_DIR"
echo ""

# Read summary values using python3
read_metric() {
  local env="$1"
  local metric="$2"
  local default="$3"
  local summary_file="$RESULTS_DIR/${env}-summary.json"

  if [ -f "$summary_file" ]; then
    python3 - <<PYEOF
import json
try:
    with open("$summary_file") as f:
        d = json.load(f)
    val = d.get("$metric")
    print(val if val is not None else "$default")
except Exception:
    print("$default")
PYEOF
  else
    echo "$default"
  fi
}

# Read metrics for each environment
AZURE_P50=$(read_metric "azure" "p50_ms" "PLACEHOLDER")
AZURE_P95=$(read_metric "azure" "p95_ms" "PLACEHOLDER")
AZURE_RPS=$(read_metric "azure" "throughput_rps" "PLACEHOLDER")

TRANSFORM_P50=$(read_metric "aws-transform" "p50_ms" "PLACEHOLDER")
TRANSFORM_P95=$(read_metric "aws-transform" "p95_ms" "PLACEHOLDER")
TRANSFORM_RPS=$(read_metric "aws-transform" "throughput_rps" "PLACEHOLDER")

MANUAL_P50=$(read_metric "aws-manual" "p50_ms" "PLACEHOLDER")
MANUAL_P95=$(read_metric "aws-manual" "p95_ms" "PLACEHOLDER")
MANUAL_RPS=$(read_metric "aws-manual" "throughput_rps" "PLACEHOLDER")

# Tag values as MEASURED or PLACEHOLDER
tag_value() {
  local val="$1"
  local prefix="${2:-}"
  if [ "$val" = "PLACEHOLDER" ]; then
    echo "PLACEHOLDER"
  else
    echo "${prefix}${val}[MEASURED]"
  fi
}

AZURE_P50_TAG=$(tag_value "$AZURE_P50")
AZURE_P95_TAG=$(tag_value "$AZURE_P95")
AZURE_RPS_TAG=$(tag_value "$AZURE_RPS")
TRANSFORM_P50_TAG=$(tag_value "$TRANSFORM_P50")
TRANSFORM_P95_TAG=$(tag_value "$TRANSFORM_P95")
TRANSFORM_RPS_TAG=$(tag_value "$TRANSFORM_RPS")
MANUAL_P50_TAG=$(tag_value "$MANUAL_P50")
MANUAL_P95_TAG=$(tag_value "$MANUAL_P95")
MANUAL_RPS_TAG=$(tag_value "$MANUAL_RPS")

# Write comparison.csv
CSV_FILE="$RESULTS_DIR/comparison.csv"
cat > "$CSV_FILE" <<CSVEOF
metric,azure,aws-transform,aws-manual,notes
auto_conversion_rate,N/A,PLACEHOLDER,N/A,Measured after Phase 6 (AWS Transform)
manual_effort_lines,N/A,PLACEHOLDER,PLACEHOLDER,Estimated from diff
functional_compatibility,PLACEHOLDER,PLACEHOLDER,PLACEHOLDER,Pass rate on test-cases.json
p50_latency_ms,${AZURE_P50_TAG},${TRANSFORM_P50_TAG},${MANUAL_P50_TAG},Measured
p95_latency_ms,${AZURE_P95_TAG},${TRANSFORM_P95_TAG},${MANUAL_P95_TAG},Measured
throughput_rps,${AZURE_RPS_TAG},${TRANSFORM_RPS_TAG},${MANUAL_RPS_TAG},Measured
operational_fitness,estimated,estimated,estimated,Qualitative 1-5 scale
security_score,estimated,estimated,estimated,Based on checklist
total_migration_effort_h,N/A,PLACEHOLDER,PLACEHOLDER,Estimated hours
CSVEOF

echo "  Wrote: $CSV_FILE"

# Write report.md using python3 for clean formatting
REPORT_FILE="$RESULTS_DIR/report.md"
python3 - <<PYEOF
import csv, io

timestamp = "$TIMESTAMP"
csv_content = open("$CSV_FILE").read()
reader = csv.DictReader(io.StringIO(csv_content))
rows = list(reader)

# Build markdown table
header = "| metric | azure | aws-transform | aws-manual | notes |"
separator = "|--------|-------|---------------|------------|-------|"
table_rows = [header, separator]
for row in rows:
    table_rows.append(
        f"| {row['metric']} | {row['azure']} | {row['aws-transform']} | {row['aws-manual']} | {row['notes']} |"
    )
table = "\n".join(table_rows)

report = f"""# Cloud Migration Fortune API — Comparison Report

> クラウド移行診断おみくじAPI: Azure Functions vs AWS Lambda Migration PoC

**Generated:** {timestamp}  
**Note:** Values marked `[MEASURED]` are actual benchmark results. `PLACEHOLDER` values require deployment (Phase 6) or manual measurement.

---

## 計測結果サマリー

{table}

---

## Discussion

### 6R Framework 分析

| ワークロード | 推奨 6R 戦略 | 根拠 |
|-------------|-------------|------|
| legacy-erp (Java/Oracle, 15年) | Re-platform | DBレイヤーがボトルネック。Lift-and-shiftは困難だがリアーキテクチャは過剰 |
| monolith-api (Ruby/Rails, 8年) | Re-factor | Railsの規約がLambdaと相性良。段階的移行が現実的 |
| cobol-batch (COBOL/DB2, 25年) | Retire / Replace | 25年稼働のCOBOLはAWS Mainframe Modernization対象。直接Lambda移行は非推奨 |
| dotnet-webapi (.NET, 6年) | Re-platform | .NET 8 Lambda対応済み。AWS Transform最適ターゲット |
| micro-service (Go/K8s, 1年) | Re-host (ECS/EKS) | Kubernetesワークロードはそのままコンテナ移行が効率的 |
| serverless-func (C#/.NET 8, 2年) | Re-factor | **AWS Transformの最適ターゲット**。最高の自動変換率が期待される |

### 🎯 伝説の COBOL おみくじ

COBOL/DB2 ワークロード（25年稼働）に対して、FortuneEngine は「大凶: レガシーの呪い」を返す可能性が最も高い。
これは自己言及的なユーモアである — 移行ツール自身が「移行しないほうがいい」と診断するケース。

### Latency Analysis

- Azure Functions と AWS Lambda の p50 レイテンシはコールドスタートに大きく依存する
- `.NET 8 isolated` (Azure) と `.NET 8 Lambda` (AWS) はどちらも `ReadyToRun` 最適化が有効
- p95 が p50 の 3 倍以上の場合、コールドスタートが p95 を引き上げている可能性が高い

### AWS Transform 評価

- **自動変換率**: `HttpTrigger` → `APIGatewayHttpApiV2ProxyRequest` の変換精度が主要指標
- **要人手修正箇所**: `IEnumerable<string>` ヘッダーアクセス → `Headers` 辞書、DI パターンの差異
- **機能互換率**: test-cases.json の 13 ケースに対するパス率で定量評価

### Recommendation

1. **AWS Transform 活用**: `.NET 8 serverless` ワークロードは自動変換率 70%+ が期待される
2. **手動移行推奨ケース**: COBOL、Java ERP は Transform 非対応。専門チームによる Re-platform が現実的
3. **PoC 結論**: Transform は C#/.NET Lambda 移行の初期スキャフォールディングとして有効

---

*Tags: project=atoa-poc*  
*Measured vs Estimated: `[MEASURED]` = 実測値, `PLACEHOLDER` / `estimated` = 推定値*
"""

with open("$REPORT_FILE", "w") as f:
    f.write(report)
print(f"  Wrote: $REPORT_FILE")
PYEOF

echo ""
echo "=== Comparison report complete ==="
echo "  CSV:    $CSV_FILE"
echo "  Report: $REPORT_FILE"
