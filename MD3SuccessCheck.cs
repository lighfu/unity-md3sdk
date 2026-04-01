using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// 成功チェックマークアニメーション。
    /// Play() で 背景フェード → チェック描画(バウンス) → 外円スイープ → ポップ＆放射 を再生。
    /// </summary>
    public class MD3SuccessCheck : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SuccessCheck, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;
        readonly float _size;

        double _startTime;
        bool _playing;
        bool _finished;

        // ── タイムライン (秒) ──
        const float BgDuration      = 0.25f;  // 背景円スケールイン
        const float CheckDelay      = 0.15f;  // チェック開始ディレイ
        const float CheckDuration   = 0.40f;  // チェック描画
        const float CircleDelay     = 0.05f;  // 外円開始ディレイ (チェック完了後)
        const float CircleDuration  = 0.50f;  // 外円スイープ
        const float PopDelay        = 0.05f;  // ポップ開始ディレイ
        const float PopDuration     = 0.35f;  // ポップ＆放射

        // 放射パーティクル
        const int ParticleCount = 12;

        float TotalDuration => BgDuration + CheckDelay + CheckDuration +
                               CircleDelay + CircleDuration + PopDelay + PopDuration;

        /// <summary>アニメーション完了時に発火</summary>
        public event System.Action Finished;

        public MD3SuccessCheck() : this(64f) { }

        public MD3SuccessCheck(float size = 64f)
        {
            _size = size;
            // 放射パーティクル用に少し余裕を持たせる
            style.width = size * 1.6f;
            style.height = size * 1.6f;
            style.alignSelf = Align.Center;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            MarkDirtyRepaint();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();
        void OnDetach(DetachFromPanelEvent evt) => Stop();

        /// <summary>アニメーション再生開始</summary>
        public void Play()
        {
            _playing = true;
            _finished = false;
            _startTime = EditorApplication.timeSinceStartup;
            MD3AnimLoop.Register(this);
        }

        void Stop()
        {
            _playing = false;
            MD3AnimLoop.Unregister(this);
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            if (!_playing && !_finished) return; // Play() 前は何も描画しない
            float elapsed = _playing ? (float)(EditorApplication.timeSinceStartup - _startTime) : TotalDuration;
            var painter = mgc.painter2D;
            float totalW = resolvedStyle.width;
            float totalH = resolvedStyle.height;
            float cx = totalW * 0.5f;
            float cy = totalH * 0.5f;
            float radius = _size * 0.5f - 3f;
            float strokeWidth = Mathf.Max(3f, _size * 0.065f);
            float scale = _size / 64f;

            // ── Phase 1: 背景円スケールイン ──
            float bgT = Mathf.Clamp01(elapsed / BgDuration);
            bgT = EaseOutBack(bgT);

            if (bgT > 0f)
            {
                var bgColor = _theme.PrimaryContainer;
                bgColor.a = 0.35f * Mathf.Clamp01(elapsed / BgDuration); // フェードイン
                painter.fillColor = bgColor;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius * bgT, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
            }

            // ── Phase 2: チェックマーク (バウンス付き) ──
            float checkStart = BgDuration + CheckDelay;
            float checkT = Mathf.Clamp01((elapsed - checkStart) / CheckDuration);

            if (checkT > 0f)
            {
                float checkEased = EaseOutBack(checkT);

                var p1 = new Vector2(cx - 14f * scale, cy + 1f * scale);
                var p2 = new Vector2(cx - 4f * scale, cy + 11f * scale);
                var p3 = new Vector2(cx + 16f * scale, cy - 9f * scale);

                painter.strokeColor = _theme.Primary;
                painter.lineWidth = strokeWidth * (1f + 0.15f * Overshoot(checkT));
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                painter.BeginPath();

                // 前半: p1 → p2
                float firstT = Mathf.Clamp01(checkEased * 2.2f);
                painter.MoveTo(p1);
                painter.LineTo(Vector2.Lerp(p1, p2, firstT));

                // 後半: p2 → p3
                if (checkEased > 0.4f)
                {
                    float secondT = Mathf.Clamp01((checkEased - 0.4f) / 0.6f);
                    painter.LineTo(Vector2.Lerp(p2, p3, secondT));
                }

                painter.Stroke();
            }

            // ── Phase 3: 外円スイープ ──
            float circleStart = checkStart + CheckDuration + CircleDelay;
            float circleT = Mathf.Clamp01((elapsed - circleStart) / CircleDuration);

            if (circleT > 0f)
            {
                float circleEased = EaseOutCubic(circleT);
                float sweepDeg = circleEased * 360f;

                // ストロークの太さが描画中に少し太くなって戻る
                float thicknessPulse = 1f + 0.4f * Mathf.Sin(circleT * Mathf.PI);
                painter.strokeColor = _theme.Primary;
                painter.lineWidth = strokeWidth * thicknessPulse;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, -90f, -90f + sweepDeg);
                painter.Stroke();
            }

            // ── Phase 4: ポップ＆放射パーティクル ──
            float popStart = circleStart + CircleDuration + PopDelay;
            float popT = Mathf.Clamp01((elapsed - popStart) / PopDuration);

            if (popT > 0f)
            {
                // 全体スケールバウンス (背景円 + 外円に反映済みだが、放射で表現)
                float popEased = EaseOutBack(popT);

                // 放射ドット＆ライン
                for (int i = 0; i < ParticleCount; i++)
                {
                    float angle = (360f / ParticleCount * i - 90f) * Mathf.Deg2Rad;
                    float dir = (i % 2 == 0) ? 1f : 0.7f; // 交互に長さ変更

                    float innerR = radius + 4f * scale;
                    float outerR = innerR + (12f + 8f * dir) * scale * popEased;

                    // フェードアウト
                    float fadeT = Mathf.Clamp01((popT - 0.3f) / 0.7f);
                    var particleColor = (i % 3 == 0) ? _theme.Tertiary :
                                        (i % 3 == 1) ? _theme.Primary : _theme.Secondary;
                    particleColor.a = 1f - EaseInCubic(fadeT);

                    if (particleColor.a < 0.01f) continue;

                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    if (i % 2 == 0)
                    {
                        // ライン
                        var from = new Vector2(cx + cos * (innerR + 2f * scale * popEased), cy + sin * (innerR + 2f * scale * popEased));
                        var to = new Vector2(cx + cos * outerR, cy + sin * outerR);
                        painter.strokeColor = particleColor;
                        painter.lineWidth = Mathf.Max(1.5f, 2f * scale);
                        painter.lineCap = LineCap.Round;
                        painter.BeginPath();
                        painter.MoveTo(from);
                        painter.LineTo(to);
                        painter.Stroke();
                    }
                    else
                    {
                        // ドット
                        float dotR = (1.5f + 1.5f * dir) * scale * (1f - fadeT * 0.5f);
                        var pos = new Vector2(cx + cos * outerR, cy + sin * outerR);
                        painter.fillColor = particleColor;
                        painter.BeginPath();
                        painter.Arc(pos, dotR, 0f, 360f);
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
            }

            // ── 完了判定 ──
            if (elapsed >= TotalDuration && _playing && !_finished)
            {
                _finished = true;
                Stop();
                schedule.Execute(() => Finished?.Invoke());
            }
        }

        // ── イージング関数 ──

        static float EaseOutCubic(float t)
        {
            t = 1f - t;
            return 1f - t * t * t;
        }

        static float EaseInCubic(float t) => t * t * t;

        static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return 1f + t * t * ((c + 1f) * t + c);
        }

        /// <summary>オーバーシュート量 (0→0, 0.5→peak, 1→0)</summary>
        static float Overshoot(float t) => Mathf.Sin(t * Mathf.PI) * Mathf.Clamp01(1f - t * 0.5f);

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
