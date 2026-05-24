# MD3SDK FontAsset Static Hybrid Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 2022.3.22f1 で MD3SDK の FontAsset 永続化がトリガしている ConsistencyChecker WARN (UUM-69151) と、それに連なる D3D11 GPU バッファ参照不整合クラッシュを、Unity 側修正 (6000.0.x で fix) を待たずに MD3SDK 側で構造的に解消する。

**Architecture:**
- **main FontAsset (`MD3_FA_theme.asset`)** を **`AtlasPopulationMode = Static`** で永続化する。atlas はビルド時のみ書き込まれ、ランタイム描画では一切 dirty 化しない。
- **icon FontAsset (`MD3_FA_icon.asset`)** も Static で、生成時に `MD3Icon` の全 PUA codepoint (4000+) を事前焼きしてから固定化する。
- 動的な文字 (LLM 応答・新規漢字・絵文字) はすべて **`HideFlags.DontSave` の memory-only Dynamic fallback FontAsset** が受ける。AssetDatabase 管理外なので `TextEditorResourceManager.DoPostRenderUpdates → ImportAsset(path)` の対象から外れる。
- main の `fallbackFontAssetTable` は **シリアライズせずランタイム再構築** とする (SetDirty / SaveAssetIfDirty を呼ばないことで artifactId 分裂を完全に断つ)。

**Tech Stack:** UnityEditor, UnityEngine.TextCore.Text.FontAsset (Unity 2022.3.22f1), AssetDatabase V2, .NET Standard 2.1。

**Critical assumption to verify in Task 1:** Static FontAsset では DoPostRenderUpdates が ImportAsset を呼ばない。NG なら本プランは破棄し案C (memory-only + atlas 単独永続化) に切り替える。

---

## File Map

| Path | Action | Responsibility |
|---|---|---|
| `MD3FontAssetStore.cs` | Rewrite | Static FontAsset 生成 + memory-only fallback ファクトリ + マイグレーション v2 |
| `MD3Theme.cs:303-366` | Modify | `LoadFontAsset` で fallback を動的構築するロジックに変更、main は `GetOrCreateMain` 経由 |
| `MD3Icon.cs` | Modify (small) | Icon FontAsset の生成箇所で全 codepoint 事前焼きヘルパーを呼ぶ |
| `MD3FontManager.cs` (no change to API) | Read-only | 既存の `LoadAllFallbackFonts` / `LoadEmojiFont` の戻り値を流用 |
| `Build~/package.json` | Modify | バージョンを `0.8.4` に bump |
| `package.json` | Modify | 同上 |
| `CHANGELOG.md` | Modify | v0.8.4 エントリ追記 |
| `docs/plans/2026-05-24-fontasset-static-hybrid.md` | Create | このプラン |

---

## Required preconditions

- 作業前に `git status` がクリーンであること (現在 `?? .serena/` のみで他に未コミット変更なし、OK)。
- 作業ブランチ: `fix/fontasset-static-hybrid` (Task 0 で作成)。
- 検証用 Unity プロジェクト: `C:\Users\sakuu\ALCOM\Projects\com.ajisaiflow.vrchat.avater` (junction で `Packages/net.ajisaiflow.md3sdk` が `C:\code\unity\unity-md3sdk` を指す)。

---

### Task 0: ブランチ作成

**Files:**
- ブランチ作成のみ (ファイル変更なし)

- [ ] **Step 1: 現在のブランチと作業ツリーを確認**

Run:
```bash
git -C "C:\code\unity\unity-md3sdk" status --short
git -C "C:\code\unity\unity-md3sdk" branch --show-current
```
Expected: `?? .serena/` のみ表示。current branch は `main`。

- [ ] **Step 2: ブランチ作成**

Run:
```bash
git -C "C:\code\unity\unity-md3sdk" switch -c fix/fontasset-static-hybrid
```
Expected: `Switched to a new branch 'fix/fontasset-static-hybrid'`

---

### Task 1: API/挙動検証スパイク (Unity Editor 上で手動実行)

