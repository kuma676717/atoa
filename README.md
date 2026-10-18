# Cloud Migration Fortune API (クラウド移行診断おみくじAPI) PoC

[![Tag](https://img.shields.io/badge/tag-project%3Datoa--poc-blue)](.)

## 目的

**AWS Transform Azure-to-AWS GA 機能の Level 300 技術 LT 向け検証 PoC**

Azure Functions (.NET 8 isolated) + Azure Blob Storage を AWS Lambda + Amazon S3 へ移行し、  
**AI 変換方式 (AWS Transform)** と **手動移行方式** を定量的に比較します。

## Fortune API コンセプト

> *「移行すべきか、否か — それをクラウドに問う」*

クラウド移行診断おみくじ API は、あなたのワークロード情報を送ると「大吉 / 吉 / 中吉 / 小吉 / 末吉 / 凶 / 大凶」の移行運勢と、  
6R 戦略の推奨 (Re-host / Re-platform / Re-factor / Re-architect / Re-purchase / Retire) を返します。

**自己言及的ユーモア**: COBOL/DB2 (25年稼働) を送ると「大凶: レガシーの呪い」が返る可能性が高い。  
移行ツール自身が「移行しないほうがいい」と診断する、というジョークが組み込まれています。

## ディレクトリ構成

| ディレクトリ | 内容 |
|-------------|------|
| `azure-app/` | Azure Functions (.NET 8 isolated) ソース + Bicep IaC |
| `azure-app/src/` | C# ソースコード (Functions, Models, Services) |
| `azure-app/infra/bicep/` | Bicep テンプレート (main, storage, functions, params) |
| `aws-lambda-manual/` | 手動移植 AWS Lambda (.NET 8) ソース + SAM テンプレート |
| `aws-lambda-manual/src/` | C# ソースコード |
| `aws-lambda-manual/template.yaml` | AWS SAM テンプレート |
| `aws-lambda-transform/` | AWS Transform 変換出力 (Phase 6 完了後に配置) |
| `aws-transform-output/` | Transform 生成物の raw 保存領域 (人手修正前) |
| `benchmark/` | テストケース・計測・比較スクリプト |
| `results/` | 計測結果 JSON/CSV/MD (git 追跡対象) |
| `.agents/tasks/` | PoC 計画アーティファクト |

## 前提条件

| ツール | バージョン | 確認コマンド |
|-------|-----------|-------------|
| .NET SDK | 8.x | `dotnet --version` |
| Azure CLI | 2.50+ | `az --version` |
| Azure Functions Core Tools | v4 | `func --version` |
| AWS CLI | 2.x | `aws --version` |
| AWS SAM CLI | 1.x | `sam --version` |
| curl | 任意 | `curl --version` |
| python3 | 3.8+ | `python3 --version` |

## セットアップ手順

### 1. ローカル開発 (Azure Functions)

```bash
cd azure-app/src
cp local.settings.json.example local.settings.json  # AzureWebJobsStorage を設定
func start
```

### 2. ローカル開発 (AWS Lambda Manual)

```bash
cd aws-lambda-manual
sam build
sam local start-api --port 3000
```

### 3. Azure デプロイ

```bash
# Bicep でリソースグループとリソースを作成
az group create --name rg-atoa-poc --location japaneast
az deployment group create \
  --resource-group rg-atoa-poc \
  --template-file azure-app/infra/bicep/main.bicep \
  --parameters azure-app/infra/bicep/parameters/poc.bicepparam
```

### 4. AWS デプロイ (手動移行)

```bash
cd aws-lambda-manual
sam build
sam deploy --guided
```

### 5. ベンチマーク実行

```bash
# 機能テスト
BASE_URL=https://my-func.azurewebsites.net ./benchmark/run-azure.sh
BASE_URL=https://xxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod ./benchmark/run-aws-manual.sh

# パフォーマンス計測
./benchmark/measure.sh azure https://my-func.azurewebsites.net
./benchmark/measure.sh aws-manual https://xxxxxx.execute-api.ap-northeast-1.amazonaws.com/Prod

# 比較レポート生成
./benchmark/compare.sh
```

## 計測メトリクス

| メトリクス | 種別 | 説明 |
|-----------|------|------|
| auto_conversion_rate | 実測 | AWS Transform の自動変換率 (%) — Phase 6 後に計測 |
| manual_effort_lines | 推定 | 人手修正行数 (diff から算出) |
| functional_compatibility | 実測 | test-cases.json 13 ケースのパス率 (%) |
| p50_latency_ms | 実測 | p50 レイテンシ (ms) — measure.sh で計測 |
| p95_latency_ms | 実測 | p95 レイテンシ (ms) — measure.sh で計測 |
| throughput_rps | 実測 | スループット (req/s) — 300 req × 並列 10 |
| operational_fitness | 推定 | 運用適合性スコア (1–5) — 定性評価 |
| security_score | 推定 | セキュリティスコア — チェックリスト評価 |
| total_migration_effort_h | 推定 | 移行総工数 (人時) |

> **実測値** = `[MEASURED]` タグ付き。**推定値** = `estimated` / `PLACEHOLDER` 表記。

## クリーンアップ

```bash
# Azure リソースグループを削除
./cleanup-azure.sh rg-atoa-poc

# AWS スタックを削除 (S3 バケットも空にしてから削除)
./cleanup-aws.sh atoa-fortune-manual atoa-fortune-transform

# 両環境まとめてクリーンアップ
./cleanup-all.sh rg-atoa-poc atoa-fortune-manual atoa-fortune-transform
```

## ライセンス / タグ

- **PoC タグ**: `project=atoa-poc`
- すべてのクラウドリソース (Azure / AWS) に上記タグを付与
- 認証情報はソースコードに含まれません (環境変数 `AzureWebJobsStorage` / `S3_BUCKET_NAME` を使用)
