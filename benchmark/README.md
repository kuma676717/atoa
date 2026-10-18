# Benchmark Scripts

このディレクトリには、Cloud Migration Fortune API PoC の機能テストとパフォーマンス計測スクリプトが含まれています。

## 前提条件

- `curl` — HTTP リクエスト送信
- `python3` — JSON 処理・レポート生成
- `jq` — (オプション) レスポンス確認用
- `xargs` — 並列スループットテスト用

## 環境変数

| 変数 | デフォルト | 説明 |
|------|-----------|------|
| `BASE_URL` | スクリプト依存 | テスト対象 API のベース URL |

## スクリプト一覧

### `run-azure.sh` — Azure Functions 機能テスト

```bash
# ローカル実行 (func start)
./run-azure.sh

# デプロイ済み環境
BASE_URL=https://my-func.azurewebsites.net ./run-azure.sh
```

デフォルト: `http://localhost:7071`

### `run-aws-manual.sh` — AWS Lambda Manual 機能テスト

```bash
# ローカル実行 (sam local start-api)
./run-aws-manual.sh

# デプロイ済み環境
BASE_URL=https://xxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod ./run-aws-manual.sh
```

デフォルト: `http://localhost:3000`

### `run-aws-transform.sh` — AWS Transform Lambda 機能テスト

```bash
# Phase 6 デプロイ後に BASE_URL を設定
BASE_URL=https://xxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod ./run-aws-transform.sh
```

デフォルト: `http://localhost:3001`

### `measure.sh` — パフォーマンス計測

```bash
./measure.sh <ENV> <BASE_URL>
# ENV: azure | aws-manual | aws-transform
```

例:

```bash
# Azure (デプロイ済み)
./measure.sh azure https://my-func.azurewebsites.net

# AWS Manual
./measure.sh aws-manual https://xxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod

# AWS Transform
./measure.sh aws-transform https://xxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod
```

**計測内容:**

1. **レイテンシフェーズ**: ケース 1–6 × 10 リクエスト = 60 サンプル → p50/p95 (ms)
2. **スループットフェーズ**: 300 リクエスト × 並列 10 → req/s

**出力ファイル** (`../results/`):
- `{ENV}-raw.json` — 全レイテンシサンプル
- `{ENV}-summary.json` — p50, p95, throughput

### `compare.sh` — 比較レポート生成

```bash
# 全環境の measure.sh 実行後
./compare.sh
```

**出力ファイル** (`../results/`):
- `comparison.csv` — メトリクス比較表
- `report.md` — 詳細 Markdown レポート

## テストケース (`test-cases.json`)

| ID | ラベル | メソッド | 期待ステータス | 説明 |
|----|--------|---------|--------------|------|
| 1 | legacy-erp | POST | 200 | Java/Oracle ERP (15年) |
| 2 | monolith-api | POST | 200 | Ruby on Rails (8年) |
| 3 | cobol-batch | POST | 200 | COBOL/DB2 (25年) — 大凶候補 |
| 4 | dotnet-webapi | POST | 200 | .NET/SQL Server (6年) |
| 5 | micro-service | POST | 200 | Go/K8s (1年) |
| 6 | serverless-func | POST | 200 | C#/.NET 8 (2年) — Transform最適 |
| 7 | validation-empty-workload | POST | 400 | バリデーション: 空のワークロード名 |
| 8 | validation-missing-description | POST | 400 | バリデーション: description欠如 |
| 9 | validation-negative-years | POST | 400 | バリデーション: 負の稼働年数 |
| 10 | edge-zero-years | POST | 200 | エッジ: 0年稼働 |
| 11 | large-tech-stack | POST | 200 | エッジ: 10技術スタック |
| 12 | long-workload-name | POST | 200 | エッジ: 100文字ワークロード名 |
| 13 | missing-assessment | GET | 404 | GET: 存在しない診断結果 |

## 出力ファイルの解釈

| ファイル | 意味 |
|---------|------|
| `*-raw.json` | 生レイテンシデータ (秒・ミリ秒) |
| `*-summary.json` | p50/p95/スループット集計 |
| `comparison.csv` | 全環境横断比較。`[MEASURED]`=実測値, `PLACEHOLDER`=未計測 |
| `report.md` | 6R分析・考察・推奨事項を含む総合レポート |