このタスクは「案F が技術的に成立するか」の Go/No-Go ゲートである。**結果次第で本プランを破棄して案C に乗り換える**ことを許容する。

**Files:**
- Create: `Assets/MD3SDKFonts/Spike/FontAssetStaticSpike.cs` (Unity プロジェクト側、SDK 外。検証後に削除する)

- [ ] **Step 1: 検証スクリプトを書く**

ユーザー側プロジェクトの `Assets/MD3SDKFonts/Spike/FontAssetStaticSpike.cs` を作成:

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public static class FontAssetStaticSpike
{
    const string SpikeDir = "Assets/MD3SDKFonts/Spike";
    const string StaticPath = SpikeDir + "/Spike_Static.asset";

    [MenuItem("Tools/MD3SDK Spike/Create Static + Memory Fallback")]
    public static void Create()
    {
        // 既存削除
        if (AssetDatabase.LoadAssetAtPath<FontAsset>(StaticPath) != null)
            AssetDatabase.DeleteAsset(StaticPath);
        if (!AssetDatabase.IsValidFolder(SpikeDir))
            AssetDatabase.CreateFolder("Assets/MD3SDKFonts", "Spike");

        // 1) ベースフォントを拾う (CJK fallback として MD3SDKFonts/ にある任意の .otf/.ttf)
        var fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/MD3SDKFonts" });
        Font baseFont = null;
        Font fallbackFont = null;
        foreach (var g in fontGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var f = AssetDatabase.LoadAssetAtPath<Font>(p);
            if (f == null) continue;
            if (baseFont == null) baseFont = f;
            else if (fallbackFont == null) { fallbackFont = f; break; }
        }
        if (baseFont == null) { Debug.LogError("No Font under Assets/MD3SDKFonts/"); return; }

        // 2) main FontAsset を Static で作る
        var main = FontAsset.CreateFontAsset(baseFont);
        main.name = "Spike_Static";
        // 事前焼き: ASCII printable のみ
        main.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?");
        main.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(main, StaticPath);
        // material と atlas をサブアセットとして付属 (Task 4 で本実装と一致)
        if (main.material != null)
        {
            main.material.name = main.name + " Material";
            AssetDatabase.AddObjectToAsset(main.material, main);
        }
        if (main.atlasTextures != null)
            for (int i = 0; i < main.atlasTextures.Length; i++)
            {
                var tex = main.atlasTextures[i];
                if (tex == null) continue;
                tex.name = $"Atlas {i}";
                AssetDatabase.AddObjectToAsset(tex, main);
            }
        EditorUtility.SetDirty(main);
        AssetDatabase.SaveAssetIfDirty(main);

        // 3) memory-only Dynamic fallback を作成し main に挿す (シリアライズしない)
        if (fallbackFont != null)
        {
            var fb = FontAsset.CreateFontAsset(fallbackFont);
            fb.name = "Spike_Fallback_Dynamic";
            fb.hideFlags = HideFlags.DontSave;
            fb.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            main.fallbackFontAssetTable = new List<FontAsset> { fb };
            // 注意: ここで SetDirty / SaveAssetIfDirty を呼ばない (= シリアライズしない)
        }

        Debug.Log($"[Spike] Created Static FontAsset at {StaticPath}. Now: " +
                  "(1) recompile any script to force domain reload, " +
                  "(2) open any window that uses this asset, " +
                  "(3) draw Japanese/CJK text to trigger fallback usage, " +
                  "(4) watch Console for ConsistencyChecker warnings.");
    }

    [MenuItem("Tools/MD3SDK Spike/Force Domain Reload")]
    public static void ForceReload() => UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
}
```

- [ ] **Step 2: 検証手順を手動実行 (ユーザー作業)**

```
1. Unity を起動 (ALCOM プロジェクト)
2. メニュー: Tools/MD3SDK Spike/Create Static + Memory Fallback
3. Console: "[Spike] Created Static FontAsset at ..." を確認
4. メニュー: Tools/MD3SDK Spike/Force Domain Reload
5. Domain reload 完了後、UnityAgent ウィンドウを開く
   (どんな日本語/絵文字テキストでも描画される EditorWindow なら何でも可)
