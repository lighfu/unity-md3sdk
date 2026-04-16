using System.Collections.Generic;
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

        static MD3Theme s_dark;
        static MD3Theme s_light;
        static MD3Theme s_default;
        static readonly Dictionary<VisualElement, MD3Theme> s_customThemes = new();

        // カスタムテーマの re-attach 復元用。ConditionalWeakTable はキーが GC されるとエントリも自動削除される。
        static readonly ConditionalWeakTable<VisualElement, MD3Theme> s_appliedThemes = new();
        static readonly ConditionalWeakTable<VisualElement, object> s_callbacksRegistered = new();
        static readonly object s_sentinel = new();

        static Font s_font;
        static FontAsset s_fontAsset;

        /// <summary>
        /// VisualElement ツリーを遡ってテーマを解決する。
        /// カスタムテーマが登録されていればそれを優先、なければ CSS クラスからデフォルト。
        /// </summary>
        public static MD3Theme Resolve(VisualElement el)
        {
            var current = el?.parent;
            while (current != null)
            {
                if (s_customThemes.TryGetValue(current, out var custom))
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
        /// Applies the theme to a root VisualElement by adding md3-dark/md3-light class
        /// and setting inline Surface background + OnSurface text color.
        /// Components resolve their colors by walking up the tree to find the theme class.
        /// </summary>
        public void ApplyTo(VisualElement root)
        {
            root.RemoveFromClassList("md3-dark");
            root.RemoveFromClassList("md3-light");
            root.AddToClassList(IsDark ? "md3-dark" : "md3-light");

            // カスタムテーマを登録（Dark()/Light() シングルトンでなければ）
            if (this != s_dark && this != s_light)
            {
                s_customThemes[root] = this;

                // re-attach 時に復元するためのバックアップ (WeakRef でリーク防止)
                s_appliedThemes.Remove(root);
                s_appliedThemes.Add(root, this);

                // コールバックは root 1 つにつき 1 回だけ登録
                if (!s_callbacksRegistered.TryGetValue(root, out _))
                {
                    s_callbacksRegistered.Add(root, s_sentinel);
                    root.RegisterCallback<DetachFromPanelEvent>(_ => s_customThemes.Remove(root));
                    root.RegisterCallback<AttachToPanelEvent>(_ =>
                    {
                        if (s_appliedThemes.TryGetValue(root, out var theme))
                            s_customThemes[root] = theme;
                    });
                }
            }
            else
            {
                s_customThemes.Remove(root);
                s_appliedThemes.Remove(root);
            }

            // Set root surface colors inline (USS custom properties can't be set from C#)
            root.style.backgroundColor = Surface;
            root.style.color = OnSurface;

            // CJK + Emoji フォント適用（フォールバック不整合防止）
            var fontAsset = LoadFontAsset();
            if (fontAsset != null)
                root.style.unityFontDefinition = new StyleFontDefinition(fontAsset);
            else
            {
                var font = LoadFont();
                if (font != null)
                    root.style.unityFontDefinition = FontDefinition.FromFont(font);
            }

            // Refresh all MD3 components in the tree
            RefreshDescendants(root);
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

        public static StyleSheet LoadThemeStyleSheet()
        {
            if (_cachedThemeSheet != null) return _cachedThemeSheet;
            var guids = AssetDatabase.FindAssets("MD3Theme t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if ((path.Contains("MD3SDK") || path.Contains("net.ajisaiflow.md3sdk")) && path.EndsWith("MD3Theme.uss"))
                    return _cachedThemeSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
            return null;
        }

        public static StyleSheet LoadComponentsStyleSheet()
        {
            if (_cachedComponentsSheet != null) return _cachedComponentsSheet;
            var guids = AssetDatabase.FindAssets("MD3Components t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if ((path.Contains("MD3SDK") || path.Contains("net.ajisaiflow.md3sdk")) && path.EndsWith("MD3Components.uss"))
                    return _cachedComponentsSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
            return null;
        }

        public static void ClearFontCache()
        {
            // DestroyImmediate しない — UI が参照中の FontAsset を破棄するとテキストが消える
            // 旧インスタンスは GC に任せ、次回 ApplyTo で新しい FontAsset を生成する
            s_fontAsset = null;
            s_font = null;
        }

        static bool s_refreshRetryScheduled;

        /// <summary>
        /// FontAsset の生成に失敗した場合 (AssetDatabase 準備中・atlas 半壊など) に
        /// 1 回だけ遅延リトライを登録する。次の editor tick で RefreshAllWindows を実行し、
        /// 全ウィンドウに fresh な FontAsset を再割り当てする。
        /// </summary>
        static void ScheduleRefreshRetry()
        {
            if (s_refreshRetryScheduled) return;
            s_refreshRetryScheduled = true;
            EditorApplication.delayCall += () =>
            {
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
            // ドメインリロードや AssetDatabase.Refresh で破棄されていたらクリア
            // FontAsset 自体が生きていても内部の atlasTexture が破棄されることがある
            if (s_fontAsset != null && !s_fontAsset) { s_fontAsset = null; s_font = null; }
            if (s_font != null && !s_font) { s_font = null; s_fontAsset = null; }
            if (s_fontAsset != null && IsFontAssetBroken(s_fontAsset)) { s_fontAsset = null; s_font = null; }
            if (s_fontAsset != null) return s_fontAsset;

            var baseFont = LoadFont();
            if (baseFont == null)
            {
                // AssetDatabase 準備中の可能性 — 遅延リトライで自動回復
                ScheduleRefreshRetry();
                return null;
            }

            var fa = FontAsset.CreateFontAsset(baseFont);
            if (fa == null)
            {
                ScheduleRefreshRetry();
                return null;
            }

            // 生成直後でも atlasTexture が null/破棄済みの場合がある
            // (ドメインリロード直後の AssetDatabase 半準備状態など)。
            // そのまま UI に渡すと UIRStylePainter.DrawTextInfo で NRE になるので
            // cache せず遅延リトライに委ねる。呼び出し側 (ApplyTo) は
            // FontDefinition.FromFont フォールバックに落ちる。
            if (IsFontAssetBroken(fa))
            {
                ScheduleRefreshRetry();
                return null;
            }

            s_fontAsset = fa;
            MD3Icon.ProtectFontAsset(s_fontAsset);
            s_fontAsset.fallbackFontAssetTable = new List<FontAsset>();

            // インストール済みの全フォントをフォールバックに追加
            // (多言語混在テキスト対応)
            var activePrefix = MD3FontManager.ActiveFontPrefix;
            foreach (var fallbackFont in MD3FontManager.LoadAllFallbackFonts(activePrefix))
            {
                var fallbackFa = FontAsset.CreateFontAsset(fallbackFont);
                if (fallbackFa != null)
                {
                    MD3Icon.ProtectFontAsset(fallbackFa);
                    s_fontAsset.fallbackFontAssetTable.Add(fallbackFa);
                }
            }

            // Emoji フォールバック
            var emojiFont = MD3FontManager.LoadEmojiFont();
            if (emojiFont != null)
            {
                var emojiFontAsset = FontAsset.CreateFontAsset(emojiFont);
                if (emojiFontAsset != null)
                {
                    MD3Icon.ProtectFontAsset(emojiFontAsset);
                    s_fontAsset.fallbackFontAssetTable.Add(emojiFontAsset);
                }
            }

            return s_fontAsset;
        }

        /// <summary>
        /// FontAsset の内部テクスチャが破棄されているか判定。
        /// ドメインリロードやプレイモード遷移で FontAsset は生存するが
        /// 内部の atlasTexture (Texture2D) が破棄されることがある。
        /// </summary>
        static bool IsFontAssetBroken(FontAsset fa)
        {
            try
            {
                // atlasTexture が null または破棄済みならキャッシュを再生成する必要がある
                if (fa.atlasTextures == null || fa.atlasTextures.Length == 0) return true;
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    var tex = fa.atlasTextures[i];
                    if (tex == null || !tex) return true;
                }
                // フォールバックチェーンも確認
                if (fa.fallbackFontAssetTable != null)
                {
                    foreach (var fallback in fa.fallbackFontAssetTable)
                    {
                        if (fallback == null || !fallback) return true;
                        if (fallback.atlasTextures != null)
                        {
                            for (int i = 0; i < fallback.atlasTextures.Length; i++)
                            {
                                var tex = fallback.atlasTextures[i];
                                if (tex == null || !tex) return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return true;
            }
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
