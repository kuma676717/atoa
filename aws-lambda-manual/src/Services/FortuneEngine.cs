using MigrationFortuneManual.Models;

namespace MigrationFortuneManual.Services;

public class FortuneEngine
{
    // ===== 技術スタック分類 =====

    // 古代遺跡級（+30点/技術）
    private static readonly HashSet<string> TierAncient = new(StringComparer.OrdinalIgnoreCase)
    {
        "COBOL", "FORTRAN", "RPG", "PL/I", "PL1", "AS/400", "iSeries",
        "ALGOL", "MUMPS", "DB2", "VSAM", "JCL", "CICS"
    };

    // レガシー級（+15点/技術）
    private static readonly HashSet<string> TierLegacy = new(StringComparer.OrdinalIgnoreCase)
    {
        "VB6", "VBScript", "PowerBuilder", "Delphi", "FoxPro", "Access",
        "ColdFusion", "Perl", "Flash", "Silverlight", "WebForms", "SOAP",
        "Visual Basic", "Classic ASP"
    };

    // 旧世代級（+5点/技術）
    private static readonly HashSet<string> TierOld = new(StringComparer.OrdinalIgnoreCase)
    {
        ".NET Framework", "Java 6", "Java 7", "PHP 5", "Python 2",
        "AngularJS", "jQuery", "WebLogic", "WebSphere", "Struts", "EJB"
    };

    private static readonly string[] NgWords = new[]
    {
        "神", "闇", "呪い", "触るな", "危険", "爆弾",
        "誰も知らない", "ドキュメントなし", "仕様書なし", "テストなし",
        "なぜか動く", "動いてるから触るな", "負債", "本番のみ", "秘伝のタレ"
    };

    private static readonly string[] GoodWords = new[]
    {
        "CI/CD", "テスト済み", "IaC", "Terraform", "Docker",
        "Kubernetes", "マイクロサービス", "自動化", "監視", "可観測性",
        "型安全", "コンテナ", "APIファースト", "Infrastructure as Code",
        "ゼロダウンタイム", "Blue/Green"
    };

    // ===== ご神託プール =====

