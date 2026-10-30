using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace MigrationFortune.Functions;

public class UiFunction
{
    [Function("Ui")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")]
        HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        await response.WriteStringAsync(GetHtml("/api/assess"));
        return response;
    }

    private static string GetHtml(string assessEndpoint) => $$"""
        <!DOCTYPE html>
        <html lang="ja">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>クラウド移行診断おみくじ</title>
          <style>
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
              font-family: 'Hiragino Kaku Gothic ProN', 'Noto Sans JP', sans-serif;
              background: #f5f0e8;
              color: #3d2b1f;
              padding: 20px;
              min-height: 100vh;
            }
            .container { max-width: 600px; margin: auto; }
            h1 { text-align: center; padding: 24px 0 20px; font-size: 1.6em; }
            .card {
              background: #faf7f0;
              border-radius: 8px;
              padding: 24px;
              margin-bottom: 20px;
              box-shadow: 0 2px 8px rgba(61,43,31,0.1);
            }
            .field { margin-bottom: 16px; }
            label { display: block; margin-bottom: 6px; font-weight: bold; font-size: 0.9em; }
            input[type=text], input[type=number], textarea {
              width: 100%;
              padding: 10px 12px;
              border: 1px solid #c9b99a;
              border-radius: 4px;
              background: #fff;
              color: #3d2b1f;
              font-family: inherit;
              font-size: 1em;
            }
            input:focus, textarea:focus {
              outline: none;
              border-color: #8B7355;
              box-shadow: 0 0 0 2px rgba(139,115,85,0.2);
            }
            textarea { resize: vertical; }
            #submitBtn {
              width: 100%;
              padding: 14px;
              background: #8B7355;
              color: #fff;
              border: none;
              border-radius: 4px;
              font-size: 1.1em;
              cursor: pointer;
              font-family: inherit;
              transition: background 0.2s;
            }
            #submitBtn:hover:not(:disabled) { background: #6b5440; }
            #submitBtn:disabled { opacity: 0.6; cursor: not-allowed; }
            .result-header { text-align: center; margin-bottom: 20px; }
            .fortune-emoji { font-size: 3.5em; display: block; line-height: 1.2; }
            .fortune-label { font-size: 2em; font-weight: bold; display: block; margin: 4px 0; }
            .complexity-badge {
              display: inline-block;
              padding: 4px 14px;
              border-radius: 20px;
              color: #fff;
              font-size: 0.85em;
              font-weight: bold;
            }
            .field-row { margin-bottom: 14px; }
            .field-label { font-size: 0.8em; color: #8B7355; margin-bottom: 3px; }
            .field-value { font-size: 1em; }
            .progress-bg { height: 12px; background: #e0d5c5; border-radius: 6px; overflow: hidden; margin: 6px 0 2px; }
            .progress-fill { height: 100%; border-radius: 6px; transition: width 0.6s ease; }
            .progress-num { font-size: 0.9em; color: #5a3e2b; }
            blockquote {
              border-left: 4px solid #8B7355;
              padding: 12px 16px;
              margin: 16px 0;
              font-size: 1.2em;
              font-style: italic;
              color: #5a3e2b;
              background: rgba(139,115,85,0.05);
              border-radius: 0 4px 4px 0;
            }
            .source-text { text-align: right; font-size: 0.8em; color: #999; }
            .assessment-id { font-size: 0.75em; color: #999; text-align: center; margin-top: 12px; user-select: all; }
            .error-msg { color: #f44336; }
          </style>
        </head>
        <body>
          <div class="container">
            <h1>🎯 クラウド移行診断おみくじ</h1>
            <div class="card">
              <form id="assessForm">
                <div class="field">
                  <label for="workloadName">ワークロード名</label>
                  <input type="text" id="workloadName" required placeholder="例: legacy-erp, cobol-batch">
                </div>
                <div class="field">
                  <label for="description">説明</label>
                  <textarea id="description" required placeholder="システムの説明" rows="3"></textarea>
                </div>
                <div class="field">
                  <label for="techStack">技術スタック（カンマ区切り）</label>
                  <input type="text" id="techStack" placeholder="例: COBOL, Java, Oracle">
                </div>
                <div class="field">
                  <label for="yearsInProduction">稼働年数</label>
                  <input type="number" id="yearsInProduction" min="0" placeholder="稼働年数">
                </div>
                <button type="submit" id="submitBtn">🔮 診断する</button>
              </form>
            </div>
            <div id="resultCard" class="card" style="display:none"></div>
          </div>
          <script>
            (function () {
              var ENDPOINT = '{{assessEndpoint}}';
              var form = document.getElementById('assessForm');
              var btn = document.getElementById('submitBtn');
              var resultCard = document.getElementById('resultCard');

              form.addEventListener('submit', function (e) {
                e.preventDefault();

                var workloadName = document.getElementById('workloadName').value.trim();
                var description = document.getElementById('description').value.trim();
                var techStackRaw = document.getElementById('techStack').value.trim();
                var yearsRaw = document.getElementById('yearsInProduction').value.trim();

                var body = { workloadName: workloadName, description: description };
                if (techStackRaw) {
                  body.techStack = techStackRaw.split(',').map(function (s) { return s.trim(); });
                }
                if (yearsRaw) {
                  body.yearsInProduction = parseInt(yearsRaw, 10);
                }

                btn.textContent = '占い中...🔮';
                btn.disabled = true;
                resultCard.style.display = 'none';

                fetch(ENDPOINT, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify(body)
                }).then(function (res) {
                  if (!res.ok) { throw new Error('HTTP ' + res.status); }
                  return res.json();
                }).then(function (data) {
                  renderResult(data);
                }).catch(function (err) {
                  resultCard.style.borderLeft = '';
                  resultCard.style.boxShadow = '0 2px 8px rgba(61,43,31,0.1)';
                  resultCard.style.display = 'block';
                  resultCard.innerHTML = '<p class="error-msg">エラーが発生しました: ' + esc(err.message) + '</p>';
                }).finally(function () {
                  btn.textContent = '🔮 診断する';
                  btn.disabled = false;
                });
              });

              function complexityColor(c) {
                if (c === 'Easy') { return '#4caf50'; }
                if (c === 'Medium') { return '#ff9800'; }
                if (c === 'Hard') { return '#f44336'; }
                if (c === 'Legendary') { return '#9c27b0'; }
                return '#8B7355';
              }

              function fortuneEmoji(f) {
                var m = {
                  '大吉': '🎊', '吉': '⭐', '中吉': '🌟', '小吉': '✨',
                  '末吉': '🍀', '凶': '💧', '大凶': '⚡'
                };
                return m[f] || '🎯';
              }

              function progressColor(p) {
                if (p <= 33) { return '#f44336'; }
                if (p <= 66) { return '#ff9800'; }
                return '#4caf50';
              }

              function esc(s) {
                if (!s) { return ''; }
                return String(s)
                  .replace(/&/g, '&amp;')
                  .replace(/</g, '&lt;')
                  .replace(/>/g, '&gt;')
                  .replace(/"/g, '&quot;');
              }

              function renderResult(data) {
                var color = complexityColor(data.migrationComplexity);
                var pct = data.successProbability || 0;
                var emoji = fortuneEmoji(data.fortune);
                var pcColor = progressColor(pct);

                resultCard.style.display = 'block';
                resultCard.style.borderLeft = '4px solid ' + color;
                if (data.migrationComplexity === 'Legendary') {
                  resultCard.style.boxShadow = '0 0 20px #ffd700';
                } else {
                  resultCard.style.boxShadow = '0 2px 8px rgba(61,43,31,0.1)';
                }

                resultCard.innerHTML =
                  '<div class="result-header">' +
                    '<span class="fortune-emoji">' + emoji + '</span>' +
                    '<span class="fortune-label">' + esc(data.fortune) + '</span>' +
                    '<span class="complexity-badge" style="background:' + color + '">' + esc(data.migrationComplexity) + '</span>' +
                  '</div>' +
                  '<div class="field-row">' +
                    '<div class="field-label">ワークロード</div>' +
                    '<div class="field-value">' + esc(data.workloadName) + '</div>' +
                  '</div>' +
                  '<div class="field-row">' +
                    '<div class="field-label">推奨戦略</div>' +
                    '<div class="field-value">' + esc(data.recommendedStrategy) + '</div>' +
                  '</div>' +
                  '<div class="field-row">' +
                    '<div class="field-label">成功確率</div>' +
                    '<div class="progress-bg">' +
                      '<div class="progress-fill" style="width:' + pct + '%;background:' + pcColor + '"></div>' +
                    '</div>' +
                    '<div class="progress-num">' + pct + '%</div>' +
                  '</div>' +
                  '<blockquote>' + esc(data.divineAdvice) + '</blockquote>' +
                  '<div class="source-text">' + esc(data.source) + '</div>' +
                  '<div class="assessment-id">Assessment ID: ' + esc(data.assessmentId) + '</div>';
              }
            }());
          </script>
        </body>
        </html>
        """;
}
