# Implementation Plan — Cloud Migration Fortune API (atoa PoC)

> **Toolchain:** `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec`; dotnet binary: `/opt/homebrew/opt/dotnet@8/libexec/dotnet`  
> **Git:** repo is not yet initialized — init on first commit  
> **Hard constraints:** `aws-transform-output/` and `aws-lambda-transform/` must stay completely empty. No credentials in source.

---

## FEAT-001 — Azure Functions app + Bicep infra

- [ ] 1. Create `azure-app/src/MigrationFortune.csproj`
      OutputType=Exe, TargetFramework=net8.0, ImplicitUsings=enable, Nullable=enable.
      NuGet: Microsoft.Azure.Functions.Worker 1.*, .Worker.Extensions.Http 3.*, .Worker.Extensions.Storage.Blobs 6.*, Azure.Storage.Blobs 12.*
      Files: `azure-app/src/MigrationFortune.csproj`
      Verify: `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet restore azure-app/src/MigrationFortune.csproj` exits 0.

- [ ] 2. Create data models
      `AssessRequest.cs`: WorkloadName (string?), Description (string?), TechStack (string[]?), YearsInProduction (int?) — JSON camelCase via [JsonPropertyName].
      `AssessmentResult.cs`: all 10 response fields (AssessmentId, WorkloadName, MigrationComplexity, RecommendedStrategy, SuccessProbability, Fortune, DivineAdvice, AssessedAt, Source, StoragePath) — JSON camelCase.
      Files: `azure-app/src/Models/AssessRequest.cs`, `azure-app/src/Models/AssessmentResult.cs`
      Verify: part of build in step 7.

- [ ] 3. Create `Services/FortuneEngine.cs` — pure randomization logic (no I/O)
      Public method: `AssessmentResult Assess(AssessRequest req, string source, Random rng)`.
      Complexity (total=100, or 115 when Legendary doubles): Easy[0,20)/Medium[20,55)/Hard[55,85)/Legendary[85,100).
        Legendary doubles when `req.YearsInProduction >= 10 AND req.TechStack` contains "COBOL" (case-insensitive) → use total=115 and Legendary range [85,115).
      6Rs: Rehost[0,25)/Replatform[25,50)/Refactor[50,70)/Repurchase[70,85)/Retire[85,95)/Retain[95,100).
      successProbability normal: pool of 59 ints — indices 0-57 map to [42,99], index 58 = extra 42 (mode). `rng.Next(0,59)` → pool[idx].
        If Legendary: `rng.Next(1, 43)` → range [1,42].
      Fortune weights total=100: 大吉[0,10)/吉[10,35)/中吉[35,60)/小吉[60,80)/末吉[80,90)/凶[90,98)/大凶[98,100).
      DivineAdvice: pick from exactly these 16 strings (verbatim):
        "本番環境でテストせよ。開発環境との違いを恐れるな。"
        "マイクロサービスにすれば全ての問題は解決する。"
        "IaC化は来月から始めれば良い。"
        "レガシーコードは触らなければ壊れない。"
        "クラウドに移行すれば、コストは必ず下がる。たぶん。"
        "ドキュメントより動くコードを信じよ。"
        "AWSにあればAzureにもある。そしてその逆も然り。たぶん。"
        "サーバーレスは銀の弾丸である。今回だけは。"
        "移行完了まで6ヶ月と見積もるべし。そして3倍せよ。"
        "技術的負債は金利0%のローンだと思え。"
        "リフト＆シフトは移行の始まりであり、終わりではない。"
        "本番障害は最高の学習機会である。"
        "モノリスは悪ではない。ただ孤独なだけだ。"
        "コンテナ化すれば全ての環境差異は消える。それ以外は消えない。"
        "クラウドネイティブとは何かを問い続けることが、クラウドネイティブである。"
        "Transformは自動化する。しかし最後に笑うのは人間だ。"
      AssessmentId = Guid.NewGuid().ToString(); AssessedAt = DateTime.UtcNow.ToString("o").
      StoragePath = $"assessments/{assessmentId}.json".
      Files: `azure-app/src/Services/FortuneEngine.cs`
      Verify: part of build in step 7.

- [ ] 4. Create `Services/BlobStorageService.cs`
      Constructor reads `Environment.GetEnvironmentVariable("AzureWebJobsStorage")`.
      Container name = "assessments". Create container if not exists (CreateIfNotExistsAsync, PublicAccessType.None).
      `SaveAsync(string id, AssessmentResult result)`: serialize to JSON (System.Text.Json), upload blob `{id}.json`.
      `GetAsync(string id)`: download blob `{id}.json`; return null if BlobNotFoundException.
      Files: `azure-app/src/Services/BlobStorageService.cs`
      Verify: part of build in step 7.