6. Tools/MD3SDK Spike/Force Domain Reload を **3 回追加で実行** (累積試験)
7. Console を確認:
   - 期待: "Importer(NativeFormatImporter) generated inconsistent result" が **Spike_Static.asset 由来では 0 件**
   - NG の場合: 案F は不成立 → STOP し案C に転進
```

- [ ] **Step 3: 結果を判定**

GO 条件 (全て満たすこと):
- (a) Spike_Static.asset 由来の inconsistent result 警告が 0 件
- (b) 日本語/絵文字が描画されている (fallback が動作)
- (c) Unity がクラッシュしない

GO ならスパイクスクリプトを削除して Task 2 へ進む。

```bash
# Delete spike artifacts
rm -f "C:\Users\sakuu\ALCOM\Projects\com.ajisaiflow.vrchat.avater\Assets\MD3SDKFonts\Spike\FontAssetStaticSpike.cs"
rm -rf "C:\Users\sakuu\ALCOM\Projects\com.ajisaiflow.vrchat.avater\Assets\MD3SDKFonts\Spike"
```

NG なら **本プランを破棄してユーザーに報告し案C を再計画**する。

---

### Task 2: 設計確定メモを追記

**Files:**
- Modify: `docs/plans/2026-05-24-fontasset-static-hybrid.md` (このファイル末尾)

- [ ] **Step 1: Task 1 の実測結果を本プラン末尾に追記**

このプラン末尾に `## Task 1 Spike Result` セクションを追記し、(a) `inconsistent result` 警告件数、(b) 日本語/絵文字描画の有無、(c) クラッシュ有無、(d) atlas サイズ実測値を記録する。

- [ ] **Step 2: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add docs/plans/2026-05-24-fontasset-static-hybrid.md
git -C "C:\code\unity\unity-md3sdk" commit -m "docs: record FontAsset Static spike results"
```

---

### Task 3: MD3FontAssetStore のリライト

**Files:**
- Modify: `MD3FontAssetStore.cs` (全体書き換え)

新しい API 設計:

| API | 旧 | 新 |
|---|---|---|
| `GetOrCreate(string key, Font baseFont, IList<Font> fallbackFonts)` | あり、戻り FontAsset (永続化済み) | あり (シグネチャ維持)、戻り FontAsset (Static 永続化済み、fallback は memory-only で main に注入済み) |
| `InvalidateAll()` | あり | あり (Generated/ 配下の `t:FontAsset` を全削除) |
| `GetOrCreateIconFont(Font iconFont, IEnumerable<string> codepointStrings)` | なし | **新規追加**。MD3Icon が全 PUA を事前焼きするために呼ぶ |

`MD3FontAssetStoreMigration` の `CurrentVersion` を `1` → `2` に上げ、サブアセット形式 + Dynamic な旧アセットを強制削除する。

- [ ] **Step 1: 旧 `MD3FontAssetStore.cs` を全置換**

`C:\code\unity\unity-md3sdk\MD3FontAssetStore.cs` を以下で完全置換:

```csharp
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
            if (existing != null && !IsBroken(existing))
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
        }
    }
}
```

- [ ] **Step 2: Unity でコンパイルが通ることを確認**

ユーザー手動:
```
1. Unity を起動 (または既に起動中ならスクリプト再コンパイルを待つ)
2. Console に MD3FontAssetStore.cs のコンパイルエラーがないことを確認
```

Expected: コンパイルエラーなし。`MD3Icon.cs` / `MD3Theme.cs` 側で `GetOrCreateIconFont` 未呼び出しでも `GetOrCreate` の API は維持しているので壊れない。

- [ ] **Step 3: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add MD3FontAssetStore.cs
git -C "C:\code\unity\unity-md3sdk" commit -m "feat(font): rewrite MD3FontAssetStore to Static main + memory-only Dynamic fallback"
```

---

### Task 4: MD3Theme.LoadFontAsset の整合性確認

