using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// FontAsset を Static atlas (.asset) として永続化し、動的文字は memory-only Dynamic
    /// fallback FontAsset で受けるストア。
    ///
    /// Unity 2022.3 の既知バグ UUM-69151 では、Dynamic FontAsset を AssetDatabase に
    /// 永続化していると TextEditorResourceManager.DoPostRenderUpdates が atlas 変更後に
    /// ImportAsset(path) を呼び、NativeFormatImporter が同 input・同 contentHash に対して
    /// 別 artifactId を生成して ConsistencyChecker が "inconsistent result" を警告、
    /// 累積で D3D11 の GPU バッファ参照不整合 → Unity クラッシュに至る。
    ///
    /// 本ストアは main FontAsset を Static で保存することでランタイムの atlas 変更を
    /// 不可能にし、動的文字描画は HideFlags.DontSave (= AssetDatabase 管理外) の
    /// Dynamic fallback に逃がすことで、ImportAsset の対象から完全に外す。
    /// </summary>
    public static class MD3FontAssetStore
    {
        const string ParentDir = "Assets/MD3SDKFonts";
        const string GeneratedDir = ParentDir + "/Generated";

        /// <summary>
        /// 永続化された Static main FontAsset を返す。動的 fallback は memory-only で
        /// 都度生成し main.fallbackFontAssetTable にランタイム代入する (シリアライズしない)。
        /// </summary>
        public static FontAsset GetOrCreate(string key, Font baseFont, IList<Font> fallbackFonts)
        {
            if (baseFont == null) return null;

            var main = GetOrCreateMainStatic(key, baseFont);
            if (main == null) return null;

            // 動的 fallback は毎回 memory-only で作り直す (ドメインリロードで消える前提)
            // 重要: SetDirty / SaveAssetIfDirty を呼ばない (シリアライズしないため
            //       main の artifactId は変化しない = WARN 発生条件を踏まない)
            if (fallbackFonts != null && fallbackFonts.Count > 0)
            {
                // ScriptableObject 派生の FontAsset は HideFlags.DontSave だと GC で
                // ネイティブ側 (atlas/glyph table) が解放されないので、自前で
                // DestroyImmediate して native leak を防ぐ。
                DestroyMemoryOnlyFallbacks(main.fallbackFontAssetTable);

                var table = new List<FontAsset>(fallbackFonts.Count);
                foreach (var fb in fallbackFonts)
                {
                    if (fb == null) continue;
                    var dyn = CreateMemoryOnlyDynamicFallback(fb);
                    if (dyn != null) table.Add(dyn);
                }
                main.fallbackFontAssetTable = table;
            }
            return main;
        }

        /// <summary>
        /// アイコン用 Static FontAsset を返す。指定した codepoint 文字列群を事前焼きしてから
        /// Static 固定する。同じ key・同じ codepoint セットなら 2 回目以降はディスクから返す。
        /// </summary>
        public static FontAsset GetOrCreateIconFont(string key, Font iconFont, IEnumerable<string> codepointStrings)
        {
            if (iconFont == null) return null;
            var path = $"{GeneratedDir}/MD3_FA_{Sanitize(key)}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !IsBroken(existing) && existing.atlasPopulationMode == AtlasPopulationMode.Static)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            FontAsset fa;
            try { fa = FontAsset.CreateFontAsset(iconFont); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MD3FontAssetStore] CreateFontAsset(icon) failed for '{key}': {ex.Message}");
                return null;
            }
            if (fa == null || IsBroken(fa)) return null;

            // 全 codepoint を事前焼き
            if (codepointStrings != null)
            {
                var sb = new System.Text.StringBuilder(8192);
                foreach (var s in codepointStrings)
                    if (!string.IsNullOrEmpty(s)) sb.Append(s);
                if (sb.Length > 0)
                    fa.TryAddCharacters(sb.ToString(), out _);
            }
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            return PersistAsSubassetBundle(fa, path, $"MD3_FA_{Sanitize(key)}");
        }

        /// <summary>生成済みアセットを全削除する。フォント設定変更時に呼ぶ。</summary>
        public static void InvalidateAll()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedDir)) return;
            var guids = AssetDatabase.FindAssets("t:FontAsset", new[] { GeneratedDir });
            foreach (var guid in guids)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        // ── 内部 ──

        static FontAsset GetOrCreateMainStatic(string key, Font baseFont)
        {
            var path = $"{GeneratedDir}/MD3_FA_{Sanitize(key)}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !IsBroken(existing) && existing.atlasPopulationMode == AtlasPopulationMode.Static)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            FontAsset fa;
            try { fa = FontAsset.CreateFontAsset(baseFont); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MD3FontAssetStore] CreateFontAsset(main) failed for '{key}': {ex.Message}");
                return null;
            }
            if (fa == null || IsBroken(fa)) return null;

            // main は空 Static でよい (動的文字は全部 fallback で受ける)
            // ただし atlas を完全に空にすると一部 Unity 内部処理が走らないので
            // ASCII printable を 1 度だけ焼いておく
            fa.TryAddCharacters(
                " !\"#$%&'()*+,-./0123456789:;<=>?@" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
                "abcdefghijklmnopqrstuvwxyz{|}~",
                out _);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            return PersistAsSubassetBundle(fa, path, $"MD3_FA_{Sanitize(key)}");
        }

        static FontAsset PersistAsSubassetBundle(FontAsset fa, string path, string baseName)
        {
            try
            {
                EnsureGeneratedDir();
                fa.name = baseName;
                AssetDatabase.CreateAsset(fa, path);

                if (fa.material != null)
                {
                    fa.material.name = baseName + " Material";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                }
                if (fa.atlasTextures != null)
                {
                    for (int i = 0; i < fa.atlasTextures.Length; i++)
                    {
                        var tex = fa.atlasTextures[i];
                        if (tex == null) continue;
                        tex.name = $"Atlas {i}";
                        AssetDatabase.AddObjectToAsset(tex, fa);
                    }
                }
                EditorUtility.SetDirty(fa);
                AssetDatabase.SaveAssetIfDirty(fa);

                var loaded = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                return loaded != null ? loaded : fa;
            }
            catch (System.Exception ex)
            {
                // CreateAsset が成功した後で失敗した場合、半端な状態の .asset が
                // ディスクに残ると次回 LoadAssetAtPath が壊れたアセットを返す。
                // 完全に削除して runtime instance のみ返す。
                try
                {
                    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                        AssetDatabase.DeleteAsset(path);
                }
                catch { /* best effort */ }

                Debug.LogWarning($"[MD3FontAssetStore] Persist failed for '{baseName}' ({ex.Message}); " +
                                 "returning runtime instance for this session.");
                return fa;
            }
        }

        static FontAsset CreateMemoryOnlyDynamicFallback(Font baseFont)
        {
            FontAsset fb;
            try { fb = FontAsset.CreateFontAsset(baseFont); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MD3FontAssetStore] memory-only fallback failed for '{baseFont.name}': {ex.Message}");
                return null;
            }
            if (fb == null) return null;
            fb.name = "MD3_FA_dyn_" + Sanitize(baseFont.name);
            fb.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fb.hideFlags = HideFlags.DontSave; // AssetDatabase 管理外 = ImportAsset 対象外
            return fb;
        }

        static void DestroyMemoryOnlyFallbacks(IList<FontAsset> table)
        {
            if (table == null) return;
            for (int i = 0; i < table.Count; i++)
            {
                var old = table[i];
                if (old != null && (old.hideFlags & HideFlags.DontSave) != 0)
                    Object.DestroyImmediate(old);
            }
        }

        static bool IsBroken(FontAsset fa)
        {
            try
            {
                if (fa == null || !fa) return true;
                if (fa.atlasTextures == null || fa.atlasTextures.Length == 0) return true;
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    var tex = fa.atlasTextures[i];
                    if (tex == null || !tex) return true;
                }
                return false;
            }
            catch { return true; }
        }

        static void EnsureGeneratedDir()
        {
            if (AssetDatabase.IsValidFolder(GeneratedDir)) return;
            if (!AssetDatabase.IsValidFolder(ParentDir))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "MD3SDKFonts"));
                AssetDatabase.Refresh();
            }
            if (!AssetDatabase.IsValidFolder(GeneratedDir))
                AssetDatabase.CreateFolder(ParentDir, "Generated");
        }

        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unnamed";
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                    chars[i] = '_';
            return new string(chars);
        }
    }

    /// <summary>
    /// 旧版 MD3FontAssetStore (v0.8.3 以前) は FontAsset を Dynamic で永続化していたため
    /// Unity 2022.3 の UUM-69151 を踏み、WARN/クラッシュを発生させていた。
    /// v0.8.4 で Static + memory-only fallback 構造に変わったので、既存の Dynamic 永続化
    /// アセットは強制削除して作り直す。
    /// </summary>
    [InitializeOnLoad]
    static class MD3FontAssetStoreMigration
    {
        const int CurrentVersion = 2;
        const string KeyPrefix = "MD3SDK.FontStore.MigrationVersion:";

        static MD3FontAssetStoreMigration()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            var key = KeyPrefix + Hash128.Compute(Application.dataPath);
            if (EditorPrefs.GetInt(key, 0) >= CurrentVersion) return;

            try { MD3FontAssetStore.InvalidateAll(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MD3FontAssetStore] migration v{CurrentVersion} failed: {ex.Message}");
                return;
            }
            EditorPrefs.SetInt(key, CurrentVersion);

            // 既存ウィンドウは削除された FontAsset への参照を保持しているため
            // (atlas 消失で描画失敗 → アイコン □ 化) 全ウィンドウに新しい FontAsset を
            // 再注入する。さらに 1 tick 遅延させて AssetDatabase の DeleteAsset が
            // 完全に反映されてから RefreshAllWindows を実行する。
            EditorApplication.delayCall += () =>
            {
                try { MD3FontManager.RefreshAllWindows(); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MD3FontAssetStore] RefreshAllWindows after migration failed: {ex.Message}");
                }
            };
        }
    }
}