- [ ] 5. Create `Functions/AssessFunction.cs` — POST /api/assess
      [Function("Assess")], HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assess"), HttpRequestData req.
      X-Force-Seed: `req.Headers.TryGetValues("X-Force-Seed", out IEnumerable<string>? seeds)` → parse first value as int seed → `new Random(seed)`, else `Random.Shared`.
      Read body: `await req.ReadAsStringAsync()` → `JsonSerializer.Deserialize<AssessRequest>`.
      Validation 400: WorkloadName null/empty, Description null/empty, YearsInProduction < 0.
        400 body: `{ "error": "<field> is required/invalid" }`.
      Call `_fortune.Assess(req, "azure-functions", rng)`, then `await _blob.SaveAsync(result.AssessmentId, result)`.
      Return 200 with result JSON.
      Files: `azure-app/src/Functions/AssessFunction.cs`
      Verify: part of build in step 7.

- [ ] 6. Create `Functions/GetAssessmentFunction.cs` — GET /api/assessment/{id}
      [Function("GetAssessment")], HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assessment/{id}"), HttpRequestData req, string id.
      `var result = await _blob.GetAsync(id)`.
      If null → 404 with `{ "error": "Assessment not found", "id": "{id}" }`.
      Else → 200 with result JSON.
      Files: `azure-app/src/Functions/GetAssessmentFunction.cs`
      Verify: part of build in step 7.

- [ ] 7. Create `Program.cs`, `host.json`, `local.settings.json`; build the project
      Program.cs: `new HostBuilder().ConfigureFunctionsWorkerDefaults().ConfigureServices(s => { s.AddSingleton<BlobStorageService>(); s.AddSingleton<FortuneEngine>(); }).Build().RunAsync()`.
      host.json: `{ "version": "2.0", "logging": { "applicationInsights": { "samplingSettings": { "isEnabled": true } } } }`.
      local.settings.json: `{ "IsEncrypted": false, "Values": { "AzureWebJobsStorage": "UseDevelopmentStorage=true", "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated" } }`.
      Files: `azure-app/src/Program.cs`, `azure-app/src/host.json`, `azure-app/src/local.settings.json`
      Verify: `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet build azure-app/src/MigrationFortune.csproj` → "Build succeeded."