`MD3FontAssetStore.GetOrCreate` のシグネチャは維持しているので **MD3Theme.cs 側の変更は不要**。ただし「fallback が memory-only になったこと」のコメントを `MD3Theme.cs:356` 付近に追記し、将来のメンテナーが SetDirty/Save を main に対して呼んでしまわないように注意喚起する。

**Files:**
- Modify: `MD3Theme.cs` (1 箇所コメント追記のみ)

- [ ] **Step 1: コメント追記**

`C:\code\unity\unity-md3sdk\MD3Theme.cs` の以下を:

```csharp
            // ディスク永続化された FontAsset を取得（ドメインリロード耐性あり）
            var fa = MD3FontAssetStore.GetOrCreate("theme", baseFont, fallbacks);
```

下記に置き換える:

```csharp
            // Static main FontAsset (ディスク永続化) + memory-only Dynamic fallback を取得。
            // 重要: 返ってきた main は Static で atlas 変更を起こさない。
            // fa の fallbackFontAssetTable はランタイム代入 (シリアライズなし) のため、
            // main に対して EditorUtility.SetDirty / AssetDatabase.SaveAssetIfDirty を
            // 呼んではならない (artifactId 分裂 → UUM-69151 を踏む)。
            var fa = MD3FontAssetStore.GetOrCreate("theme", baseFont, fallbacks);
```

- [ ] **Step 2: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add MD3Theme.cs
git -C "C:\code\unity\unity-md3sdk" commit -m "docs(font): note Static main + memory-only fallback invariants"
```

---

### Task 5: MD3Icon の Icon FontAsset を全 codepoint 事前焼き Static に変更

`MD3Icon.cs` で Icon FontAsset を生成している箇所を特定し、`MD3FontAssetStore.GetOrCreateIconFont(...)` を呼ぶように変更する。

**Files:**
- Modify: `MD3Icon.cs` (`EnsureFont` / `EnsureFilledFont` 周辺)

- [ ] **Step 1: 既存実装を読む**

```bash
# MD3Icon.cs 全体は 4408 行で前半 (1-1110) はアイコン const string。
# EnsureFont / EnsureFilledFont は後半。Grep で正確に特定する。
```

```
Grep "EnsureFont|EnsureFilledFont|MD3FontAssetStore.GetOrCreate" C:\code\unity\unity-md3sdk\MD3Icon.cs
```

- [ ] **Step 2: 全 codepoint 列挙ヘルパーを `MD3Icon.cs` に追加**

`MD3Icon` クラス内に以下のメソッドを追加:

```csharp
        /// <summary>このクラスの全 const string (Material Symbols PUA codepoint 群) を列挙する。</summary>
        static IEnumerable<string> EnumerateAllIconCodepoints()
        {
            var t = typeof(MD3Icon);
            var fields = t.GetFields(System.Reflection.BindingFlags.Public |
                                     System.Reflection.BindingFlags.Static);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(string)) continue;
                if (!f.IsLiteral || f.IsInitOnly) continue; // const のみ
                var v = (string)f.GetRawConstantValue();
                if (!string.IsNullOrEmpty(v)) yield return v;
            }
        }
```

- [ ] **Step 3: 既存の `MD3FontAssetStore.GetOrCreate("icon", ...)` 呼び出しを `GetOrCreateIconFont` に切り替え**

`MD3Icon.cs` 内で `MD3FontAssetStore.GetOrCreate("icon", iconFont, ...)` (もしくは `GetOrCreate("icon_filled", ...)`) を呼んでいる箇所を:

```csharp
var fa = MD3FontAssetStore.GetOrCreateIconFont("icon", iconFont, EnumerateAllIconCodepoints());
```

に置き換える (`icon_filled` も同様)。

- [ ] **Step 4: Unity でコンパイル通過 + atlas サイズ実測**

ユーザー手動:
```
1. Unity で再コンパイルを待つ
2. MD3FontAssetStore.InvalidateAll を 1 度だけ実行 (EditorPrefs key を消すか migration v3 にすると trigger される)
3. MD3SDK Settings ウィンドウなどアイコンを描画するウィンドウを開く
4. MD3_FA_icon.asset が再生成され、Inspector で Atlas Width/Height を確認
5. atlas が 8192x8192 を超えるなら警告 (Task 6 で size 設定検討)
```

Expected: atlas は最大でも 4096x4096 〜 8192x8192 程度。コンパイルエラーなし。

- [ ] **Step 5: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add MD3Icon.cs
git -C "C:\code\unity\unity-md3sdk" commit -m "feat(icon): pre-bake all Material Symbols codepoints into Static FontAsset"
```

