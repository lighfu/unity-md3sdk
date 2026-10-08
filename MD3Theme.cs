using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace AjisaiFlow.MD3SDK.Editor
{
    public interface IMD3Themeable
    {
        void RefreshTheme();
    }

    [InitializeOnLoad]
    public class MD3Theme
    {
        static MD3Theme()
        {
            RestoreDefault();
        }

        public Color Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer;
        public Color Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer;
        public Color Tertiary, OnTertiary, TertiaryContainer, OnTertiaryContainer;
        public Color Surface, OnSurface, SurfaceVariant, OnSurfaceVariant;
        public Color Outline, OutlineVariant;
        public Color Error, OnError, ErrorContainer, OnErrorContainer;
        public Color InverseSurface, InverseOnSurface, InversePrimary;
        public Color SurfaceContainerLowest, SurfaceContainerLow, SurfaceContainer, SurfaceContainerHigh, SurfaceContainerHighest;

        public bool IsDark { get; set; }

        /// <summary>Optional text font. Null uses the SDK-managed font.</summary>
        public Font TextFont { get; set; }

        /// <summary>Optional text FontAsset. Takes precedence over TextFont.</summary>
        public FontAsset TextFontAsset { get; set; }

        /// <summary>
        /// Creates an independent palette and font configuration. Font assets are shared.
        /// Clone Dark()/Light()/Auto() before editing their shared defaults.
        /// </summary>
        public MD3Theme Clone() => (MD3Theme)MemberwiseClone();

        static MD3Theme s_dark;
        static MD3Theme s_light;
        static MD3Theme s_default;
        // Weak keys retain scopes across detach/re-attach without retaining abandoned trees.
        static readonly ConditionalWeakTable<VisualElement, MD3Theme> s_appliedThemes = new();
        // スコープの外に置くポップアップが起点のテーマを引き継ぐための表。フォント更新の対象にはしない。
        static readonly ConditionalWeakTable<VisualElement, MD3Theme> s_linkedThemes = new();

        static Font s_font;
        static FontAsset s_fontAsset;

        /// <summary>
        /// 要素自身から VisualElement ツリーを遡ってテーマを解決する。
        /// カスタムテーマが登録されていればそれを優先、なければ CSS クラスからデフォルト。
        /// </summary>
        public static MD3Theme Resolve(VisualElement el)
        {
            var current = el;
            while (current != null)
            {
                if (s_appliedThemes.TryGetValue(current, out var custom) ||
                    s_linkedThemes.TryGetValue(current, out custom))
                    return custom;
                if (current.ClassListContains("md3-dark") || current.ClassListContains("md3-light"))
                    return current.ClassListContains("md3-dark") ? Dark() : Light();
                current = current.parent;
            }
            return s_default ?? Auto();
        }

        /// <summary>
        /// SDK レベルのデフォルトテーマを設定する。
        /// カスタムテーマが ApplyTo されていない要素は、Auto() の代わりにこのテーマを使用する。
        /// </summary>
        public static void SetDefault(MD3Theme theme)
        {
            s_default = theme;
            if (theme != null)
            {
                EditorPrefs.SetString("MD3SDK_DefaultSeedColor", $"#{ColorUtility.ToHtmlStringRGBA(s_defaultSeedColor)}");
                EditorPrefs.SetBool("MD3SDK_DefaultThemeIsDark", theme.IsDark);
                EditorPrefs.SetBool("MD3SDK_DefaultThemeEnabled", true);
            }
            else
            {
                EditorPrefs.DeleteKey("MD3SDK_DefaultSeedColor");
                EditorPrefs.DeleteKey("MD3SDK_DefaultThemeIsDark");
                EditorPrefs.SetBool("MD3SDK_DefaultThemeEnabled", false);
            }
        }

        /// <summary>
        /// SDK レベルのデフォルトテーマをクリアし、Auto() フォールバックに戻す。
        /// </summary>
        public static void ClearDefault()
        {
            s_default = null;
            s_defaultSeedColor = new Color(0.4f, 0.314f, 0.643f); // M3 baseline purple
            EditorPrefs.DeleteKey("MD3SDK_DefaultSeedColor");
            EditorPrefs.DeleteKey("MD3SDK_DefaultThemeIsDark");
            EditorPrefs.SetBool("MD3SDK_DefaultThemeEnabled", false);
        }

        /// <summary>
        /// 現在の SDK デフォルトテーマを返す。未設定なら null。
        /// </summary>
        public static MD3Theme Default => s_default;

        static Color s_defaultSeedColor = new Color(0.4f, 0.314f, 0.643f);

        /// <summary>デフォルトテーマのシードカラー。</summary>
        public static Color DefaultSeedColor
        {
            get => s_defaultSeedColor;
            set => s_defaultSeedColor = value;
        }

        /// <summary>
        /// シードカラーと明暗からデフォルトテーマを生成して設定する。
        /// </summary>
        public static void SetDefaultFromSeed(Color seedColor, bool isDark)
        {
            s_defaultSeedColor = seedColor;
            SetDefault(FromSeedColor(seedColor, isDark));
        }

        /// <summary>
        /// EditorPrefs から保存済みデフォルトテーマを復元する。
        /// エディタ起動時や domain reload 後に呼ばれる。
        /// </summary>
        public static void RestoreDefault()
        {
            if (!EditorPrefs.GetBool("MD3SDK_DefaultThemeEnabled", false)) return;
            var hex = EditorPrefs.GetString("MD3SDK_DefaultSeedColor", "");
            if (string.IsNullOrEmpty(hex)) return;
            if (!ColorUtility.TryParseHtmlString(hex, out var seed)) return;
            var isDark = EditorPrefs.GetBool("MD3SDK_DefaultThemeIsDark", true);
            s_defaultSeedColor = seed;
            s_default = FromSeedColor(seed, isDark);
        }

        public static MD3Theme Auto() => EditorGUIUtility.isProSkin ? Dark() : Light();

        public static MD3Theme Dark()
        {
            if (s_dark != null) return s_dark;
            s_dark = new MD3Theme
            {
                IsDark = true,
                Primary            = Hex("#D0BCFF"), OnPrimary            = Hex("#381E72"),
                PrimaryContainer   = Hex("#4F378B"), OnPrimaryContainer   = Hex("#EADDFF"),
                Secondary          = Hex("#CCC2DC"), OnSecondary          = Hex("#332D41"),
                SecondaryContainer = Hex("#4A4458"), OnSecondaryContainer = Hex("#E8DEF8"),
                Tertiary           = Hex("#EFB8C8"), OnTertiary           = Hex("#492532"),
                TertiaryContainer  = Hex("#633B48"), OnTertiaryContainer  = Hex("#FFD8E4"),
                Surface            = Hex("#1C1B1F"), OnSurface            = Hex("#E6E1E5"),
                SurfaceVariant     = Hex("#49454F"), OnSurfaceVariant     = Hex("#CAC4D0"),
                Outline            = Hex("#938F99"), OutlineVariant       = Hex("#49454F"),
                Error              = Hex("#F2B8B5"), OnError              = Hex("#601410"),
                ErrorContainer     = Hex("#8C1D18"), OnErrorContainer     = Hex("#F9DEDC"),
                InverseSurface     = Hex("#E6E1E5"), InverseOnSurface     = Hex("#313033"),
                InversePrimary     = Hex("#6750A4"),
                SurfaceContainerLowest = Hex("#0F0D13"),
                SurfaceContainerLow    = Hex("#211F26"),
                SurfaceContainer       = Hex("#252329"),
                SurfaceContainerHigh   = Hex("#2B2930"),
                SurfaceContainerHighest = Hex("#36343B"),
            };
            return s_dark;
        }

        public static MD3Theme Light()
        {
            if (s_light != null) return s_light;
            s_light = new MD3Theme
            {
                IsDark = false,
                Primary            = Hex("#6750A4"), OnPrimary            = Hex("#FFFFFF"),
                PrimaryContainer   = Hex("#EADDFF"), OnPrimaryContainer   = Hex("#21005D"),
                Secondary          = Hex("#625B71"), OnSecondary          = Hex("#FFFFFF"),
                SecondaryContainer = Hex("#E8DEF8"), OnSecondaryContainer = Hex("#1D192B"),
                Tertiary           = Hex("#7D5260"), OnTertiary           = Hex("#FFFFFF"),
                TertiaryContainer  = Hex("#FFD8E4"), OnTertiaryContainer  = Hex("#31111D"),
                Surface            = Hex("#FFFBFE"), OnSurface            = Hex("#1C1B1F"),
                SurfaceVariant     = Hex("#E7E0EC"), OnSurfaceVariant     = Hex("#49454F"),
                Outline            = Hex("#79747E"), OutlineVariant       = Hex("#CAC4D0"),
                Error              = Hex("#B3261E"), OnError              = Hex("#FFFFFF"),
                ErrorContainer     = Hex("#F9DEDC"), OnErrorContainer     = Hex("#410E0B"),
                InverseSurface     = Hex("#313033"), InverseOnSurface     = Hex("#F4EFF4"),
                InversePrimary     = Hex("#D0BCFF"),
                SurfaceContainerLowest = Hex("#FFFFFF"),
                SurfaceContainerLow    = Hex("#F7F2FA"),
                SurfaceContainer       = Hex("#F3EDF7"),
                SurfaceContainerHigh   = Hex("#ECE6F0"),
                SurfaceContainerHighest = Hex("#E6E0E9"),
            };
            return s_light;
        }

        /// <summary>
        /// Generate a complete MD3 theme from a single seed color.
        /// All 25 color roles are derived using HCT tonal palettes.
        /// </summary>
        public static MD3Theme FromSeedColor(Color seedColor, bool isDark = true)
        {
            var palettes = MD3Palette.FromSeed(seedColor);
            return isDark ? MD3Palette.ToDarkScheme(palettes) : MD3Palette.ToLightScheme(palettes);
        }

        /// <summary>
        /// Applies a theme scope to a container or an individual component.
        /// Nested scopes retain their own themes. Reapply after editing color/font fields.
        /// </summary>
        public void ApplyTo(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            root.RemoveFromClassList("md3-dark");
            root.RemoveFromClassList("md3-light");
            root.AddToClassList(IsDark ? "md3-dark" : "md3-light");
            root.EnableInClassList("md3-theme-component", root is IMD3Themeable);
            s_appliedThemes.Remove(root);
            s_appliedThemes.Add(root, this);
            // 入れ子かどうかはパネルに追加されるまで決まらないので、追加時にフォントを決め直す。
            // 同じデリゲートは要素ごとに 1 回しか登録されない。
            root.RegisterCallback(s_onScopeAttached);

            // Set root surface colors inline (USS custom properties can't be set from C#)
            // Components own their backgrounds (for example, text remains transparent).
            if (!(root is IMD3Themeable))
                root.style.backgroundColor = Surface;
            root.style.color = OnSurface;
            ApplyTextFont(root);
            RefreshChildFonts(root);

            // Refresh all MD3 components in the tree
            RefreshDescendants(root);
        }

        /// <summary>
        /// Installs the SDK styles once, adds custom sheets last, and applies this theme.
        /// USS controls typography, shape and spacing; theme fields control stateful colors.
        /// </summary>
        public void ApplyTo(VisualElement root, params StyleSheet[] customStyleSheets)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            AddStyleSheet(root, LoadThemeStyleSheet());
            AddStyleSheet(root, LoadComponentsStyleSheet());
            if (customStyleSheets != null)
            {
                foreach (var sheet in customStyleSheets)
                {
                    if (sheet == null) continue;
                    // Moving an existing custom sheet to the end preserves its precedence.
                    if (root.styleSheets.Contains(sheet)) root.styleSheets.Remove(sheet);
                    root.styleSheets.Add(sheet);
                }
            }
            ApplyTo(root);
        }

        static void AddStyleSheet(VisualElement root, StyleSheet sheet)
        {
            if (sheet != null && !root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);
        }

        /// <summary>
        /// Removes the theme scope and its inline surface/text/font styles, then refreshes
        /// components using their inherited theme. Attached style sheets remain available.
        /// </summary>
        public static void ClearFrom(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (!s_appliedThemes.TryGetValue(root, out _) &&
                !root.ClassListContains("md3-dark") && !root.ClassListContains("md3-light"))
                return;

            s_appliedThemes.Remove(root);
            root.UnregisterCallback(s_onScopeAttached);
            root.RemoveFromClassList("md3-dark");
            root.RemoveFromClassList("md3-light");
            root.RemoveFromClassList("md3-theme-component");
            if (!(root is IMD3Themeable))
                root.style.backgroundColor = StyleKeyword.Null;
            root.style.color = StyleKeyword.Null;
            root.style.unityFontDefinition = StyleKeyword.Null;
            RefreshChildFonts(root);
            RefreshDescendants(root);
        }

        static readonly EventCallback<AttachToPanelEvent> s_onScopeAttached = OnScopeAttached;

        static void OnScopeAttached(AttachToPanelEvent evt)
        {
            var root = (VisualElement)evt.currentTarget;
            if (s_appliedThemes.TryGetValue(root, out var theme))
                theme.ApplyTextFont(root, keepCurrentOnFailure: true);
        }

        /// <param name="keepCurrentOnFailure">
        /// true なら SDK フォントを用意できないときに今のフォントを残す。
        /// フォント更新の直後は TTF のインポートが終わっておらず、ここで置き換えると
        /// 開いているウィンドウがフォールバックの無いフォントや既定フォントに落ちる。
        /// </param>
        void ApplyTextFont(VisualElement root, bool keepCurrentOnFailure = false)
        {
            if (TextFontAsset != null)
                root.style.unityFontDefinition = new StyleFontDefinition(TextFontAsset);
            else if (TextFont != null)
                root.style.unityFontDefinition = FontDefinition.FromFont(TextFont);
            else if (HasAncestorScope(root))
                // 入れ子のスコープはフォントを指定せず、外側のスコープのフォントを継承する
                root.style.unityFontDefinition = StyleKeyword.Null;
            else
            {
                var fontAsset = LoadFontAsset();
                if (fontAsset != null)
                    root.style.unityFontDefinition = new StyleFontDefinition(fontAsset);
                else if (!keepCurrentOnFailure)
                {
                    var font = LoadFont();
                    if (font != null)
                        root.style.unityFontDefinition = FontDefinition.FromFont(font);
                }
            }
        }

        static bool HasAncestorScope(VisualElement el)
        {
            for (var current = el.parent; current != null; current = current.parent)
            {
                if (s_appliedThemes.TryGetValue(current, out _) || s_linkedThemes.TryGetValue(current, out _))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// ポップアップなど、スコープの外に置く要素に起点のテーマを引き継がせる。
        /// インラインの色は設定しない（要素側が自分で塗る）。
        /// </summary>
        internal static void LinkScope(VisualElement el, MD3Theme theme)
        {
            s_linkedThemes.Remove(el);
            s_linkedThemes.Add(el, theme);
        }

        internal static void RefreshFonts(VisualElement root)
        {
            if (s_appliedThemes.TryGetValue(root, out var theme))
                theme.ApplyTextFont(root, keepCurrentOnFailure: true);
            else if ((root.ClassListContains("md3-dark") || root.ClassListContains("md3-light")) &&
                     !HasAncestorScope(root))
                // ApplyTo を使わずクラスだけ付けたウィンドウのルート
                Resolve(root).ApplyTextFont(root, keepCurrentOnFailure: true);
            RefreshChildFonts(root);
        }

        // 外側のスコープが増減すると、内側のスコープが継承するか自分で指定するかが変わる。
        static void RefreshChildFonts(VisualElement root)
        {
            var children = root.hierarchy;
            for (int i = 0; i < children.childCount; i++)
                RefreshFonts(children[i]);
        }

        static void RefreshDescendants(VisualElement el)
        {
            if (el is IMD3Themeable t)
                t.RefreshTheme();
            var children = el.hierarchy;
            for (int i = 0; i < children.childCount; i++)
                RefreshDescendants(children[i]);
        }

        public Color HoverOverlay(Color bg, Color fg) => Color.Lerp(bg, fg, 0.08f);
        public Color PressOverlay(Color bg, Color fg) => Color.Lerp(bg, fg, 0.12f);
        public Color Disabled(Color c) => new Color(c.r, c.g, c.b, c.a * 0.38f);

        static StyleSheet _cachedThemeSheet;
        static StyleSheet _cachedComponentsSheet;

        internal static void AddStyleSheetsTo(VisualElement root)
        {
            AddStyleSheet(root, LoadThemeStyleSheet());
            AddStyleSheet(root, LoadComponentsStyleSheet());
        }

        public static StyleSheet LoadThemeStyleSheet()
            => LoadStyleSheet("MD3Theme", ref _cachedThemeSheet);

        public static StyleSheet LoadComponentsStyleSheet()
            => LoadStyleSheet("MD3Components", ref _cachedComponentsSheet);

        static StyleSheet LoadStyleSheet(string assetName, ref StyleSheet cached)
        {
            if (cached != null) return cached;
            var guids = AssetDatabase.FindAssets(assetName + " t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if ((path.Contains("MD3SDK") || path.Contains("net.ajisaiflow.md3sdk")) && path.EndsWith(assetName + ".uss"))
                    return cached = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
            return null;
        }

        /// <summary>
        /// static キャッシュをクリアし、永続化済みの生成 FontAsset アセットも削除する。
        /// アトラスが壊れた場合の手動復旧用。
        /// </summary>
        public static void ClearFontCache()
        {
            ResetFontCache();
            MD3FontAssetStore.InvalidateAll();
        }

        /// <summary>
        /// static キャッシュだけを捨てる。生成済みアセットは消さない。
        /// 元フォントが変わった場合は MD3FontAssetStore 側の入力ハッシュ判定で
        /// 自動的に焼き直される。
        /// </summary>
        internal static void ResetFontCache()
        {
            s_fontAsset = null;
            s_font = null;
        }

        // 旧実装は delayCall の中で s_refreshRetryScheduled を「処理の前に」false へ戻して
        // いたため、RefreshAllWindows -> LoadFontAsset が「未スケジュール」と誤認して
        // 毎 tick 際限なく再武装していた。フラグは必ず処理後に戻し、試行回数で頭を打つ。
        const int MaxRefreshRetries = 3;
        static bool s_refreshRetryScheduled;
        static int s_refreshRetryCount;

        /// <summary>
        /// FontAsset の生成に失敗した場合 (AssetDatabase 準備中・atlas 半壊など) に
        /// 遅延リトライを登録する。次の editor tick で RefreshAllWindows を実行し、
        /// 全ウィンドウに fresh な FontAsset を再割り当てする。
        /// 成功するまで最大 <see cref="MaxRefreshRetries"/> 回。
        /// </summary>
        static void ScheduleRefreshRetry()
        {
            if (s_refreshRetryScheduled) return;
            if (s_refreshRetryCount >= MaxRefreshRetries) return;

            s_refreshRetryScheduled = true;
            s_refreshRetryCount++;
            EditorApplication.delayCall += () =>
            {
                // フラグは処理の「前」に戻す。
                // finally で戻すと、RefreshAllWindows の中から呼ばれる
                // ScheduleRefreshRetry が「予約済み」と誤認して連鎖がそこで切れ、
                // 上限 3 回が実質 1 回になってしまう。
                // 旧実装の無限再武装は s_refreshRetryCount の上限が止めるので、
                // ここで先に戻しても安全。
                s_refreshRetryScheduled = false;
                MD3FontManager.RefreshAllWindows();
            };
        }

        /// <summary>
        /// FontAsset をロードして返す (RefreshAllWindows から使用)。
        /// キャッシュがクリアされていれば新規生成される。
        /// </summary>
        public static FontAsset LoadFontAssetPublic() => LoadFontAsset();

        static FontAsset LoadFontAsset()
        {
            // ドメインリロードや再インポートで static 参照が破棄されていたらクリア
            if (s_fontAsset != null && !s_fontAsset) { s_fontAsset = null; s_font = null; }
            if (s_font != null && !s_font) { s_font = null; s_fontAsset = null; }
            if (s_fontAsset != null) return s_fontAsset;

            var baseFont = LoadFont();
            if (baseFont == null)
            {
                // AssetDatabase 準備中の可能性 — 次 tick で再試行
                ScheduleRefreshRetry();
                return null;
            }

            // フォールバックチェーン (多言語 + Emoji)
            var fallbacks = MD3FontManager.LoadAllFallbackFonts(MD3FontManager.ActiveFontPrefix);
            var emojiFont = MD3FontManager.LoadEmojiFont();
            if (emojiFont != null) fallbacks.Add(emojiFont);

            // Static main FontAsset (ディスク永続化) + memory-only Dynamic fallback を取得。
            // 重要: 返ってきた main は Static で atlas 変更を起こさない。
            // fa の fallbackFontAssetTable はランタイム代入 (シリアライズなし) のため、
            // main に対して EditorUtility.SetDirty / AssetDatabase.SaveAssetIfDirty を
            // 呼んではならない (artifactId 分裂 → UUM-69151 を踏む)。
            var fa = MD3FontAssetStore.GetOrCreate("theme", baseFont, fallbacks);
            if (fa == null)
            {
                ScheduleRefreshRetry();
                return null;
            }

            s_fontAsset = fa;
            s_refreshRetryCount = 0;
            return s_fontAsset;
        }

        static Font LoadFont()
        {
            if (s_font != null) return s_font;
            s_font = MD3FontManager.LoadActiveFont();
            if (s_font != null) return s_font;

            // ActiveFont 未設定 → インストール済みフォントから自動選択
            var installed = MD3FontManager.GetInstalledFonts();
            if (installed.Count > 0)
            {
                var recommended = MD3FontManager.DetectRecommendedFont();
                var pick = recommended.HasValue && installed.Exists(f => f.FontPrefix == recommended.Value.FontPrefix)
                    ? recommended.Value
                    : installed[0];
                MD3FontManager.ActiveFontPrefix = pick.FontPrefix;
                s_font = MD3FontManager.LoadActiveFont();
            }
            return s_font;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
