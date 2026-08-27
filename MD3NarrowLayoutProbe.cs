using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// 幅 100px 前後でレイアウトさせるとメインスレッドが戻ってこなくなる問題
    /// (issue #4) の犯人を、1 回の強制終了で特定するための検査ウィンドウ。
    ///
    /// 使い方:
    ///   1. Window/紫陽花広場/Unity Material Design 3 SDK/Diagnostics/Narrow Layout Probe を実行する
    ///   2. ハングしなければ、もう一度同じメニューを実行する (次のステップへ自動で進む)
    ///   3. ハングしたら Unity を強制終了する
    ///   4. 再起動して「ログを開く」を実行する。
    ///      "START" だけがあって対応する "SURVIVED" が無いステップが犯人
    ///
    /// 仕掛け: ステップ番号は「構築の前に」進めて永続化する。だからハングして
    /// 強制終了しても、次に開いたときは同じステップを踏み直さず先へ進める。
    /// ログは 1 行ごとに開いて閉じて書くので、強制終了してもディスクに残る。
    ///
    /// 各ステップは幅 100px の浮動ウィンドウを 1 つ作り、そこに 1 種類だけ置く。
    /// ステップ 1 と 2 はコンポーネントを一切使わず、素の Label に日本語を入れて
    /// 折り返させる。ここで差が出れば原因はフォント/テキスト計測の側にあり、
    /// MD3 コンポーネントの flex 設定ではない、と切り分けられる。
    /// </summary>
    public class MD3NarrowLayoutProbe : EditorWindow
    {
        // warm と cold でステップ番号を分ける。
        // 共有すると、warm を最後まで回したあとに cold を実行しても
        // 「全ステップ完走」ダイアログが出るだけで 1 度も開かず、
        // 実際に issue #4 の条件を再現するモードが走らない。
        // 交互に実行した場合も、1 つのカウンタが進むだけで各モードは
        // 半分しか踏んでいないのにログ上は完走したように見える。
        const string StepKeyPrefix = "MD3SDK.NarrowLayoutProbe.NextStep";

        static string StepKey(bool cold) => StepKeyPrefix + (cold ? ".cold" : ".warm");
        const float ProbeHeight = 100f;

        // warm: 広いところから段階的に絞る。各段の直前にログを刻むので、
        // ハングしても「どの幅で落ちたか」が残る。
        static readonly float[] WarmRamp =
            { 400f, 300f, 220f, 180f, 150f, 130f, 115f, 105f, 100f, 96f, 90f, 80f };

        // cold: 一度も広い幅でレイアウトさせずに、いきなり 100px で開く。
        // issue #4 の報告はこちら (EditorWindow の既定 minSize が 100x100 なので、
        // minSize を設定していないウィンドウは初回レイアウトが必ずこの幅になる)。
        // warm と cold で結果が変わるなら、原因は「初回レイアウトの状態」にある。
        static readonly float[] ColdRamp = { 100f };

        // 日本語は空白が無いので、狭幅では必ず文字単位の折り返しに落ちる。
        const string JapaneseText =
            "このウィンドウは幅百ピクセルで折り返しの検査をしています。" +
            "日本語には単語区切りの空白が無いため、狭い幅では文字単位の" +
            "折り返しに落ちます。テキスト計測が収束するかどうかを見ます。";

        struct Step
        {
            public string Name;
            public bool ApplyMd3Theme;
            public Action<VisualElement> Build;

            public Step(string name, bool applyMd3Theme, Action<VisualElement> build)
            {
                Name = name;
                ApplyMd3Theme = applyMd3Theme;
                Build = build;
            }
        }

        static List<Step> BuildSteps()
        {
            return new List<Step>
            {
                // ── コンポーネントを使わない対照実験 ──
                new Step("00 baseline-ascii-nowrap", false, root =>
                {
                    var l = new Label("Hello");
                    l.style.whiteSpace = WhiteSpace.NoWrap;
                    root.Add(l);
                }),
                new Step("01 jp-wrap-DEFAULT-font", false, root =>
                {
                    // MD3 のフォントを一切当てない。エディタ既定フォントで折り返す。
                    var l = new Label(JapaneseText);
                    l.style.whiteSpace = WhiteSpace.Normal;
                    root.Add(l);
                }),
                new Step("02 jp-wrap-MD3-font", true, root =>
                {
                    // 01 と同じ内容を MD3 の Static main + memory-only Dynamic fallback で。
                    // 01 が通って 02 が止まるなら、原因はフォント経路。
                    var l = new Label(JapaneseText);
                    l.style.whiteSpace = WhiteSpace.Normal;
                    root.Add(l);
                }),
                new Step("03 jp-wrap-MD3-font-in-scrollview", true, root =>
                {
                    // 縦スクロールバーの出し入れが折り返し幅を 12px 動かす。
                    // 幅 100px ではこれが 12% にあたり、収束判定が振動しやすい。
                    var sv = new ScrollView(ScrollViewMode.Vertical);
                    sv.style.flexGrow = 1;
                    for (int i = 0; i < 22; i++)
                    {
                        var l = new Label(JapaneseText);
                        l.style.whiteSpace = WhiteSpace.Normal;
                        sv.Add(l);
                    }
                    root.Add(sv);
                }),

                // ── MD3 コンポーネント単体 ──
                new Step("04 icon-row-nogap", true, root =>
                {
                    var row = new MD3Row();
                    for (int i = 0; i < 9; i++)
                        row.Add(new MD3IconButton(MD3Icon.Star));
                    row.Add(new MD3Spacer());
                    root.Add(row);
                }),
                new Step("05 icon-row-gap", true, root =>
                {
                    var row = new MD3Row(8f);
                    for (int i = 0; i < 9; i++)
                        row.Add(new MD3IconButton(MD3Icon.Star));
                    row.Add(new MD3Spacer());
                    root.Add(row);
                }),
                new Step("06 chip-row", true, root =>
                {
                    var row = new MD3Row(8f);
                    row.Add(new MD3Chip("チップA"));
                    row.Add(new MD3Chip("チップB"));
                    row.Add(new MD3Spacer());
                    row.Add(new MD3Button("送信"));
                    root.Add(row);
                }),
                new Step("07 textfield-row", true, root =>
                {
                    var row = new MD3Row(8f);
                    row.Add(new MD3IconButton(MD3Icon.Add));
                    row.Add(new MD3TextField("入力"));
                    row.Add(new MD3IconButton(MD3Icon.Send));
                    root.Add(row);
                }),
                new Step("08 linear-progress", true, root =>
                {
                    root.Add(new MD3LinearProgress(0.5f));
                }),
                new Step("09 foldout-rows-jp", true, root =>
                {
                    var sv = new ScrollView(ScrollViewMode.Vertical);
                    sv.style.flexGrow = 1;
                    var fold = new MD3Foldout("履歴", true);
                    for (int i = 0; i < 22; i++)
                    {
                        var row = new MD3Row();
                        var l = new Label(JapaneseText);
                        l.style.whiteSpace = WhiteSpace.Normal;
                        row.Add(l);
                        fold.Content.Add(row);
                    }
                    sv.Add(fold);
                    root.Add(sv);
                }),

                // ── 全部乗せ (報告されたツリーの再現) ──
                new Step("10 all-combined", true, root =>
                {
                    var toolbar = new MD3Row(4f);
                    for (int i = 0; i < 9; i++)
                        toolbar.Add(new MD3IconButton(MD3Icon.Star));
                    toolbar.Add(new MD3Spacer());
                    root.Add(toolbar);

                    var sv = new ScrollView(ScrollViewMode.Vertical);
                    sv.style.flexGrow = 1;
                    var fold = new MD3Foldout("履歴", true);
                    for (int i = 0; i < 22; i++)
                    {
                        var row = new MD3Row();
                        var l = new Label(JapaneseText);
                        l.style.whiteSpace = WhiteSpace.Normal;
                        row.Add(l);
                        fold.Content.Add(row);
                    }
                    sv.Add(fold);
                    root.Add(sv);

                    root.Add(new MD3LinearProgress(0.5f));

                    var chips = new MD3Row(8f);
                    chips.Add(new MD3Chip("チップA"));
                    chips.Add(new MD3Chip("チップB"));
                    chips.Add(new MD3Spacer());
                    chips.Add(new MD3Button("送信"));
                    root.Add(chips);

                    var input = new MD3Row(8f);
                    input.Add(new MD3IconButton(MD3Icon.Add));
                    input.Add(new MD3TextField("入力"));
                    input.Add(new MD3IconButton(MD3Icon.Send));
                    root.Add(input);
                }),
            };
        }

        // ── ログ ──

        // 出力はプロジェクト直下ではなく Logs/ に置く。
        // Logs/ は Unity 標準の .gitignore に含まれるので、消費側のリポジトリを汚さない。
        static string LogPath
        {
            get
            {
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
                try { Directory.CreateDirectory(dir); } catch { /* 書けなければ Log 側で握る */ }
                return Path.Combine(dir, "MD3SDK_LayoutProbe.log");
            }
        }

        /// <summary>1 行ごとに開いて閉じる。強制終了してもディスクに残す。</summary>
        static void Log(string line)
        {
            var stamped = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line;
            try { File.AppendAllText(LogPath, stamped + Environment.NewLine); }
            catch (Exception e) { Debug.LogWarning("[MD3Probe] ログを書けません: " + e.Message); }
            Debug.Log("[MD3Probe] " + line);
        }

        // ── メニュー ──

        [MenuItem(MD3Menu.Diagnostics + "Narrow Layout Probe (warm: 400px から絞る)", false, MD3Menu.ProbeWarmPriority)]
        public static void RunNextStepWarm() { RunNextStep(false); }

        [MenuItem(MD3Menu.Diagnostics + "Narrow Layout Probe (cold: 100px で開く)", false, MD3Menu.ProbeColdPriority)]
        public static void RunNextStepCold() { RunNextStep(true); }

        static void RunNextStep(bool cold)
        {
            var steps = BuildSteps();
            var ramp = cold ? ColdRamp : WarmRamp;
            var stepKey = StepKey(cold);
            int step = EditorPrefs.GetInt(stepKey, 0);

            if (step >= steps.Count)
            {
                EditorUtility.DisplayDialog(
                    "Narrow Layout Probe",
                    (cold ? "cold" : "warm") + " モードの全 " + steps.Count + " ステップが完走しました。\n" +
                    "この構成では再現していません。\n\n" +
                    "ログ: " + LogPath,
                    "OK");
                return;
            }

            // ★ 構築の前に次のステップを確定させる。
            //    こうしないと、ハング → 強制終了 → 再実行 で同じステップを
            //    永久に踏み直すことになる。
            EditorPrefs.SetInt(stepKey, step + 1);

            var s = steps[step];
            Log("START  step=" + step + " (" + s.Name + ")  md3Theme=" + s.ApplyMd3Theme
                + "  mode=" + (cold ? "COLD" : "warm")
                + "  widthRamp=" + string.Join(",", Array.ConvertAll(ramp, x => x.ToString("F0"))));

            var w = CreateInstance<MD3NarrowLayoutProbe>();
            w.titleContent = new GUIContent("MD3 Probe " + step);
            w._stepIndex = step;
            w._step = s;
            w._ramp = ramp;
            w._cold = cold;

            // 狭い幅を通させる。minSize を先に下げないと position が効かない。
            // cold では ShowUtility の前にサイズを決め、一度も広い幅で
            // レイアウトさせない。
            w.minSize = new Vector2(1f, 1f);
            w.maxSize = new Vector2(4000f, 4000f);
            w.position = new Rect(200f, 200f, ramp[0], ProbeHeight);
            w.ShowUtility();
            w.position = new Rect(200f, 200f, ramp[0], ProbeHeight);
        }

        [MenuItem(MD3Menu.Diagnostics + "Narrow Layout Probe (ステップをリセット)", false, MD3Menu.ProbeResetPriority)]
        public static void ResetSteps()
        {
            EditorPrefs.SetInt(StepKey(false), 0);
            EditorPrefs.SetInt(StepKey(true), 0);
            Log("---- ステップをリセットしました (warm / cold 両方) ----");
        }

        [MenuItem(MD3Menu.Diagnostics + "Narrow Layout Probe (ログを開く)", false, MD3Menu.ProbeLogPriority)]
        public static void OpenLog()
        {
            if (!File.Exists(LogPath))
            {
                EditorUtility.DisplayDialog("Narrow Layout Probe",
                    "まだログがありません。\n" + LogPath, "OK");
                return;
            }
            EditorUtility.RevealInFinder(LogPath);
            Debug.Log("[MD3Probe] ログ: " + LogPath + "\n" + File.ReadAllText(LogPath));
        }

        // ── ウィンドウ本体 ──

        int _stepIndex = -1;
        Step _step;
        float[] _ramp;
        bool _cold;
        int _rampIndex = -1;

        void CreateGUI()
        {
            if (_step.Build == null) return; // ドメインリロード後の復元インスタンス

            var root = rootVisualElement;
            root.Clear();

            if (_step.ApplyMd3Theme)
            {
                var themeSheet = MD3Theme.LoadThemeStyleSheet();
                var compSheet = MD3Theme.LoadComponentsStyleSheet();
                if (themeSheet != null && !root.styleSheets.Contains(themeSheet))
                    root.styleSheets.Add(themeSheet);
                if (compSheet != null && !root.styleSheets.Contains(compSheet))
                    root.styleSheets.Add(compSheet);
                (MD3Theme.Default ?? MD3Theme.Auto()).ApplyTo(root);
            }

            Log("BUILD  step=" + _stepIndex + " (" + _step.Name + ")");
            _step.Build(root);
            Log("BUILT  step=" + _stepIndex + " — ここから幅を絞っていく");

            // 幅を 1 段ずつ絞る。tick が回ってきたということは
            // 直前の幅でレイアウトと描画が完走したということ。
            // だからログの最後の "WIDTH ... 開始" が犯人の幅になる。
            _rampIndex = 0;
            root.schedule.Execute(NextWidth).Every(300);
        }

        void NextWidth()
        {
            if (_ramp == null) { Close(); return; }

            if (_rampIndex > 0)
                Log("  ok    step=" + _stepIndex + " width=" + _ramp[_rampIndex - 1].ToString("F0")
                    + " を完走 (実測 " + rootVisualElement.layout.width.ToString("F0")
                    + "x" + rootVisualElement.layout.height.ToString("F0") + ")");

            if (_rampIndex >= _ramp.Length)
            {
                Log("SURVIVED step=" + _stepIndex + " (" + _step.Name + ") ["
                    + (_cold ? "COLD" : "warm") + "] — 全幅で完走");
                Close();
                return;
            }

            float w = _ramp[_rampIndex];
            _rampIndex++;
            Log("  ---> step=" + _stepIndex + " width=" + w.ToString("F0") + " 開始");
            var pos = position;
            position = new Rect(pos.x, pos.y, w, ProbeHeight);
        }
    }
}