---

### Task 6: 手動検証 (Unity 上で WARN 消失とクラッシュ非再現を確認)

**Files:**
- 変更なし (検証のみ)

- [ ] **Step 1: ベースラインリセット**

ユーザー手動:
```
1. Unity を閉じる
2. C:\Users\sakuu\ALCOM\Projects\com.ajisaiflow.vrchat.avater\Assets\MD3SDKFonts\Generated\ を削除
3. 同プロジェクトの Library/ScriptAssemblies を残したまま Unity を起動
   (Library 全削除は重い + 別の問題が出るので避ける)
```

- [ ] **Step 2: 累積試験**

```
1. Unity 起動完了後、UnityAgent ウィンドウを開く
2. 何らかの日本語/絵文字テキストが描画されることを確認
3. C# スクリプトに無害な空白を 1 文字加えて保存 (= forced recompile + domain reload)
4. 上記 3 を **5 回繰り返す**
5. その間、コンソールに以下が出ないことを確認:
   - "Importer(NativeFormatImporter) generated inconsistent result"
   - "ConsistencyChecker -"
6. Unity が **クラッシュしない** ことを確認
```

GO 条件:
- (a) MD3_FA_theme.asset / MD3_FA_icon.asset 由来の inconsistent result 警告が 0 件
- (b) 5 回の domain reload で 1 度もクラッシュしない
- (c) フォント描画が正常 (日本語・絵文字・アイコン)

NG なら STOP しユーザーに報告 (Task 3 のロジック修正 or 案C 転進を相談)。

- [ ] **Step 3: Editor.log を取得**

ユーザー手動:
```
%LOCALAPPDATA%\Unity\Editor\Editor.log (現セッション分) または
%LOCALAPPDATA%\Unity\Editor\Editor-prev.log
を本タスクのコミットに添えてアーカイブ (例: docs/_verification/2026-05-24-editor-log-after-fix.log にコピー)。
```

実際にはログは大きすぎてコミットしない方が良い。WARN 件数とクラッシュ有無を Step 4 で本プランに記録するだけで十分。

- [ ] **Step 4: 検証結果を本プランに追記**

本プラン末尾に以下を追記:

```markdown
## Task 6 Verification Result

- Domain reload 回数: 5
- inconsistent result WARN: 0 件 (期待通り)
- クラッシュ: 0 件
- フォント描画: 正常 (日本語/絵文字/アイコンとも OK)
- 検証日時: 2026-05-24 JST <時刻>
```

- [ ] **Step 5: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add docs/plans/2026-05-24-fontasset-static-hybrid.md
git -C "C:\code\unity\unity-md3sdk" commit -m "docs(font): record verification result for Static hybrid fix"
```

---

### Task 7: CHANGELOG / version bump

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `package.json`
- Modify: `Build~/package.json`

- [ ] **Step 1: CHANGELOG.md に v0.8.4 エントリを追加**

`CHANGELOG.md` の最上部 (v0.8.3 エントリの上) に追加:

```markdown
## [0.8.4] - 2026-05-24

### Fixed
- Unity 2022.3 で `Importer(NativeFormatImporter) generated inconsistent result` 警告が累積し、
  最終的に D3D11 GPU バッファ参照不整合で Unity がクラッシュする問題を構造的に修正。
  根本原因は Unity 公式の既知バグ UUM-69151 (Unity 6 で fix) で、Dynamic な FontAsset を
  `.asset` として永続化していると `TextEditorResourceManager.DoPostRenderUpdates` が
  `ImportAsset(path)` を呼び、AssetDatabase V2 が同 input・同 contentHash に対して
  異なる artifactId を生成することで分裂が発生する。