- [ ] 8. Create Bicep files
      `storage.bicep`: StorageAccount (Standard_LRS, StorageV2, Hot, kind=StorageV2) + blobContainer "assessments". Tags: project=atoa-poc, env=dev, app=migration-fortune, managedBy=bicep.
      `functions.bicep`: AppServicePlan (Y1/Dynamic/Linux) + FunctionApp (.NET 8 isolated, Linux, functionsVersion=~4, systemAssigned MI, appSettings FUNCTIONS_WORKER_RUNTIME=dotnet-isolated + AzureWebJobsStorage from storageConnectionString param) + roleAssignment (Storage Blob Data Contributor role 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' to functionApp MI on storageAccount). Tags same as above.
      `main.bicep`: params location (default 'japaneast') and prefix (default 'atoaPoc'). Module references to storage.bicep and functions.bicep with correct wiring.
      `parameters/poc.bicepparam`: using '../main.bicep', location='japaneast', prefix='atoaPoc'.
      Files: `azure-app/infra/bicep/storage.bicep`, `azure-app/infra/bicep/functions.bicep`, `azure-app/infra/bicep/main.bicep`, `azure-app/infra/bicep/parameters/poc.bicepparam`
      Verify: `az bicep build --file azure-app/infra/bicep/main.bicep` exits 0 (lints the Bicep).

- [ ] 9. Create `azure-app/README.md`
      Sections: Prerequisites (Azure CLI, func v4, .NET 8, Azurite), Local development (func start), Deploy (az deployment group create), Cleanup (refer to cleanup-azure.sh).
      Files: `azure-app/README.md`
      Verify: file exists and is non-empty.

---

## FEAT-002 — Manual Lambda app + SAM template

- [ ] 10. Create `aws-lambda-manual/src/MigrationFortuneManual.csproj`
       TargetFramework=net8.0, GenerateRuntimeConfigurationFiles=true, AWSProjectType=Lambda, Nullable=enable, ImplicitUsings=enable.
       NuGet: Amazon.Lambda.Core 2.*, Amazon.Lambda.APIGatewayEvents 2.*, Amazon.Lambda.Serialization.SystemTextJson 2.*, AWSSDK.S3 3.*
       Files: `aws-lambda-manual/src/MigrationFortuneManual.csproj`
       Verify: `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet restore aws-lambda-manual/src/MigrationFortuneManual.csproj` exits 0.

- [ ] 11. Create data models (independent, no Azure deps)
       Same field structure as FEAT-001 models but in namespace `MigrationFortuneManual.Models`.
       Files: `aws-lambda-manual/src/Models/AssessRequest.cs`, `aws-lambda-manual/src/Models/AssessmentResult.cs`
       Verify: part of build in step 14.

- [ ] 12. Create `Services/FortuneEngine.cs` — independent reimplementation
       Identical logic to FEAT-001's FortuneEngine but in namespace `MigrationFortuneManual.Services`.
       No Azure SDK references. Same all 16 divine advice strings verbatim.
       Files: `aws-lambda-manual/src/Services/FortuneEngine.cs`
       Verify: part of build in step 14.

- [ ] 13. Create `Services/S3StorageService.cs`
       Constructor reads `Environment.GetEnvironmentVariable("S3_BUCKET_NAME")`.
       Uses `Amazon.S3.AmazonS3Client` (default region from env/IAM role).
       `SaveAsync(string id, AssessmentResult result)`: PutObject with key `assessments/{id}.json`, ContentType=application/json.
       `GetAsync(string id)`: GetObject; catch AmazonS3Exception (key=NoSuchKey) → return null.
       Files: `aws-lambda-manual/src/Services/S3StorageService.cs`
       Verify: part of build in step 14.

- [ ] 14. Create `Functions/AssessFunction.cs` and `Functions/GetAssessmentFunction.cs`; add assembly serializer attribute; build
       `[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]` in AssessFunction.cs.
       AssessFunction handler: `public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)`.
       X-Force-Seed: headers are lowercase in HTTP/2 → `request.Headers?.TryGetValue("x-force-seed", out string? seedVal)`.
       Validation 400: same rules as FEAT-001. source = "aws-lambda-manual".
       GetAssessmentFunction: path param from `request.PathParameters["id"]`.
       Files: `aws-lambda-manual/src/Functions/AssessFunction.cs`, `aws-lambda-manual/src/Functions/GetAssessmentFunction.cs`
       Verify: `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet build aws-lambda-manual/src/MigrationFortuneManual.csproj` → "Build succeeded."

- [ ] 15. Create `aws-lambda-manual/src/aws-lambda-tools-defaults.json`
       `{ "profile": "", "region": "ap-northeast-1", "configuration": "Release", "framework": "net8.0", "function-architecture": "arm64" }`
       Files: `aws-lambda-manual/src/aws-lambda-tools-defaults.json`
       Verify: `python3 -c "import json; json.load(open('aws-lambda-manual/src/aws-lambda-tools-defaults.json'))"` exits 0.

- [ ] 16. Create `aws-lambda-manual/template.yaml` (SAM)
       S3Bucket: `atoa-poc-manual-${AWS::AccountId}`, BucketEncryption AES256, VersioningConfiguration Suspended, Tags.
       IAM Role: least-privilege — S3 PutObject/GetObject on `arn:aws:s3:::atoa-poc-manual-${AWS::AccountId}/assessments/*`.
       LambdaAssess: Runtime=dotnet8, Architectures=[arm64], MemorySize=512, Timeout=30, Handler=MigrationFortuneManual::MigrationFortuneManual.Functions.AssessFunction::HandleAsync, env S3_BUCKET_NAME=!Sub atoa-poc-manual-${AWS::AccountId}.
       LambdaGetAssessment: Runtime=dotnet8, Architectures=[arm64], MemorySize=256, Timeout=10, Handler=MigrationFortuneManual::MigrationFortuneManual.Functions.GetAssessmentFunction::HandleAsync.
       HTTP API v2: POST /assess → LambdaAssess, GET /assessment/{id} → LambdaGetAssessment.
       Tags on ALL resources: project=atoa-poc, variant=manual, env=dev, app=migration-fortune.
       Files: `aws-lambda-manual/template.yaml`
       Verify: `sam validate --template aws-lambda-manual/template.yaml --lint` exits 0.

---

## FEAT-003 — Benchmark scripts, cleanup scripts, root docs

- [ ] 17. Create `benchmark/test-cases.json` with 13 test cases
       Cases 1-6 (valid, expected 200): legacy-erp(Java,Oracle,15y), monolith-api(Ruby,PostgreSQL,8y), cobol-batch(COBOL,DB2,25y), dotnet-webapi(.NET,SQL Server,6y), micro-service(Go,PostgreSQL,Kubernetes,1y), serverless-func(C#,.NET 8,2y).
       Cases 7-9 (validation errors, expected 400): workloadName="", description omitted, yearsInProduction=-1.
       Case 10 (valid edge, expected 200): yearsInProduction=0.
       Case 11 (valid, expected 200): techStack with 10 elements.
       Case 12 (valid, expected 200): workloadName = 100-char string.
       Case 13 (expected 404): GET /api/assessment/00000000-0000-0000-0000-000000000000.
       Each case: id, label, method, endpoint (relative path), body (if POST), expectedStatus.
       Files: `benchmark/test-cases.json`
       Verify: `python3 -c "import json; d=json.load(open('benchmark/test-cases.json')); assert len(d)==13"` exits 0.

- [ ] 18. Create run scripts and measure.sh
       `run-azure.sh` / `run-aws-manual.sh`: both use `BASE_URL` env var (defaults to http://localhost:7071 / http://localhost:3000). Read test-cases.json, iterate with curl, check status codes, print PASS/FAIL per case.
       `run-aws-transform.sh`: placeholder noting it's for Phase 6 (AWS Transform output), same structure.
       `measure.sh ENV BASE_URL`: runs cases 1-6 × 10 each; record per-request latency with `curl -o /dev/null -s -w "%{time_total}\n" -X POST ...`; collect all 60 latencies; compute p50/p95 via awk sort+percentile; throughput test with `printf '%s\n' {1..300} | xargs -P 10 -I{} curl -s -o /dev/null ...` for ~15 seconds; output `/Users/koji/kiro2/atoa/results/${ENV}-raw.json` and `/Users/koji/kiro2/atoa/results/${ENV}-summary.json`. Use python3 for JSON output.
       Files: `benchmark/run-azure.sh`, `benchmark/run-aws-manual.sh`, `benchmark/run-aws-transform.sh`, `benchmark/measure.sh`
       Verify: `chmod +x benchmark/run-azure.sh benchmark/run-aws-manual.sh benchmark/run-aws-transform.sh benchmark/measure.sh` exits 0; `bash -n benchmark/measure.sh` exits 0.

- [ ] 19. Create `benchmark/compare.sh`
       Reads `results/{env}-summary.json` for each of 3 envs (gracefully handles missing files with PLACEHOLDER).
       Outputs `results/comparison.csv` with columns: metric,azure,aws-transform,aws-manual,notes.
       Rows: auto_conversion_rate, manual_effort_lines, functional_compatibility, p50_latency_ms, p95_latency_ms, throughput_rps, operational_fitness (estimated), security_score (estimated), total_migration_effort_h.
       Outputs `results/report.md`: markdown table + discussion section. Clearly labels MEASURED vs ESTIMATED.
       Files: `benchmark/compare.sh`
       Verify: `bash -n benchmark/compare.sh` exits 0.

- [ ] 20. Create `benchmark/README.md`
       Documents usage of all scripts, ENV variables, expected output format.
       Files: `benchmark/README.md`
       Verify: file exists and non-empty.

- [ ] 21. Create cleanup scripts
       `cleanup-azure.sh <rg-name>`: list resources in RG, prompt `削除しますか? [y/N]`, if y: `az group delete --name $1 --yes --no-wait` and print job started.
       `cleanup-aws.sh <manual-stack> <transform-stack>`: list stack resources for both stacks, confirm, `aws s3 rb s3://$(aws cloudformation describe-stack-resource --stack-name $1 ...) --force` for each S3 bucket, then `aws cloudformation delete-stack` for each stack.
       `cleanup-all.sh`: calls `./cleanup-azure.sh "$@"` then `./cleanup-aws.sh "$@"`.
       Files: `cleanup-azure.sh`, `cleanup-aws.sh`, `cleanup-all.sh`
       Verify: `bash -n cleanup-azure.sh && bash -n cleanup-aws.sh && bash -n cleanup-all.sh` all exit 0.

- [ ] 22. Create root `README.md`
       Sections: Purpose (AWS Transform Azure-to-AWS GA, Level 300 LT), Fortune API concept (self-referential joke API), Prerequisites, Directory structure, Step-by-step setup (local dev → Azure deploy → AWS deploy → benchmark), Metric definitions (clearly separating MEASURED vs ESTIMATED), Cost/billing notes, Cleanup.
       Files: `README.md`
       Verify: file exists and non-empty.

- [ ] 23. Create `.gitignore` and `results/.gitkeep`; initialize git repo; chmod all scripts
       `.gitignore` content: `bin/`, `obj/`, `local.settings.json`, `.env`, `*.user`. Do NOT list aws-transform-output/ or results/.
       `results/.gitkeep`: empty file to track results/ directory.
       Files: `.gitignore`, `results/.gitkeep`
       Verify:
         `chmod +x benchmark/*.sh cleanup-azure.sh cleanup-aws.sh cleanup-all.sh`
         `git -C /Users/koji/kiro2/atoa init && git -C /Users/koji/kiro2/atoa add -A && git -C /Users/koji/kiro2/atoa commit -m "feat: initial PoC implementation"`
         Both .NET builds pass:
           `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet build azure-app/src/MigrationFortune.csproj`
           `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec /opt/homebrew/opt/dotnet@8/libexec/dotnet build aws-lambda-manual/src/MigrationFortuneManual.csproj`