    private static readonly string[] AdvicesGeneral = new[]
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
        "Transformは自動化する。しかし最後に笑うのは人間だ。",
    };

    private static readonly string[] AdvicesAncient = new[]
    {
        "COBOLは死なない。ただ、COBOLエンジニアが希少になるだけだ。",
        "このシステムの稼働年数は、チームの平均年齢を超えている。",
        "AS/400は今日も元気に動いている。それが問題だ。",
        "JCLを読める人間は、今や絶滅危惧種に指定されるべきだ。",
        "このシステムには、誰も触りたくない聖域がある。そこにこそ移行の核心がある。",
    };

    private static readonly string[] AdvicesLegacy = new[]
    {
        "Visual Basicを選んだ過去の自分を責めるな。当時はベストだった。たぶん。",
        "PowerBuilderは今も生きている。ただし、あなたのキャリアに影響を与えるかもしれない。",
        "Flash時代のUIをReactに書き直す前に、本当にそれが必要かを考えよ。",
        "ColdFusionを知っている者は、語らない。語る者は、知らない。",
    };

    private static readonly string[] AdvicesOld = new[]
    {
        ".NET Frameworkからの移行は、リモコンの電池交換のようなものだ。やれば10年戦える。",
        "Spring XMLの設定ファイルを見た者は、Kubernetesを恋しく思うようになる。",
        "WebLogicの設定は、誰も理解していない。設定した本人でさえも。",
    };

    private static readonly string[] AdvicesLongRunning = new[]
    {
        "20年以上稼働したシステムは、もはや文化遺産である。移行には学芸員の資格が必要かもしれない。",
        "このシステムを最後に触った人間の消息は、誰も知らない。",
        "長く動いているシステムほど、誰も止められない。それは愛着ではなく、恐怖だ。",
    };

    private static readonly string AdviceCobol30 =
        "30年稼働したCOBOLを移行するより、会社ごと移行した方が早い場合がある。";
    private static readonly string AdviceCobol20 =
        "このコードを書いた人は、すでに2回転職して定年退職している。";
    private static readonly string AdviceCobol10 =
        "このコードを書いた人は、すでに定年退職している。";
    private static readonly string AdviceAncient20 =
        "移行は不可能ではない。ただ、あなたの在職中には終わらない可能性がある。";
    private static readonly string AdviceLegendary =
        "伝説となったシステムには、伝説の予算と伝説の忍耐が必要である。";

    // ===== レガシースコア計算 =====

    private static int CalcLegacyScore(AssessRequest req)
    {
        int score = 0;
        var stack = req.TechStack ?? Array.Empty<string>();

        foreach (var t in stack)
        {
            if      (TierAncient.Contains(t)) score += 30;
            else if (TierLegacy.Contains(t))  score += 15;
            else if (TierOld.Contains(t))      score += 5;
        }

        int years = req.YearsInProduction ?? 0;
        score += Math.Min(years * 2, 50);

        return score;
    }

    private static int CalcVibeScore(string? description)
    {
        if (string.IsNullOrEmpty(description)) return 0;
        int score = 0;
        foreach (var w in NgWords)   if (description.Contains(w, StringComparison.OrdinalIgnoreCase)) score--;
        foreach (var w in GoodWords) if (description.Contains(w, StringComparison.OrdinalIgnoreCase)) score++;
        return Math.Clamp(score, -3, 3);
    }

    // ===== メイン =====

    public AssessmentResult Assess(AssessRequest req, string source, Random rng)
    {
        int legacyScore = CalcLegacyScore(req);
        int years = req.YearsInProduction ?? 0;
        var stack = req.TechStack ?? Array.Empty<string>();

        string complexity   = DrawComplexity(rng, legacyScore);
        string strategy     = DrawStrategy(rng);
        int    successProb  = DrawSuccessProbability(rng, complexity, legacyScore);
        int vibeScore   = CalcVibeScore(req.Description);
        string fortune      = DrawFortune(rng, complexity, vibeScore);
        string divineAdvice = DrawAdvice(rng, stack, legacyScore, years, complexity);

        return new AssessmentResult
        {
            AssessmentId        = Guid.NewGuid().ToString(),
            WorkloadName        = req.WorkloadName ?? string.Empty,
            MigrationComplexity = complexity,
            RecommendedStrategy = strategy,
            SuccessProbability  = successProb,
            Fortune             = fortune,
            DivineAdvice        = divineAdvice,
            AssessedAt          = DateTime.UtcNow.ToString("o"),
            Source              = source,
            StoragePath         = $"assessments/{Guid.NewGuid()}.json"
        };
    }

    // ===== 抽選ロジック =====

    private static string DrawComplexity(Random rng, int score)
    {
        int easy, medium, hard, legendary;
        if      (score <  20) { easy = 40; medium = 35; hard = 20; legendary =  5; }
        else if (score <  40) { easy = 20; medium = 35; hard = 30; legendary = 15; }
        else if (score <  60) { easy = 10; medium = 25; hard = 40; legendary = 25; }
        else if (score <  80) { easy =  5; medium = 15; hard = 35; legendary = 45; }
        else                  { easy =  0; medium =  5; hard = 20; legendary = 75; }

        int total = easy + medium + hard + legendary;
        int r = rng.Next(0, total);
        if (r < easy)                 return "Easy";
        if (r < easy + medium)        return "Medium";
        if (r < easy + medium + hard) return "Hard";
        return "Legendary";
    }

    private static string DrawStrategy(Random rng)
    {
        int r = rng.Next(0, 100);
        return r < 25 ? "Rehost"
             : r < 50 ? "Replatform"
             : r < 70 ? "Refactor"
             : r < 85 ? "Repurchase"
             : r < 95 ? "Retire"
             : "Retain";
    }

    private static int DrawSuccessProbability(Random rng, string complexity, int legacyScore)
    {
        if (complexity == "Legendary")
        {
            int max = legacyScore >= 80 ? 20 : 42;
            return rng.Next(1, max + 1);
        }

        if (legacyScore >= 60)
            return rng.Next(30, 76);

        int[] pool = new int[59];
        for (int i = 0; i < 58; i++) pool[i] = 42 + i;
        pool[58] = 42;
        return pool[rng.Next(0, 59)];
    }

    private static string DrawFortune(Random rng, string complexity, int vibeScore)
    {
        int daiKichi, kichi, chuKichi, shoKichi, sueKichi, kyo, daiKyo;
        switch (complexity)
        {
            case "Easy":
                daiKichi = 20; kichi = 30; chuKichi = 25; shoKichi = 15; sueKichi = 7; kyo = 2; daiKyo = 1;
                break;
            case "Medium":
                daiKichi = 10; kichi = 25; chuKichi = 25; shoKichi = 20; sueKichi = 10; kyo = 8; daiKyo = 2;
                break;
            case "Hard":
                daiKichi =  5; kichi = 15; chuKichi = 20; shoKichi = 20; sueKichi = 20; kyo = 15; daiKyo = 5;
                break;
            default: // Legendary
                daiKichi =  1; kichi =  5; chuKichi = 10; shoKichi = 14; sueKichi = 20; kyo = 30; daiKyo = 20;
                break;
        }
        int shift = Math.Abs(vibeScore) * 10;
        if (vibeScore > 0)
        {
            daiKichi += shift;
            kichi += shift;
        }
        else if (vibeScore < 0)
        {
            daiKyo += shift;
            kyo += shift;
        }

        int total = daiKichi + kichi + chuKichi + shoKichi + sueKichi + kyo + daiKyo;
        int r = rng.Next(0, total);
        int acc = 0;
        if (r < (acc += daiKichi)) return "大吉";
        if (r < (acc += kichi))    return "吉";
        if (r < (acc += chuKichi)) return "中吉";
        if (r < (acc += shoKichi)) return "小吉";
        if (r < (acc += sueKichi)) return "末吉";
        if (r < (acc += kyo))      return "凶";
        return "大凶";
    }

    private static string DrawAdvice(Random rng, string[] stack, int legacyScore, int years, string complexity)
    {
        bool hasAncient = stack.Any(t => TierAncient.Contains(t));
        bool hasCobol   = stack.Any(t => t.Equals("COBOL", StringComparison.OrdinalIgnoreCase));
        bool hasLegacy  = stack.Any(t => TierLegacy.Contains(t));
        bool hasOld     = stack.Any(t => TierOld.Contains(t));

        // スペシャルメッセージ（優先度順）
        if (hasCobol   && years >= 30) return AdviceCobol30;
        if (hasCobol   && years >= 20) return AdviceCobol20;
        if (hasCobol   && years >= 10) return AdviceCobol10;
        if (hasAncient && years >= 20) return AdviceAncient20;
        if (complexity == "Legendary") return AdviceLegendary;

        var pool = new List<string>(AdvicesGeneral);
        if (hasAncient)       pool.AddRange(AdvicesAncient);
        if (hasLegacy)        pool.AddRange(AdvicesLegacy);
        if (hasOld)           pool.AddRange(AdvicesOld);
        if (years >= 20)      pool.AddRange(AdvicesLongRunning);
        else if (years >= 10) pool.Add("10年以上稼働したシステムには、誰も触りたくない理由がある。");

        return pool[rng.Next(0, pool.Count)];
    }
}
