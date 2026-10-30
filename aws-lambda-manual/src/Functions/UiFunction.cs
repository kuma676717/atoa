using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace MigrationFortuneManual.Functions;

public class UiFunction
{
    public Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        return Task.FromResult(new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = GetHtml("/assess"),
            Headers = new Dictionary<string, string> { { "Content-Type", "text/html; charset=utf-8" } }
        });
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
            .tech-group { margin-bottom: 12px; }
            .tech-group-label { font-size: 0.8em; color: #8B7355; margin-bottom: 6px; font-weight: bold; }
            .tech-chips { display: flex; flex-wrap: wrap; gap: 6px; }
            .tech-chip input[type=checkbox] { display: none; }
            .tech-chip label {
              display: inline-block;
              padding: 4px 10px;
              border-radius: 20px;
              border: 1px solid #c9b99a;
              cursor: pointer;
              font-size: 0.85em;
              transition: all 0.15s;
              user-select: none;
            }
            .tech-chip.ancient label { background: #f5ede0; border-color: #a07040; color: #5a3218; }
            .tech-chip.ancient input:checked + label { background: #8B5020; color: #fff; border-color: #8B5020; }
            .tech-chip.legacy label { background: #f5f0e8; border-color: #c9b99a; color: #5a3e2b; }
            .tech-chip.legacy input:checked + label { background: #8B7355; color: #fff; border-color: #8B7355; }
            .tech-chip.old label { background: #f0f0f5; border-color: #9999bb; color: #333366; }
            .tech-chip.old input:checked + label { background: #5555aa; color: #fff; border-color: #5555aa; }
            .tech-chip.modern label { background: #e8f5e9; border-color: #66bb6a; color: #1b5e20; }
            .tech-chip.modern input:checked + label { background: #2e7d32; color: #fff; border-color: #2e7d32; }
            .vibe-hint { font-size: 0.8em; margin-top: 6px; color: #666; }
            .vibe-hint .ng { color: #c62828; }
            .vibe-hint .good { color: #2e7d32; }
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
                <div class="vibe-hint">
                  💀 凶ワード例：<span class="ng">「神」「闇」「触るな」「秘伝のタレ」</span><br>
                  ✨ 吉ワード例：<span class="good">「Terraform」「CI/CD」「テスト済み」</span>
                </div>
                <div class="field">
                  <label>技術スタック</label>
                  <div class="tech-group">
                    <div class="tech-group-label">🏺 古代遺跡（選ぶと重たい結果になりやすい）</div>
                    <div class="tech-chips">
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-COBOL" value="COBOL"><label for="t-COBOL">COBOL</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-FORTRAN" value="FORTRAN"><label for="t-FORTRAN">FORTRAN</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-RPG" value="RPG"><label for="t-RPG">RPG</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-AS400" value="AS/400"><label for="t-AS400">AS/400</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-CICS" value="CICS"><label for="t-CICS">CICS</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-JCL" value="JCL"><label for="t-JCL">JCL</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-DB2" value="DB2"><label for="t-DB2">DB2</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-VSAM" value="VSAM"><label for="t-VSAM">VSAM</label></span>
                      <span class="tech-chip ancient"><input type="checkbox" name="tech" id="t-PLI" value="PL/I"><label for="t-PLI">PL/I</label></span>
                    </div>
                  </div>
                  <div class="tech-group">
                    <div class="tech-group-label">📼 レガシー</div>
                    <div class="tech-chips">
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-VB6" value="VB6"><label for="t-VB6">VB6</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-PB" value="PowerBuilder"><label for="t-PB">PowerBuilder</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-Delphi" value="Delphi"><label for="t-Delphi">Delphi</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-CF" value="ColdFusion"><label for="t-CF">ColdFusion</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-Flash" value="Flash"><label for="t-Flash">Flash</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-CASP" value="Classic ASP"><label for="t-CASP">Classic ASP</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-WF" value="WebForms"><label for="t-WF">WebForms</label></span>
                      <span class="tech-chip legacy"><input type="checkbox" name="tech" id="t-Perl" value="Perl"><label for="t-Perl">Perl</label></span>
                    </div>
                  </div>
                  <div class="tech-group">
                    <div class="tech-group-label">🖥️ 旧世代</div>
                    <div class="tech-chips">
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-DOTNETFW" value=".NET Framework"><label for="t-DOTNETFW">.NET Framework</label></span>
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-Java67" value="Java 6/7"><label for="t-Java67">Java 6/7</label></span>
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-PHP5" value="PHP 5"><label for="t-PHP5">PHP 5</label></span>
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-AngularJS" value="AngularJS"><label for="t-AngularJS">AngularJS</label></span>
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-WL" value="WebLogic"><label for="t-WL">WebLogic</label></span>
                      <span class="tech-chip old"><input type="checkbox" name="tech" id="t-WS" value="WebSphere"><label for="t-WS">WebSphere</label></span>
                    </div>
                  </div>
                  <div class="tech-group">
                    <div class="tech-group-label">✨ モダン（選ぶと軽い結果になりやすい）</div>
                    <div class="tech-chips">
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-DN8" value=".NET 8"><label for="t-DN8">.NET 8</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-Java17" value="Java 17+"><label for="t-Java17">Java 17+</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-Node" value="Node.js"><label for="t-Node">Node.js</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-Py3" value="Python 3"><label for="t-Py3">Python 3</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-Go" value="Go"><label for="t-Go">Go</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-K8s" value="Kubernetes"><label for="t-K8s">Kubernetes</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-TF" value="Terraform"><label for="t-TF">Terraform</label></span>
                      <span class="tech-chip modern"><input type="checkbox" name="tech" id="t-React" value="React"><label for="t-React">React</label></span>
                    </div>
                  </div>
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
                var yearsRaw = document.getElementById('yearsInProduction').value.trim();

                var body = { workloadName: workloadName, description: description };
                var techStack = Array.from(document.querySelectorAll('input[name="tech"]:checked'))
                    .map(function(cb) { return cb.value; });
                if (techStack.length > 0) body.techStack = techStack;
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
