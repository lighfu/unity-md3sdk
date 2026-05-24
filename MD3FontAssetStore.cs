using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// FontAsset をディスクアセットとして永続化するストア。
    ///
    /// FontAsset.CreateFontAsset() が返す実行時インスタンスは、内部 atlasTexture が
    /// ドメインリロード / プレイモード遷移で破棄され、文字が「歯抜け」になる。
    /// 本ストアは FontAsset・atlasTexture・material を .asset サブアセットとして保存し、
    /// リロード後はディスクからロードし直すことでアトラスを無傷のまま復帰させる。
    /// </summary>
    public static class MD3FontAssetStore
    {
        const string ParentDir = "Assets/MD3SDKFonts";
        const string GeneratedDir = ParentDir + "/Generated";

        /// <summary>
        /// 永続化された FontAsset を返す。初回はビルドして保存、以降はディスクからロードする。
        /// <paramref name="fallbackFonts"/> はフォールバックチェーンに使う Font 群 (null 可)。
        /// 生成に失敗した場合は非永続の実行時 FontAsset を返す（当該セッションのみ有効）。
        /// </summary>
        public static FontAsset GetOrCreate(string key, Font baseFont, IList<Font> fallbackFonts)
        {
            if (baseFont == null) return null;

            var main = GetOrCreateSingle(key, baseFont, out bool created);
            if (main == null) return null;

            // フォールバックチェーンは新規ビルド時のみ構築する。
            // 既存アセットをロードした場合は fallbackFontAssetTable がシリアライズ済み。
            if (created && fallbackFonts != null && fallbackFonts.Count > 0)
            {
                var table = new List<FontAsset>();
                foreach (var fb in fallbackFonts)
                {
                    if (fb == null) continue;
                    var fbFa = GetOrCreateSingle("fb_" + Sanitize(fb.name), fb, out _);
                    if (fbFa != null && fbFa != main && !table.Contains(fbFa))
                        table.Add(fbFa);
                }
                main.fallbackFontAssetTable = table;
                EditorUtility.SetDirty(main);
                AssetDatabase.SaveAssetIfDirty(main);
            }
            return main;
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

        static FontAsset GetOrCreateSingle(string key, Font baseFont, out bool created)
        {
            created = false;
            if (baseFont == null) return null;

            var path = $"{GeneratedDir}/MD3_FA_{Sanitize(key)}.asset";

            // 既存アセットが健全ならそれを返す（リロード後はこの経路）
            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !IsBroken(existing))
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path); // 破損 — 作り直す

            FontAsset fa;
            try
            {
                fa = FontAsset.CreateFontAsset(baseFont);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MD3FontAssetStore] CreateFontAsset failed for '{key}': {ex.Message}");
                return null;
            }
            if (fa == null || IsBroken(fa)) return null;

            try
            {
                EnsureGeneratedDir();
                fa.name = $"MD3_FA_{Sanitize(key)}";
                AssetDatabase.CreateAsset(fa, path);

                if (fa.material != null)
                {
                    fa.material.name = fa.name + " Material";
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
                // 注意: 過去には SaveAssetIfDirty の直後に AssetDatabase.ImportAsset(path) を呼んでいたが、
                // CreateAsset + AddObjectToAsset + SaveAssetIfDirty で既にインポートは完了している。
                // 明示的な再 ImportAsset は AssetDatabase V2 で同じ guid に対して
                // artifactId が分裂する原因となり (ConsistencyChecker が "inconsistent result" を警告)、
                // 後続の SceneView 描画中に TextEditorResourceManager.DoPostRenderUpdates が
                // 該当アセットを再インポートしようとした際に GPU バッファ破壊 → Unity クラッシュを
                // 引き起こしていたため、この呼び出しは削除する。

                created = true;
                var loaded = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                return loaded != null ? loaded : fa;
            }
            catch (System.Exception ex)
            {
                // AssetDatabase が import 中などで保存に失敗した場合の縮退動作。
                // 非永続だが当該セッションは描画可能。次回呼び出しで永続化を再試行する。
                Debug.LogWarning($"[MD3FontAssetStore] Persist failed for '{key}' ({ex.Message}); " +
                                 "using a runtime FontAsset for this session.");
                return fa;
            }
        }

        /// <summary>FontAsset の atlasTexture が null / 破棄済みかを判定する軽量チェック。</summary>
        static bool IsBroken(FontAsset fa)
        {
            try
            {
                if (fa == null || !fa) return true; // C# null / Unity 破棄済み の両方を弾く
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
                // 物理フォルダは存在するが AssetDatabase 未登録のケースに対応する。
                // 通常 GetOrCreate 到達時点で MD3SDKFonts は登録済みのため、ここはほぼ通らない。
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
    /// 旧版 MD3FontAssetStore は SaveAssetIfDirty の直後に AssetDatabase.ImportAsset を
    /// 呼んでおり、AssetDatabase V2 で同一 guid に対し artifactId 分裂を発生させ、
    /// SceneView 描画中の TextEditorResourceManager.DoPostRenderUpdates による
    /// 再インポートで GPU バッファが破壊され Unity がクラッシュしていた。
    /// 修正版コードでは分裂は発生しないが、既に分裂状態で永続化された .asset は
    /// 残ったままなので、SDK アップグレード時にユーザーが手動で
    /// Assets/MD3SDKFonts/Generated/ を削除する手間を省くために
    /// 1 度だけ <see cref="MD3FontAssetStore.InvalidateAll"/> を実行する。
    /// </summary>
    [InitializeOnLoad]
    static class MD3FontAssetStoreMigration
    {
        const int CurrentVersion = 1;
        const string KeyPrefix = "MD3SDK.FontStore.MigrationVersion:";

        static MD3FontAssetStoreMigration()
        {
            // 静的初期化中 / import 中は AssetDatabase 操作が不安定なので 1 tick 遅延する。
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            // EditorPrefs はマシン共通なので、プロジェクト dataPath で名前空間を切る。
            // string.GetHashCode は AppDomain ごとにランダム化されるため Hash128 を使う。
            var key = KeyPrefix + Hash128.Compute(Application.dataPath);
            if (EditorPrefs.GetInt(key, 0) >= CurrentVersion) return;

            try
            {
                MD3FontAssetStore.InvalidateAll();
            }
            catch (System.Exception ex)
            {
                // フラグは立てず、次回 domain reload で再試行させる。
                Debug.LogWarning($"[MD3FontAssetStore] migration v{CurrentVersion} failed: {ex.Message}");
                return;
            }
            EditorPrefs.SetInt(key, CurrentVersion);
        }
    }
}
