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

        // ── 生成アセットの入力ハッシュ ──
        // アトラスに焼き込んだ「元フォント + 文字セット」を記録し、次回の要求と
        // 一致しない場合だけ再生成する。記録先に AssetImporter.userData を使うと
        // SaveAndReimport が走り UUM-69151 の ImportAsset 経路に触れてしまうため、
        // AssetDatabase の外側 (EditorPrefs) に置く。
        const string InputHashKeyPrefix = "MD3SDK.FontStore.InputHash:";

        /// <summary>
        /// main Static atlas に焼き込む文字セット (ASCII printable)。
        /// 動的文字は全部 memory-only Dynamic fallback で受けるため、main はこれだけでよい。
        /// </summary>
        const string MainStaticCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~";

        static uint[] s_mainStaticCodepoints;

        /// <summary><see cref="MainStaticCharacters"/> の codepoint 版 (遅延生成)。</summary>
        static uint[] MainStaticCodepoints =>
            s_mainStaticCodepoints ?? (s_mainStaticCodepoints = ToCodepoints(new[] { MainStaticCharacters }));

        static string InputHashKey(string assetPath) =>
            InputHashKeyPrefix + Hash128.Compute(Application.dataPath) + ":" + assetPath;

        /// <summary>
        /// 文字列群を重複なしの codepoint 配列に変換する (昇順)。
        ///
        /// 重要: FontAsset.TryAddCharacters(string) はサロゲートペアを 1 つの codepoint
        /// として扱わず、UTF-16 単位 2 つを個別に探しに行って両方失敗する。
        /// Material Symbols は U+FFF7E 以降 (Plane 15 の私用領域) にもアイコンを持つため、
        /// string 版で焼くとそれらが 1 つも atlas に入らず、定数は存在するのに □ になる。
        /// codepoint に直して uint[] 版を使えば正しく焼ける (実測で確認済み)。
        ///
        /// 昇順に並べるのは、リフレクションのフィールド順に依存せず入力ハッシュを
        /// 安定させるため。
        /// </summary>
        static uint[] ToCodepoints(IEnumerable<string> strings)
        {
            var seen = new HashSet<uint>();
            var list = new List<uint>(8192);
            if (strings == null) return list.ToArray();

            foreach (var s in strings)
            {
                if (string.IsNullOrEmpty(s)) continue;
                for (int i = 0; i < s.Length; i++)
                {
                    uint cp;
                    if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                    {
                        cp = (uint)char.ConvertToUtf32(s[i], s[i + 1]);
                        i++;
                    }
                    else if (char.IsSurrogate(s[i]))
                    {
                        continue; // 片割れだけの壊れた文字は捨てる
                    }
                    else
                    {
                        cp = s[i];
                    }
                    if (seen.Add(cp)) list.Add(cp);
                }
            }
            list.Sort();
            return list.ToArray();
        }

        /// <summary>元フォントの GUID と焼き込む codepoint 集合から入力ハッシュを作る。</summary>
        static string ComputeInputHash(Font baseFont, uint[] codepoints)
        {
            string fontId = baseFont == null ? "<null>" : baseFont.name;
            if (baseFont != null &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(baseFont, out string guid, out long _))
                fontId = guid;

            // codepoint をそのまま連結すると数万文字の文字列になるので FNV-1a で畳む。
            ulong h = 14695981039346656037UL;
            if (codepoints != null)
            {
                for (int i = 0; i < codepoints.Length; i++)
                {
                    ulong v = codepoints[i];
                    for (int b = 0; b < 4; b++)
                    {
                        h ^= (v >> (b * 8)) & 0xFF;
                        h *= 1099511628211UL;
                    }
                }
            }
            int count = codepoints == null ? 0 : codepoints.Length;
            return Hash128.Compute(fontId + "|" + count + "|" + h.ToString("x16")).ToString();
        }

        /// <summary>警告用に codepoint を U+XXXX 形式で並べる (先頭 max 件まで)。</summary>
        static string FormatCodepoints(uint[] codepoints, int max)
        {
            if (codepoints == null || codepoints.Length == 0) return "(なし)";
            var parts = new List<string>(Mathf.Min(max, codepoints.Length));
            for (int i = 0; i < codepoints.Length && i < max; i++)
                parts.Add("U+" + codepoints[i].ToString("X"));
            var text = string.Join(", ", parts);
            if (codepoints.Length > max) text += " ほか " + (codepoints.Length - max) + " 件";
            return text;
        }

        /// <summary>
        /// 既存アセットが現在の入力で焼かれたものかを判定する。
        /// 記録が無い場合 (= このバージョンより前に焼かれたアセット) は現在の入力で
        /// 焼かれたものとみなして記録だけ引き継ぐ。ここで一律に焼き直すと、
        /// 全ユーザーがアップグレード直後に 4211 glyph の再生成を 1 度踏むことになる。
        /// 古い世代を強制的に捨てたいときは MD3FontAssetStoreMigration.CurrentVersion を上げる。
        /// </summary>
        static bool InputHashMatches(string assetPath, string wantHash)
        {
            var prefsKey = InputHashKey(assetPath);
            var stored = EditorPrefs.GetString(prefsKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
            {
                EditorPrefs.SetString(prefsKey, wantHash);
                return true;
            }
            return stored == wantHash;
        }

        /// <summary>永続化に成功したときだけ入力ハッシュを確定させる。</summary>
        static void StoreInputHash(string assetPath, string wantHash)
        {
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
                EditorPrefs.SetString(InputHashKey(assetPath), wantHash);
            else
                EditorPrefs.DeleteKey(InputHashKey(assetPath));
        }

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
                // ただし repaint 中に TextCore が古い fallback の native ポインタを
                // 参照している可能性があるため、まずテーブルを差し替えて参照を切り、
                // 古いインスタンスの destroy は 1 tick 遅延させる。
                var stale = main.fallbackFontAssetTable;

                var table = new List<FontAsset>(fallbackFonts.Count);
                foreach (var fb in fallbackFonts)
                {
                    if (fb == null) continue;
                    var dyn = CreateMemoryOnlyDynamicFallback(fb);
                    if (dyn != null) table.Add(dyn);
                }
                main.fallbackFontAssetTable = table;

                if (stale != null)
                    EditorApplication.delayCall += () => DestroyMemoryOnlyFallbacks(stale);
            }
            return main;
        }

        /// <summary>
        /// アイコン用 Static FontAsset を返す。指定した codepoint 文字列群を事前焼きしてから
        /// Static 固定する。同じ key・同じ元フォント・同じ codepoint セットなら
        /// 2 回目以降はディスクから返す。いずれかが変わっていれば作り直す。
        /// </summary>
        public static FontAsset GetOrCreateIconFont(string key, Font iconFont, IEnumerable<string> codepointStrings)
        {
            if (iconFont == null) return null;
            var path = $"{GeneratedDir}/MD3_FA_{Sanitize(key)}.asset";

            // 焼き込む codepoint を先に確定させ、入力ハッシュを取る。
            var codepoints = ToCodepoints(codepointStrings);
            string inputHash = ComputeInputHash(iconFont, codepoints);

            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !IsBroken(existing)
                && existing.atlasPopulationMode == AtlasPopulationMode.Static
                && InputHashMatches(path, inputHash))
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            FontAsset fa;
            try
            {
                // Material Symbols は 4000+ PUA codepoint を持つため default の
                // 1024x1024 atlas / samplingPointSize=90 では収まらない (overflow → □)。
                // 2048x2048 + samplingPointSize=50 + multi-atlas で全 codepoint を焼く。
                // SDF レンダリングなので samplingPointSize を下げても表示時の品質は
                // 影響を受けにくい (UI の icon は 16-24px 程度で描画される)。
                fa = FontAsset.CreateFontAsset(
                    iconFont,
                    samplingPointSize: 50,
                    atlasPadding: 4,
                    renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    atlasWidth: 2048,
                    atlasHeight: 2048,
                    atlasPopulationMode: AtlasPopulationMode.Dynamic,
                    enableMultiAtlasSupport: true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MD3FontAssetStore] CreateFontAsset(icon) failed for '{key}': {ex.Message}");
                return null;
            }
            if (fa == null || IsBroken(fa)) return null;

            // 全 codepoint を事前焼き
            // 注意: default の atlas は 1024x1024 で、Material Symbols 4000+ codepoint は
            //       1 ページに収まらず silently drop される (= 一部アイコンが □ 表示)。
            //       isMultiAtlasTexturesEnabled = true で overflow を別 atlas に逃がす。
            if (codepoints.Length > 0)
            {
                // string 版ではなく uint[] 版を使う (サロゲートペア対応。ToCodepoints 参照)。
                if (!fa.TryAddCharacters(codepoints, out uint[] missing, false) &&
                    missing != null && missing.Length > 0)
                {
                    Debug.LogWarning(
                        $"[MD3FontAssetStore] '{key}' のアイコン atlas に " +
                        $"{codepoints.Length} 個中 {missing.Length} 個の codepoint を収録できませんでした: " +
                        $"{FormatCodepoints(missing, 8)}。" +
                        $"samplingPointSize を下げるか、アイコンフォントを分割してください。");
                }
            }
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            var persisted = PersistAsSubassetBundle(fa, path, $"MD3_FA_{Sanitize(key)}");
            StoreInputHash(path, inputHash);
            return persisted;
        }

        /// <summary>生成済みアセットを全削除する。フォント設定変更時に呼ぶ。</summary>
        public static void InvalidateAll()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedDir)) return;
            var guids = AssetDatabase.FindAssets("t:FontAsset", new[] { GeneratedDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EditorPrefs.DeleteKey(InputHashKey(path));
                AssetDatabase.DeleteAsset(path);
            }
        }

        // ── 内部 ──

        static FontAsset GetOrCreateMainStatic(string key, Font baseFont)
        {
            var path = $"{GeneratedDir}/MD3_FA_{Sanitize(key)}.asset";

            // key は "theme" 固定なので、テーマフォントを差し替えても path は変わらない。
            // 元フォントを含む入力ハッシュで判定しないと、古いフォントで焼いた atlas を
            // 使い続けてしまう。
            string inputHash = ComputeInputHash(baseFont, MainStaticCodepoints);

            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !IsBroken(existing)
                && existing.atlasPopulationMode == AtlasPopulationMode.Static
                && InputHashMatches(path, inputHash))
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
            fa.TryAddCharacters(MainStaticCodepoints, out uint[] _, false);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            var persisted = PersistAsSubassetBundle(fa, path, $"MD3_FA_{Sanitize(key)}");
            StoreInputHash(path, inputHash);
            return persisted;
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
                catch (System.Exception delEx)
                {
                    Debug.LogWarning(
                        $"[MD3FontAssetStore] Failed to delete partial asset at '{path}' " +
                        $"after persist failure: {delEx.Message}");
                }

                Debug.LogWarning($"[MD3FontAssetStore] Persist failed for '{baseName}' ({ex.Message}); " +
                                 "returning runtime instance for this session.");
                return fa;
            }
        }

        static FontAsset CreateMemoryOnlyDynamicFallback(Font baseFont)
        {
            FontAsset fb;
            try
            {
                // single-atlas (2048x2048) + multi-atlas disable: ランタイムで atlas が
                // 拡張されると新しい Texture2D が default HideFlags で作られ、
                // UnloadUnusedAssets で回収されて MissingReferenceException を引き起こす。
                // single atlas に集約して新 Texture が作られないようにする。
                // 2048x2048 + samplingPointSize=50 で約 1200 glyph 収容可能。
                // overflow した場合は missing characters として □ になるが、
                // クラッシュ/MissingReferenceException よりは妥当な劣化。
                fb = FontAsset.CreateFontAsset(
                    baseFont,
                    samplingPointSize: 50,
                    atlasPadding: 4,
                    renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    atlasWidth: 2048,
                    atlasHeight: 2048,
                    atlasPopulationMode: AtlasPopulationMode.Dynamic,
                    enableMultiAtlasSupport: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MD3FontAssetStore] memory-only fallback failed for '{baseFont.name}': {ex.Message}");
                return null;
            }
            if (fb == null) return null;
            fb.name = "MD3_FA_dyn_" + Sanitize(baseFont.name);

            // HideFlags.HideAndDontSave 必須。DontSave だけでは UnloadUnusedAssets で
            // 内部の atlas Texture2D が回収され、TryAddCharacterInternal で
            // MissingReferenceException が発生する。FontAsset 本体だけでなく
            // material と atlasTextures にも明示的に同じ HideFlags を設定する
            // (Unity は親 Object の HideFlags を子に伝播しない)。
            fb.hideFlags = HideFlags.HideAndDontSave;
            if (fb.material != null)
                fb.material.hideFlags = HideFlags.HideAndDontSave;
            if (fb.atlasTextures != null)
            {
                for (int i = 0; i < fb.atlasTextures.Length; i++)
                {
                    var tex = fb.atlasTextures[i];
                    if (tex != null) tex.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return fb;
        }

        static void DestroyMemoryOnlyFallbacks(IList<FontAsset> table)
        {
            if (table == null) return;
            for (int i = 0; i < table.Count; i++)
            {
                var old = table[i];
                // HideAndDontSave は DontSave bit を含むので両方マッチする
                if (old != null && (old.hideFlags & HideFlags.DontSave) != 0)
                    Object.DestroyImmediate(old);
            }
        }

        static bool IsBroken(FontAsset fa)
        {
            try
            {
                if (fa == null || !fa) return true;
                var textures = fa.atlasTextures;
                if (textures == null || textures.Length == 0) return true;

                // TextCore は multi-atlas を拡張するとき m_AtlasTextures を実使用枚数より
                // 大きく確保するため、末尾のスロットは正常な状態でも null のまま残る
                // (例: 3 枚使用 → 配列長 4)。これを「壊れている」と判定すると multi-atlas な
                // アイコンフォントは毎回キャッシュミスして削除・再生成され、4000+ codepoint の
                // SDF 焼き直し (数分・メインスレッド同期) がウィンドウを開くたびに走る。
                //
                // 実使用枚数は atlasTextureCount (= m_AtlasTextureIndex + 1) が返す。
                // m_AtlasTextureIndex は [SerializeField] なのでロード後も有効。
                // 「最後の非 null スロットまでを検査する」ヒューリスティックでは、
                // 最終ページの sub-asset が失われたケース ([t0, t1, null, null] で
                // atlasTextureCount == 3) を健全と誤判定してしまう。
                int used = Mathf.Clamp(fa.atlasTextureCount, 1, textures.Length);
                for (int i = 0; i < used; i++)
                {
                    var tex = textures[i];
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
        // v3: BMP 外 (サロゲートペア) の codepoint が 1 つも焼かれていなかった不具合の修正。
        //     入力ハッシュの記録が無い既存アセットは InputHashMatches が「現在の入力で
        //     焼かれたもの」として採用するため、放置すると v0.8.5 以前で焼かれた
        //     51 個欠けたままの atlas が使われ続ける。ここで 1 度だけ確実に捨てる。
        const int CurrentVersion = 3;
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
