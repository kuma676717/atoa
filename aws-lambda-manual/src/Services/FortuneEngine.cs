using MigrationFortuneManual.Models;

namespace MigrationFortuneManual.Services;

public class FortuneEngine
{
    private static readonly string[] Advices = new[]
    {
        "本番環境でテストせよ。開発環境との違いを恐れるな。",
        "マイクロサービスにすれば全ての問題は解決する。",
        "IaC化は来月から始めれば良い。",
        "レガシーコードは触らなければ壊れない。",
        "クラウドに移行すれば、コストは必ず下がる。たぶん。",
        "ドキュメントより動くコードを信じよ。",
        "AWSにあればAzureにもある。そしてその逆も然り。たぶん。",
        "サーバーレスは銀の弾丸である。今回だけは。",
        "移行完了まで6ヶ月と見積もるべし。そして3倍せよ。",
        "技術的負債は金利0%のローンだと思え。",
        "リフト＆シフトは移行の始まりであり、終わりではない。",
        "本番障害は最高の学習機会である。",
        "モノリスは悪ではない。ただ孤独なだけだ。",
        "コンテナ化すれば全ての環境差異は消える。それ以外は消えない。",
        "クラウドネイティブとは何かを問い続けることが、クラウドネイティブである。",
        "Transformは自動化する。しかし最後に笑うのは人間だ。"
    };

    public AssessmentResult Assess(AssessRequest req, string source, Random rng)
    {
        // (a) Migration complexity — Legendary doubles the range when COBOL + 10+ years
        bool legendaryDouble = req.YearsInProduction >= 10
            && req.TechStack != null
            && req.TechStack.Any(t => t.Equals("COBOL", StringComparison.OrdinalIgnoreCase));

        int total = legendaryDouble ? 115 : 100;
        int r = rng.Next(0, total);
        string complexity = r < 20 ? "Easy"
            : r < 55 ? "Medium"
            : r < 85 ? "Hard"
            : "Legendary";

        // (b) Recommended strategy (6Rs)
        int r2 = rng.Next(0, 100);
        string strategy = r2 < 25 ? "Rehost"
            : r2 < 50 ? "Replatform"
            : r2 < 70 ? "Refactor"
            : r2 < 85 ? "Repurchase"
            : r2 < 95 ? "Retire"
            : "Retain";

        // (c) Success probability
        int successProbability;
        if (complexity == "Legendary")
        {
            successProbability = rng.Next(1, 43); // range [1, 42]
        }
        else
        {
            int[] pool = new int[59];
            for (int i = 0; i < 58; i++) pool[i] = 42 + i;
            pool[58] = 42; // extra 42 → mode
            successProbability = pool[rng.Next(0, 59)];
        }

        // (d) Fortune
        int rf = rng.Next(0, 100);
        string fortune = rf < 10 ? "大吉"
            : rf < 35 ? "吉"
            : rf < 60 ? "中吉"
            : rf < 80 ? "小吉"
            : rf < 90 ? "末吉"
            : rf < 98 ? "凶"
            : "大凶";

        // (e) Divine advice
        string divineAdvice = Advices[rng.Next(0, 16)];

        // (f) Build result
        string assessmentId = Guid.NewGuid().ToString();
        string assessedAt = DateTime.UtcNow.ToString("o");
        string storagePath = $"assessments/{assessmentId}.json";

        return new AssessmentResult
        {
            AssessmentId = assessmentId,
            WorkloadName = req.WorkloadName ?? string.Empty,
            MigrationComplexity = complexity,
            RecommendedStrategy = strategy,
            SuccessProbability = successProbability,
            Fortune = fortune,
            DivineAdvice = divineAdvice,
            AssessedAt = assessedAt,
            Source = source,
            StoragePath = storagePath
        };
    }
}