- `MD3FontAssetStore` を Static main + memory-only Dynamic fallback 構造に再設計。
  main FontAsset は ASCII printable のみ事前焼きして `AtlasPopulationMode.Static` で固定し、
  動的文字 (日本語・絵文字・新規漢字) はすべて `HideFlags.DontSave` の memory-only
  Dynamic fallback で受ける。これにより main の atlas が dirty 化せず、
  `DoPostRenderUpdates` が `ImportAsset` を呼ばなくなる。
- Material Symbols アイコンフォントは生成時に全 PUA codepoint (4000+) を `TryAddCharacters`
  で事前焼きしてから Static 固定するため、ランタイムでの atlas 拡張が発生しなくなった。
- 旧バージョン (v0.8.3 以前) で生成済みの Dynamic 永続化アセットは migration v2 で
  自動削除して作り直す (`Assets/MD3SDKFonts/Generated/` 配下)。

### Note
- Unity 6000.0.x 以降を使う場合は本修正は不要 (Unity 自身が UUM-69151 を fix 済み)。
  ただし本修正は Unity 6 でも動作する。
```

- [ ] **Step 2: バージョン bump (package.json)**

両方を `0.8.3` → `0.8.4` に変更:
- `C:\code\unity\unity-md3sdk\package.json`
- `C:\code\unity\unity-md3sdk\Build~\package.json`

- [ ] **Step 3: コミット**

```bash
git -C "C:\code\unity\unity-md3sdk" add CHANGELOG.md package.json Build~/package.json
git -C "C:\code\unity\unity-md3sdk" commit -m "chore: bump version to 0.8.4"
```

---

### Task 8: PR 作成 / リリース判断

**Files:**
- 変更なし

- [ ] **Step 1: ブランチを push して PR を作成**

```bash
git -C "C:\code\unity\unity-md3sdk" push -u origin fix/fontasset-static-hybrid
gh -R lighfu/unity-md3sdk pr create --title "fix(font): Static main + memory-only Dynamic fallback to dodge UUM-69151" --body "$(cat <<'EOF'
## Summary
- Unity 2022.3 で v0.8.3 リリース後も再発した `ConsistencyChecker` WARN + D3D11 クラッシュを構造的に解消。
- 根本原因: Unity 公式バグ UUM-69151 (Dynamic FontAsset の AssetDatabase 永続化で artifactId 分裂)。Unity 6 で fix 済みだが 2022.3 では未修正。
- 対策 (案F ハイブリッド): main FontAsset を Static で永続化し、動的文字は memory-only Dynamic fallback (`HideFlags.DontSave`) に逃がす。
- アイコンフォントは全 Material Symbols codepoint を事前焼き。

## Test plan
- [ ] Generated/ 削除 → Unity 起動 → UnityAgent オープン → 日本語/絵文字描画確認
- [ ] Domain reload を 5 回繰り返し、ConsistencyChecker WARN が 0 件であること
- [ ] 同上で Unity がクラッシュしないこと
- [ ] アイコン描画が正常 (Material Symbols の任意のアイコン)
- [ ] フォント設定変更時の cache clear が動作

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

- [ ] **Step 2: PR URL をユーザーに返却**

PR 作成後、URL をユーザーに伝える。リリース (`/release`) は別タスクとして相談 (PR マージ後、main からリリース)。

---

## Self-Review Checklist

- [x] **Spec coverage**: 案F の 4 つの構成要素 (main Static, icon Static + 全 codepoint, memory-only fallback, no SetDirty) が Task 3-5 で実装される
- [x] **Placeholder scan**: 全 Step に実コードまたは実コマンド記載済み
- [x] **Type consistency**: `GetOrCreate` (旧 API 維持) と `GetOrCreateIconFont` (新規) のシグネチャを Task 3 で定義し、Task 5 で同じ名前で呼んでいる
- [x] **Spike gating**: Task 1 で「NG なら STOP」が明示され、案C 転進パスを残している
- [x] **手動検証の明示**: Editor 専用機能なので自動テストは書かず、Task 1 と Task 6 で具体的な手動手順と GO/NG 条件を記載

