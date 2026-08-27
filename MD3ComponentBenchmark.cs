using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// MD3SDK の全コンポーネントを一括で計測するベンチマーク。
    ///
    /// 測るもの (1 インスタンスあたり):
    ///   - 生成    … コンストラクタの実行時間
    ///   - レイアウト … 幅ごとの Yoga レイアウト時間
    ///   - 要素数   … 生成される VisualElement の総数 (自分自身を含む)
    ///   - 確保量   … 生成 1 回あたりのマネージドヒープ増加量
    ///
    /// レイアウトは <c>EditorPanel.ValidateLayout()</c> を直接呼んで同期実行する。
    /// UI Toolkit のレイアウトは差分更新なので、計測対象を足した直後に呼べば
    /// その部分木ぶんだけが計算される。空のホストで同じことをした時間を
    /// ベースラインとして差し引く。
    ///
    /// 幅を複数取るのは、狭い幅で急に重くなるコンポーネントを見つけるため。
    /// 表の「比」列は レイアウト(最小幅) / レイアウト(最大幅)。ここが大きいものが
    /// 狭いウィンドウで効いてくる。
    ///
    /// 注意: 初回生成は USS の解決やグリフの焼き込みで桁違いに遅い。
    /// 各コンポーネントで必ずウォームアップを 1 回行ってから計測する。
    /// </summary>
    public class MD3ComponentBenchmark : EditorWindow
    {
        const string CsvName = "MD3SDK_ComponentBenchmark.csv";
        const string LogName = "MD3SDK_ComponentBenchmark.log";

        // 出力はプロジェクト直下ではなく Logs/ に置く。
        // Logs/ は Unity 標準の .gitignore に含まれるので、消費側のリポジトリを汚さない。
        static string OutputDir
        {
            get
            {
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
                try { Directory.CreateDirectory(dir); } catch { /* 書けなければ呼び出し側で握る */ }
                return dir;
            }
        }

        static string LogPath => Path.Combine(OutputDir, LogName);

        /// <summary>
        /// 1 行ごとに開いて閉じて書く。計測中にハングして強制終了しても、
        /// 最後の "measuring" 行がどのコンポーネントで止まったかを名指しする。
        /// </summary>
        static void LogLine(string line)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line + Environment.NewLine);
            }
            catch { /* ログが書けなくても計測は続ける */ }
        }

        [MenuItem("Window/紫陽花広場/MD3 SDK Diagnostics/Component Benchmark")]
        public static void Open()
        {
            var w = GetWindow<MD3ComponentBenchmark>("MD3 Benchmark");
            w.minSize = new Vector2(720f, 420f);
        }

        // ── 設定 ──

        int _iterations = 20;
        string _widthsText = "100, 300, 800";
        string _filter = "";

        /// <summary>
        /// レイアウト用コンテナに共通の中身を入れてから測るか。
        /// 空のまま測るとレイアウト時間がほぼ 0 になり、コンテナ同士の比較にならない。
        /// 入れた行は名前に "+中身" が付く。
        /// </summary>
        bool _fillEmpty = true;

        /// <summary>
        /// 確保量も測るか。<c>GC.GetTotalMemory(true)</c> は完全な GC を強制するので、
        /// ヒープの大きいプロジェクトではコンポーネント 1 つあたり数百 ms かかる
        /// (全 66 件で数分)。時間の大半はこれなので既定は off。
        /// </summary>
        bool _measureAlloc = false;

        const int PayloadCount = 6;

        /// <summary>
        /// 中身を入れる対象。「子を持たない = コンテナ」で判定すると
        /// MD3Divider や MD3Spinner のような葉コンポーネントまで巻き込み、
        /// 実際には存在しない使い方を測ってしまうため、明示的に列挙する。
        /// </summary>
        static readonly HashSet<string> LayoutContainers = new HashSet<string>(StringComparer.Ordinal)
        {
            "MD3Row", "MD3Column", "MD3Grid", "MD3Stack", "MD3Center",
            "MD3Constrained", "MD3ScrollColumn", "MD3Toolbar", "MD3Card", "MD3SplitPane",
        };

        // ── 結果 ──

        class Row
        {
            public string Name;
            public int Elements;
            public double CtorUs;
            public long AllocBytes;
            public double[] LayoutUs;   // _widths と同じ順
            public string Error;
            public bool Filled;

            /// <summary>
            /// 狭い幅 / 広い幅 のレイアウト時間比。広い方が 1µs 未満のときは
            /// 割り算がノイズを拡大するだけなので NaN を返して表では "-" にする。
            /// </summary>
            public double Ratio
            {
                get
                {
                    if (LayoutUs == null || LayoutUs.Length < 2) return double.NaN;
                    double wide = LayoutUs[LayoutUs.Length - 1];
                    if (wide < 1.0) return double.NaN;
                    return LayoutUs[0] / wide;
                }
            }
        }

        float[] _widths = { 100f, 300f, 800f };
        readonly List<Row> _rows = new List<Row>();
        readonly List<string> _skipped = new List<string>();
        VisualElement _host;
        VisualElement _resultsArea;
        Label _statusLabel;
        string _sortKey = "layout0";

        // ── UI ──

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var themeSheet = MD3Theme.LoadThemeStyleSheet();
            var compSheet = MD3Theme.LoadComponentsStyleSheet();
            if (themeSheet != null && !root.styleSheets.Contains(themeSheet))
                root.styleSheets.Add(themeSheet);
            if (compSheet != null && !root.styleSheets.Contains(compSheet))
                root.styleSheets.Add(compSheet);
            (MD3Theme.Default ?? MD3Theme.Auto()).ApplyTo(root);

            // 計測用のホスト。画面外に置くが display は none にしない
            // (none にするとレイアウトそのものが省略され、計測にならない)。
            _host = new VisualElement { name = "benchmark-host" };
            _host.style.position = Position.Absolute;
            _host.style.left = -30000f;
            _host.style.top = 0f;
            _host.style.overflow = Overflow.Hidden;
            root.Add(_host);

            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.flexWrap = Wrap.Wrap;
            bar.style.paddingLeft = 8;
            bar.style.paddingRight = 8;
            bar.style.paddingTop = 8;
            bar.style.paddingBottom = 8;
            bar.style.flexShrink = 0;
            root.Add(bar);

            var iterField = new IntegerField("繰り返し") { value = _iterations };
            iterField.style.width = 140;
            iterField.RegisterValueChangedCallback(e => _iterations = Mathf.Clamp(e.newValue, 1, 500));
            bar.Add(iterField);

            var widthField = new TextField("幅 (px)") { value = _widthsText };
            widthField.style.width = 220;
            widthField.RegisterValueChangedCallback(e => _widthsText = e.newValue);
            bar.Add(widthField);

            var filterField = new TextField("絞り込み") { value = _filter };
            filterField.style.width = 200;
            filterField.RegisterValueChangedCallback(e => _filter = e.newValue);
            bar.Add(filterField);

            var fillToggle = new Toggle("空のコンテナに中身を入れる") { value = _fillEmpty };
            fillToggle.style.width = 220;
            fillToggle.RegisterValueChangedCallback(e => _fillEmpty = e.newValue);
            bar.Add(fillToggle);

            var allocToggle = new Toggle("確保量も測る (遅い)") { value = _measureAlloc };
            allocToggle.style.width = 190;
            allocToggle.RegisterValueChangedCallback(e => _measureAlloc = e.newValue);
            bar.Add(allocToggle);

            var runBtn = new Button(() => EditorApplication.delayCall += RunBenchmark) { text = "計測する" };
            runBtn.style.height = 24;
            bar.Add(runBtn);

            var csvBtn = new Button(ExportCsv) { text = "CSV 出力" };
            csvBtn.style.height = 24;
            bar.Add(csvBtn);

            _statusLabel = new Label("「計測する」で全コンポーネントを計測します。");
            _statusLabel.style.paddingLeft = 8;
            _statusLabel.style.paddingBottom = 4;
            _statusLabel.style.flexShrink = 0;
            root.Add(_statusLabel);

            _resultsArea = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _resultsArea.style.flexGrow = 1;
            root.Add(_resultsArea);

            if (_rows.Count > 0) BuildResultsTable();
        }

        // ── 計測 ──

        /// <summary>
        /// _statusLabel は _host と同じく CreateGUI で作られる。つまり _host が null の
        /// ときは _statusLabel も null なので、素の代入では案内を出す代わりに
        /// NullReferenceException になる。
        /// </summary>
        void SetStatus(string message)
        {
            if (_statusLabel != null) _statusLabel.text = message;
            else Debug.LogWarning("[MD3Benchmark] " + message);
        }

        void RunBenchmark()
        {
            if (_host == null || _host.panel == null)
            {
                SetStatus("ホストが panel に載っていません。ウィンドウを開き直してください。");
                return;
            }

            _widths = ParseWidths(_widthsText);
            if (_widths.Length == 0)
            {
                SetStatus("幅を 1 つ以上指定してください (例: 100, 300, 800)。");
                return;
            }

            var doLayout = ResolveLayoutInvoker(_host.panel);
            if (doLayout == null)
            {
                SetStatus("ValidateLayout() を解決できませんでした。この Unity では計測できません。");
                return;
            }

            var factories = CollectFactories(out var skipped);
            _rows.Clear();
            _skipped.Clear();
            _skipped.AddRange(skipped);

            var sw = new Stopwatch();
            int done = 0;
            LogLine("==== run start: " + factories.Count + " components, iterations=" + _iterations
                + ", widths=" + string.Join("/", _widths.Select(w => w.ToString("F0")))
                + ", alloc=" + _measureAlloc + ", fill=" + _fillEmpty + " ====");
            try
            {
                foreach (var kv in factories)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "MD3 Component Benchmark",
                            kv.Key + "  (" + (done + 1) + "/" + factories.Count + ")",
                            (float)done / Mathf.Max(1, factories.Count)))
                        break;
                    done++;

                    var row = new Row { Name = kv.Key, LayoutUs = new double[_widths.Length] };
                    _rows.Add(row);

                    LogLine("measuring " + kv.Key);
                    try
                    {
                        MeasureOne(WithPayload(kv.Value, row), row, doLayout, sw);
                        LogLine("   ok      " + kv.Key
                            + "  elements=" + row.Elements
                            + "  layout=[" + string.Join(", ", row.LayoutUs.Select(x => x.ToString("F1"))) + "]us");
                    }
                    catch (Exception ex)
                    {
                        row.Error = ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message;
                        LogLine("   FAILED  " + kv.Key + "  " + row.Error);
                    }
                    finally
                    {
                        _host.Clear();
                        doLayout();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _host.Clear();
                doLayout();
            }

            LogLine("==== run end ====");
            SetStatus(_rows.Count + " コンポーネント計測 / " + _skipped.Count + " 件スキップ"
                + "  (繰り返し " + _iterations + " 回, 幅 " + string.Join("/", _widths.Select(w => w.ToString("F0"))) + ")");
            BuildResultsTable();
        }

        void MeasureOne(Func<VisualElement> factory, Row row, Action doLayout, Stopwatch sw)
        {
            // ── ウォームアップ ──
            // 初回は USS の解決・アイコングリフの焼き込みで桁違いに遅い。
            // これを計測に混ぜると「最初に測ったコンポーネントが一番遅い」という
            // 順番依存の嘘が出る。必ず捨て 1 回を挟む。
            for (int i = 0; i < 2; i++)
            {
                var warm = factory();
                _host.Add(warm);
                doLayout();
                _host.Remove(warm);
            }
            doLayout();

            // ── 要素数 ──
            var probe = factory();
            row.Elements = CountElements(probe);

            // ── 確保量 (任意) ──
            // 完全な GC を強制するので、ヒープの大きいプロジェクトでは非常に遅い。
            if (_measureAlloc)
            {
                // 保持用の配列は計測の「前」に確保する。あとで確保すると
                // 配列そのもの (8 * _iterations バイト) が結果に混ざる。
                var allocSet = new VisualElement[_iterations];
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long before = GC.GetTotalMemory(true);
                for (int i = 0; i < _iterations; i++) allocSet[i] = factory();
                // after も強制 GC 付きで読む。false のままだとコンストラクタが出した
                // 一時ゴミが未回収のまま加算され、「1 インスタンスが生き残らせる大きさ」
                // ではなく「実行中に触れた量」を測ってしまう。
                long after = GC.GetTotalMemory(true);
                row.AllocBytes = Math.Max(0, (after - before) / _iterations);
                GC.KeepAlive(allocSet);
            }
            else
            {
                row.AllocBytes = -1;
            }

            // ── 生成時間 ──
            // ファクトリは ConstructorInfo.Invoke + OptionalParamBinding を通るので、
            // 呼び出し 1 回あたり 1-3us のリフレクション費用が乗る。差し引かないと、
            // MD3Spacer のように実作業が 1us 未満のコンポーネントは測定値のほぼ全部が
            // リフレクション費用になり、表の順位が意味を失う。
            // 素の VisualElement を同じ経路で作った時間をベースラインにする。
            var baselineMade = new VisualElement[_iterations];
            sw.Restart();
            for (int i = 0; i < _iterations; i++) baselineMade[i] = BaselineFactory();
            sw.Stop();
            double ctorBaseline = sw.Elapsed.TotalMilliseconds;

            var made = new VisualElement[_iterations];
            sw.Restart();
            for (int i = 0; i < _iterations; i++) made[i] = factory();
            sw.Stop();
            GC.KeepAlive(made);
            GC.KeepAlive(baselineMade);
            row.CtorUs = Math.Max(0, sw.Elapsed.TotalMilliseconds - ctorBaseline) * 1000.0 / _iterations;

            // ── レイアウト時間 (幅ごと) ──
            for (int wi = 0; wi < _widths.Length; wi++)
            {
                _host.style.width = _widths[wi];
                doLayout();

                // 空のホストで同じ操作をしたときの時間をベースラインにする。
                double baseline = 0;
                for (int i = 0; i < _iterations; i++)
                {
                    var filler = new VisualElement();
                    _host.Add(filler);
                    sw.Restart();
                    doLayout();
                    sw.Stop();
                    baseline += sw.Elapsed.TotalMilliseconds;
                    _host.Remove(filler);
                    doLayout();
                }

                // 幅ごとに新しいインスタンスを使う。
                // 同じインスタンスを幅をまたいで使い回すと、2 巡目以降は
                // レイアウト結果がキャッシュされて速く見え、最初に測った幅
                // (幅は昇順なので最小幅) だけが不当に遅く出る。
                // それでは「比 (狭/広)」が水増しされ、狭幅で重いものを
                // 探すという目的そのものを損なう。
                var set = new VisualElement[_iterations];
                for (int i = 0; i < _iterations; i++) set[i] = factory();

                double total = 0;
                for (int i = 0; i < _iterations; i++)
                {
                    _host.Add(set[i]);
                    sw.Restart();
                    doLayout();
                    sw.Stop();
                    total += sw.Elapsed.TotalMilliseconds;
                    _host.Remove(set[i]);
                    doLayout();
                }

                row.LayoutUs[wi] = Math.Max(0, (total - baseline)) * 1000.0 / _iterations;
            }
        }

        /// <summary>
        /// 生成物が空なら共通の中身を足すファクトリに包む。
        /// 1 度だけ試作して空かどうかを判定し、その結果を row.Filled に残す。
        /// </summary>
        Func<VisualElement> WithPayload(Func<VisualElement> factory, Row row)
        {
            if (!_fillEmpty) return factory;

            int tick = row.Name.IndexOf('<');
            var bare = tick > 0 ? row.Name.Substring(0, tick) : row.Name;
            if (!LayoutContainers.Contains(bare)) return factory;

            bool empty;
            try { empty = factory().childCount == 0; }
            catch { return factory; }

            if (!empty) return factory;
            row.Filled = true;
            return () =>
            {
                var v = factory();
                for (int i = 0; i < PayloadCount; i++)
                    v.Add(new Label("Item " + i));
                return v;
            };
        }

        static Func<VisualElement> s_baselineFactory;

        /// <summary>計測対象と同じリフレクション経路で素の VisualElement を作るファクトリ。</summary>
        static Func<VisualElement> BaselineFactory
        {
            get
            {
                if (s_baselineFactory != null) return s_baselineFactory;
                var ctor = typeof(VisualElement).GetConstructor(Type.EmptyTypes);
                var argv = new object[0];
                return s_baselineFactory = () => (VisualElement)ctor.Invoke(
                    BindingFlags.OptionalParamBinding, null, argv, CultureInfo.InvariantCulture);
            }
        }

        static int CountElements(VisualElement ve)
        {
            int n = 1;
            for (int i = 0; i < ve.hierarchy.childCount; i++)
                n += CountElements(ve.hierarchy[i]);
            return n;
        }

        /// <summary>panel.ValidateLayout() をリフレクションで叩けるようにする。</summary>
        static Action ResolveLayoutInvoker(IPanel panel)
        {
            var m = panel.GetType().GetMethod("ValidateLayout",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (m == null) return null;
            return () => m.Invoke(panel, null);
        }

        float[] ParseWidths(string text)
        {
            var list = new List<float>();
            foreach (var part in (text ?? "").Split(',', ';', ' '))
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0)
                    list.Add(v);
            }
            list.Sort();
            return list.ToArray();
        }

        // ── コンポーネントの収集 ──

        /// <summary>
        /// SDK アセンブリの public な VisualElement 派生型をすべて拾い、
        /// 引数なし (または全引数が省略可能) のコンストラクタでファクトリを作る。
        /// </summary>
        SortedDictionary<string, Func<VisualElement>> CollectFactories(out List<string> skipped)
        {
            skipped = new List<string>();
            var result = new SortedDictionary<string, Func<VisualElement>>(StringComparer.Ordinal);

            var asm = typeof(MD3ComponentBenchmark).Assembly;
            var veType = typeof(VisualElement);

            foreach (var t0 in asm.GetTypes())
            {
                if (!t0.IsPublic || t0.IsAbstract) continue;
                if (!veType.IsAssignableFrom(t0)) continue;
                if (t0 == typeof(MD3ComponentBenchmark)) continue;

                var t = t0;
                if (t.IsGenericTypeDefinition)
                {
                    // ジェネリックは string で閉じて代表値を測る
                    var args = t.GetGenericArguments();
                    if (args.Length != 1) { skipped.Add(t.Name + " (ジェネリック引数が 1 個ではない)"); continue; }
                    try { t = t.MakeGenericType(typeof(string)); }
                    catch { skipped.Add(t.Name + " (string で閉じられない)"); continue; }
                }

                if (!string.IsNullOrEmpty(_filter) &&
                    t.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var ctors = t.GetConstructors();
                var ctor = ctors.FirstOrDefault(c => c.GetParameters().Length == 0)
                           ?? ctors.FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
                if (ctor == null)
                {
                    skipped.Add(t.Name + " (引数なしで生成できない)");
                    continue;
                }

                var argv = ctor.GetParameters().Select(_ => Type.Missing).ToArray();
                var captured = ctor;
                result[Nice(t)] = () => (VisualElement)captured.Invoke(
                    BindingFlags.OptionalParamBinding, null, argv, CultureInfo.InvariantCulture);
            }
            return result;
        }

        static string Nice(Type t)
        {
            if (!t.IsGenericType) return t.Name;
            var name = t.Name;
            int tick = name.IndexOf('`');
            if (tick > 0) name = name.Substring(0, tick);
            return name + "<" + string.Join(",", t.GetGenericArguments().Select(a => a.Name)) + ">";
        }

        // ── 結果表示 ──

        void BuildResultsTable()
        {
            _resultsArea.Clear();

            var header = MakeRow(true);
            header.Add(MakeCell("コンポーネント", 240, true, () => SetSort("name")));
            header.Add(MakeCell("要素", 60, true, () => SetSort("elements")));
            header.Add(MakeCell("生成 µs", 90, true, () => SetSort("ctor")));
            header.Add(MakeCell("確保 B", 90, true, () => SetSort("alloc")));
            for (int i = 0; i < _widths.Length; i++)
            {
                int idx = i;
                header.Add(MakeCell(_widths[i].ToString("F0") + "px µs", 100, true, () => SetSort("layout" + idx)));
            }
            if (_widths.Length >= 2)
                header.Add(MakeCell("比 (狭/広)", 90, true, () => SetSort("ratio")));
            _resultsArea.Add(header);

            foreach (var r in SortRows())
            {
                var line = MakeRow(false);
                line.Add(MakeCell(r.Name + (r.Filled ? "  +中身" : ""), 240, false, null));
                if (r.Error != null)
                {
                    var err = MakeCell("計測できず: " + r.Error, 600, false, null);
                    err.style.color = new Color(0.9f, 0.45f, 0.45f);
                    line.Add(err);
                    _resultsArea.Add(line);
                    continue;
                }
                line.Add(MakeCell(r.Elements.ToString(), 60, false, null));
                line.Add(MakeCell(r.CtorUs.ToString("F1"), 90, false, null));
                line.Add(MakeCell(r.AllocBytes < 0 ? "-" : r.AllocBytes.ToString(), 90, false, null));
                for (int i = 0; i < _widths.Length; i++)
                    line.Add(MakeCell(r.LayoutUs[i].ToString("F1"), 100, false, null));
                if (_widths.Length >= 2)
                {
                    bool has = !double.IsNaN(r.Ratio);
                    var c = MakeCell(has ? r.Ratio.ToString("F2") : "-", 90, false, null);
                    if (has && r.Ratio >= 2.0) c.style.color = new Color(0.95f, 0.6f, 0.3f);
                    line.Add(c);
                }
                _resultsArea.Add(line);
            }

            if (_skipped.Count > 0)
            {
                var note = new Label("スキップ: " + string.Join(" / ", _skipped));
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.paddingLeft = 8;
                note.style.paddingTop = 8;
                note.style.opacity = 0.7f;
                _resultsArea.Add(note);
            }
        }

        void SetSort(string key)
        {
            _sortKey = key;
            BuildResultsTable();
        }

        IEnumerable<Row> SortRows()
        {
            var ok = _rows.Where(r => r.Error == null).ToList();
            var bad = _rows.Where(r => r.Error != null).ToList();

            Comparison<Row> cmp;
            if (_sortKey == "name") cmp = (a, b) => string.CompareOrdinal(a.Name, b.Name);
            else if (_sortKey == "elements") cmp = (a, b) => b.Elements.CompareTo(a.Elements);
            else if (_sortKey == "ctor") cmp = (a, b) => b.CtorUs.CompareTo(a.CtorUs);
            else if (_sortKey == "alloc") cmp = (a, b) => b.AllocBytes.CompareTo(a.AllocBytes);
            else if (_sortKey == "ratio")
                cmp = (a, b) =>
                {
                    double x = double.IsNaN(a.Ratio) ? double.NegativeInfinity : a.Ratio;
                    double y = double.IsNaN(b.Ratio) ? double.NegativeInfinity : b.Ratio;
                    return y.CompareTo(x);
                };
            else
            {
                int idx = 0;
                if (_sortKey.StartsWith("layout"))
                    int.TryParse(_sortKey.Substring("layout".Length), out idx);
                idx = Mathf.Clamp(idx, 0, _widths.Length - 1);
                cmp = (a, b) => b.LayoutUs[idx].CompareTo(a.LayoutUs[idx]);
            }
            ok.Sort(cmp);
            return ok.Concat(bad);
        }

        static VisualElement MakeRow(bool header)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 0;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1f, 1f, 1f, header ? 0.25f : 0.06f);
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            return row;
        }

        static Label MakeCell(string text, float width, bool header, Action onClick)
        {
            var l = new Label(text);
            l.style.width = width;
            l.style.flexShrink = 0;
            l.style.paddingLeft = 6;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            l.style.overflow = Overflow.Hidden;
            if (header)
            {
                l.style.unityFontStyleAndWeight = FontStyle.Bold;
                if (onClick != null)
                {
                    l.style.cursor = new StyleCursor(StyleKeyword.Initial);
                    l.RegisterCallback<ClickEvent>(_ => onClick());
                }
            }
            return l;
        }

        // ── CSV ──

        void ExportCsv()
        {
            if (_rows.Count == 0)
            {
                SetStatus("先に計測してください。");
                return;
            }

            var path = Path.Combine(OutputDir, CsvName);
            var sb = new System.Text.StringBuilder();
            sb.Append("component,payload_added,elements,ctor_us,alloc_bytes");
            foreach (var w in _widths) sb.Append(",layout_us_" + w.ToString("F0") + "px");
            if (_widths.Length >= 2) sb.Append(",ratio_narrow_over_wide");
            sb.Append(",error");
            sb.AppendLine();

            foreach (var r in SortRows())
            {
                sb.Append(Csv(r.Name)).Append(',');
                sb.Append(r.Filled ? "yes" : "no").Append(',');
                sb.Append(r.Error == null ? r.Elements.ToString() : "").Append(',');
                sb.Append(r.Error == null ? r.CtorUs.ToString("F2", CultureInfo.InvariantCulture) : "").Append(',');
                sb.Append(r.Error == null && r.AllocBytes >= 0 ? r.AllocBytes.ToString() : "");
                for (int i = 0; i < _widths.Length; i++)
                {
                    sb.Append(',');
                    if (r.Error == null) sb.Append(r.LayoutUs[i].ToString("F2", CultureInfo.InvariantCulture));
                }
                if (_widths.Length >= 2)
                {
                    sb.Append(',');
                    if (r.Error == null && !double.IsNaN(r.Ratio))
                        sb.Append(r.Ratio.ToString("F3", CultureInfo.InvariantCulture));
                }
                sb.Append(',').Append(Csv(r.Error ?? ""));
                sb.AppendLine();
            }

            try
            {
                File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
                SetStatus("CSV を書きました: " + path);
                Debug.Log("[MD3Benchmark] CSV: " + path);
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception e)
            {
                SetStatus("CSV を書けません: " + e.Message);
            }
        }

        static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
