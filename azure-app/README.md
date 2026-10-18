# Azure Functions App — Cloud Migration Fortune API

おみくじ感覚でクラウド移行診断を行うREST APIのAzure Functions実装です。  
ワークロード情報を入力すると、移行複雑度・推奨戦略・成功確率・神のお言葉を返します。

---

## Prerequisites

| ツール | バージョン |
|--------|------------|
| Azure CLI | 2.x 以上 (`az --version`) |
| Azure Functions Core Tools | v4 (`func --version`) |
| .NET SDK | 8.0 (`dotnet --version`) |
| Azurite (ローカルBlob Storage エミュレーター) | `npm install -g azurite` |

---

## Local Development

```bash
# 1. Azurite をバックグラウンドで起動
azurite --silent &

# 2. 依存パッケージを復元
dotnet restore src/MigrationFortune.csproj

# 3. ローカル起動
cd src
func start
```

エンドポイント例:
```bash
# 移行アセスメント
curl -X POST http://localhost:7071/api/assess \
  -H "Content-Type: application/json" \
  -d '{"workloadName":"legacy-erp","description":"SAP ECC on-prem","techStack":["ABAP","Oracle"],"yearsInProduction":15}'

# 結果取得
curl http://localhost:7071/api/assessment/{assessmentId}
```

---

## Deploy to Azure

```bash
# 1. ログイン
az login

# 2. リソースグループ作成（japaneast）
az group create \
  --name rg-atoa-poc-dev \
  --location japaneast \
  --tags project=atoa-poc env=dev app=migration-fortune managedBy=bicep

# 3. Bicep デプロイ
az deployment group create \
  --resource-group rg-atoa-poc-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/parameters/poc.bicepparam

# 4. 関数アプリへコードをデプロイ
FUNC_APP=$(az deployment group show \
  --resource-group rg-atoa-poc-dev \
  --name functionsDeployment \
  --query properties.outputs.functionAppName.value -o tsv)

cd src
func azure functionapp publish $FUNC_APP
```

---

## Cost Notes

| リソース | 無料枠 | 備考 |
|----------|--------|------|
| Azure Functions (Consumption) | 月100万リクエスト無料 | PoC規模では無料枠内 |
| Azure Blob Storage | 5 GB/月（初年度） | 超過分 ~$0.02/GB/月 |
| Application Insights | 5 GB/日まで無料 | サンプリング有効済み |

> ⚠️ **ストレージ課金**: Functions の無料枠とは別に、Blob Storage のデータ保存・読み書き転送に微小な料金が発生します。PoC期間中は無視できる金額ですが、長期運用時は注意してください。

---

## Cleanup

Azure リソースを削除するには cleanup スクリプトを使用してください:

```bash
../../cleanup-azure.sh rg-atoa-poc-dev
```

スクリプトはリソース一覧を表示してから削除確認を求めます。