---

## Open Risks

1. **Static FontAsset でも TextEditorResourceManager が ImportAsset を呼ぶ可能性** — Task 1 のスパイクで Go/No-Go 判定する。
2. **memory-only fallback を main に動的代入したとき、main 自体が dirty 化される可能性** — `EditorUtility.SetDirty(main)` を呼ばないことで回避するが、Unity 内部で `fallbackFontAssetTable` の代入が暗黙に dirty 化する可能性が残る。Task 1 で確認する。
3. **アイコン atlas サイズが Unity の上限 (8192x8192) を超える可能性** — Task 5 Step 4 で実測。超えるなら `TryAddCharacters` を分割してマルチ atlas にする (`isMultiAtlasTexturesEnabled = true` で自動分割) の検討が必要。
4. **マイグレーション v2 と既存ユーザーのワークフロー衝突** — `EditorPrefs` で 1 度だけ実行されるが、エラー時は flag を立てないので次回再試行される。安全。

---

## Task 1 Spike Result

- 実行日時: 2026-05-24 JST
- スパイクスクリプト: `Assets/MD3SDKFonts/Spike/FontAssetStaticSpike.cs` (検証後に削除済み)
- 結果:
  - **Spike_Static.asset 由来の `inconsistent result` 警告: 0 件 ✅**
  - 報告された警告は既存の `MD3_FA_theme.asset` (旧 Dynamic アセット、未マイグレーション) のみ
  - 日本語/絵文字描画: OK (memory-only Dynamic fallback が機能) ✅
  - Unity クラッシュ: なし ✅
- 判定: **GO** — 案F は技術的に成立。Task 3 の本実装に進む。
- 既存 `MD3_FA_theme.asset` 由来の警告は Task 3 の migration v2 で自動削除されることで消える見込み。

---

## Task 6 Verification Result

- 実行日時: 2026-05-24 JST
- 検証環境: Unity 2022.3.22f1, ALCOM プロジェクト (com.ajisaiflow.vrchat.avater)
- 実施操作:
  1. `Assets/MD3SDKFonts/Generated/` を削除 (Unity MCP `DeleteAsset`)
  2. `TriggerDomainReload` を複数回実行
  3. UnityAgent ウィンドウを開いて描画確認
- 結果:
  - **`Importer(NativeFormatImporter) generated inconsistent result` WARN: 0 件 ✅** (案F の主目的達成)
  - **`[MD3FontAssetStore] Icon atlas could not contain all codepoints` WARN: 0 件 ✅** (atlas overflow も解消)
  - **Unity クラッシュ: なし ✅**
  - 日本語/絵文字描画: 正常 (memory-only Dynamic fallback が機能)
  - MD3SDK の `MD3Icon.*` 定数経由のアイコン描画: 正常
  - 例外: UnityAgent 側 `ToolbarPanel.cs:83` で `""` という古い Material Symbols codepoint が直書きされており、現行フォントにグリフが存在しないため □ 表示。`MD3Icon.History`（`""`）への置き換えが必要だが、これは UnityAgent 側のバグで MD3SDK の責務外。
- 適用された修正コミット:
  - `840ba60` feat(font): rewrite MD3FontAssetStore to Static main + memory-only Dynamic fallback
  - `ffcb924` fix(font): destroy old fallbacks, refresh windows after migration, wire MD3Icon to GetOrCreateIconFont
  - `e8c2e0d` fix(font): enable multi-atlas for icons, defer fallback destroy, log delete failures
  - `e780f93` fix(font): use full CreateFontAsset overload for icon to fit all 4000+ codepoints
- 判定: **PR 作成可** (案F は技術的に成立、Unity 2022.3 での WARN/クラッシュを構造的に解消)
