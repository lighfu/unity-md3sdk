# FontAsset 永続化による「歯抜けテキスト」根本対策 — 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** UnityAgent / MD3SDK で実行時生成される動的 FontAsset をディスクアセットとして永続化し、ドメインリロードでアトラステクスチャが破棄されて文字が「歯抜け」になるバグを根本的に解消する。

**Architecture:** 新ユーティリティ `MD3FontAssetStore` が `FontAsset`・atlasTexture・material を `.asset` サブアセットとして `Assets/MD3SDKFonts/Generated/` に保存する。ディスクアセットはドメインリロードを生き延びるため、リロード後はロードし直すだけでアトラスが無傷で復帰する。`MD3Theme` / `MD3Icon` をこのストア経由に切り替え、不要になったリトライ/race 回避機構を削減する。

**Tech Stack:** Unity Editor, UI Toolkit (UIElements), TextCore (`UnityEngine.TextCore.Text.FontAsset`), `AssetDatabase`, C#。対象 2 リポジトリ: `unity-md3sdk`(branch `fix/fontasset-persistence`)・`unity-agent`。

---

## 補足: 検証方針

Unity Editor のフォント描画はヘッドレスな自動テストができない（ドメインリロードと目視確認が必須）。本計画では各実装タスクで **コンパイル確認**（Unity 再コンパイルで 0 エラー）を行い、Task 6 で **機能検証**（永続化チェックスクリプト＋手動確認）を行う。`unity-agent` の `EditorStateTools`（コンパイル状態取得・強制ドメインリロード）を検証に利用できる。

## 設計上の前提（重要）

- `MD3Theme.ClearFontCache()` / `MD3Icon.ClearCache()` に `MD3FontAssetStore.InvalidateAll()`（生成アセット削除）を組み込む。これらは **フォント設定変更時のみ** 呼ばれるべき関数である。
- 現状コードはこれらを **ドメインリロードのたび** にも呼んでいる（`MD3FontAutoSetup.OnAfterAssemblyReload`、`CheckAndDownload` 冒頭、`UnityAgentWindow.CreateGUI`）。これを残すと **リロードごとに永続アセットが削除・再生成され、根本対策が無効化される**。
- したがって **Task 4・Task 5 は任意の整理ではなく必須**。Task 2→3→4→5 の順で実施すること。

## ファイル構成

| ファイル | 責務 | タスク |
|---|---|---|
| `unity-md3sdk/MD3FontAssetStore.cs` | FontAsset の永続化（生成・保存・ロード・無効化） | Task 1（新規） |
| `unity-md3sdk/MD3Theme.cs` | テーマフォントの読み込みをストア経由に。無効化フックを追加 | Task 2 |
| `unity-md3sdk/MD3Icon.cs` | アイコンフォントの読み込みをストア経由に。`ProtectFontAsset` 削除 | Task 3 |
| `unity-md3sdk/MD3FontManager.cs` | `MD3FontAutoSetup` の race 回避コード削除 | Task 4 |
| `unity-agent/Editor/Core/UnityAgentWindow.cs` | `CreateGUI()` の歯抜け緩和ブロック簡素化 | Task 5 |

---

## Task 1: `MD3FontAssetStore` を新規作成

**Files:**
- Create: `unity-md3sdk/MD3FontAssetStore.cs`

このタスクは新規ファイルのみで、既存コードからは未参照のため単独でコンパイルが通る。

- [ ] **Step 1: `MD3FontAssetStore.cs` を作成**

以下の内容で新規ファイルを作成する:

```csharp
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
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path);

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
}
```

- [ ] **Step 2: コンパイル確認**

Unity Editor にフォーカスを移し再コンパイルさせる。Console（Ctrl+Shift+C）を確認。
Expected: `MD3FontAssetStore.cs` 関連のコンパイルエラーが 0 件。

- [ ] **Step 3: コミット**

```bash
cd C:/code/unity/unity-md3sdk
git add MD3FontAssetStore.cs MD3FontAssetStore.cs.meta
git commit -m "feat: add MD3FontAssetStore for FontAsset disk persistence"
```

（`.cs.meta` は Unity が生成する。未生成ならこのコミットでは省略し、後続コミットに含める。）

---

## Task 2: `MD3Theme` をストア経由に切り替え

**Files:**
- Modify: `unity-md3sdk/MD3Theme.cs`（`ClearFontCache` / `LoadFontAsset` / `IsFontAssetBroken`）

**前提:** Task 1 完了済み。

- [ ] **Step 1: `ClearFontCache` に無効化フックを追加**

`MD3Theme.cs` の `ClearFontCache`（現 303-309 行）を以下で置換:

置換前:
```csharp
        public static void ClearFontCache()
        {
            // DestroyImmediate しない — UI が参照中の FontAsset を破棄するとテキストが消える
            // 旧インスタンスは GC に任せ、次回 ApplyTo で新しい FontAsset を生成する
            s_fontAsset = null;
            s_font = null;
        }
```

置換後:
```csharp
        public static void ClearFontCache()
        {
            // フォント設定変更時に呼ばれる。static キャッシュをクリアし、
            // 永続化済みの生成 FontAsset アセットも削除して再生成を促す。
            s_fontAsset = null;
            s_font = null;
            MD3FontAssetStore.InvalidateAll();
        }
```

- [ ] **Step 2: `LoadFontAsset` をストア経由に置換**

`MD3Theme.cs` の `LoadFontAsset`（現 335-400 行、`static FontAsset LoadFontAsset()` 全体）を以下で置換:

置換後:
```csharp
        static FontAsset LoadFontAsset()
        {
            // ドメインリロードで static 参照が破棄されていたらクリア
            if (s_fontAsset != null && !s_fontAsset) { s_fontAsset = null; s_font = null; }
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

            // ディスク永続化された FontAsset を取得（ドメインリロード耐性あり）
            var fa = MD3FontAssetStore.GetOrCreate("theme", baseFont, fallbacks);
            if (fa == null)
            {
                ScheduleRefreshRetry();
                return null;
            }

            s_fontAsset = fa;
            return s_fontAsset;
        }
```

- [ ] **Step 3: 不要になった `IsFontAssetBroken` を削除**

`MD3Theme.cs` の `IsFontAssetBroken`（現 402-440 行、`/// <summary>` コメントから `static bool IsFontAssetBroken(FontAsset fa)` のメソッド本体・閉じ括弧まで全体）を削除する。Step 2 の置換後、このメソッドは未参照になる（破損検知は `MD3FontAssetStore` 内部に移管済み）。

削除対象（先頭と末尾）:
```csharp
        /// <summary>
        /// FontAsset の内部テクスチャが破棄されているか判定。
        /// ... (中略) ...
        static bool IsFontAssetBroken(FontAsset fa)
        {
            ... (中略) ...
        }
```

`ScheduleRefreshRetry` / `s_refreshRetryScheduled` / `LoadFontAssetPublic` / `LoadFont` は **残す**（起動時の AssetDatabase 未準備に備えた軽量な安全網）。

- [ ] **Step 4: コンパイル確認**

Unity を再コンパイル。Console を確認。
Expected: 0 エラー。特に `IsFontAssetBroken` の未定義参照が無いこと（あれば削除し残しがある）。

- [ ] **Step 5: コミット**

```bash
cd C:/code/unity/unity-md3sdk
git add MD3Theme.cs
git commit -m "refactor: route MD3Theme font loading through MD3FontAssetStore"
```

---

## Task 3: `MD3Icon` をストア経由に切り替え

**Files:**
- Modify: `unity-md3sdk/MD3Icon.cs`（`ClearCache` / `EnsureFont` / `EnsureFilledFont` / `IsFontAssetBroken` / `ProtectFontAsset`）

**前提:** Task 1・Task 2 完了済み（Task 2 で `MD3Theme` 側の `ProtectFontAsset` 参照が消えていること）。

- [ ] **Step 1: `ClearCache` に無効化フックを追加**

`MD3Icon.cs` の `ClearCache`（現 19-25 行）を以下で置換:

置換後:
```csharp
        /// <summary>フォントキャッシュをクリア。ダウンロード後に呼ぶと次回描画で再読み込みされる。</summary>
        public static void ClearCache()
        {
            s_font = null;
            s_fontAsset = null;
            s_filledFont = null;
            s_filledFontAsset = null;
            MD3FontAssetStore.InvalidateAll();
        }
```

- [ ] **Step 2: `EnsureFont` をストア経由に置換**

`MD3Icon.cs` の `EnsureFont`（現 4302-4340 行）を以下で置換:

置換後:
```csharp
        static void EnsureFont()
        {
            if (s_fontAsset != null && !s_fontAsset) { s_fontAsset = null; s_font = null; }
            if (s_fontAsset != null) return;

            if (s_font == null)
            {
                var guids = AssetDatabase.FindAssets("MaterialSymbolsOutlined t:Font");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".ttf")) continue;
                    if (path.Contains("Filled")) continue; // Skip filled variant
                    if (!path.Contains("MD3SDK") && !path.Contains("net.ajisaiflow.md3sdk") && !path.Contains("MD3SDKFonts")) continue;
                    s_font = AssetDatabase.LoadAssetAtPath<Font>(path);
                    if (s_font != null) break;
                }
            }

            if (s_font != null)
                s_fontAsset = MD3FontAssetStore.GetOrCreate("icon", s_font, null);

            if (s_fontAsset != null)
            {
                s_retryScheduled = false;
                return;
            }

            // フォント未検出 / 生成失敗 — AssetDatabase 準備中の可能性。1 回だけ遅延リトライ。
            if (!s_retryScheduled)
            {
                s_retryScheduled = true;
                EditorApplication.delayCall += () =>
                {
                    s_retryScheduled = false;
                    MD3FontManager.RefreshAllWindows();
                };
            }
        }
```

- [ ] **Step 3: 不要になった `IsFontAssetBroken` を削除**

`MD3Icon.cs` の `IsFontAssetBroken`（現 4342-4355 行、メソッド全体）を削除する。Step 2・Step 4 の置換後、未参照になる。

削除対象:
```csharp
        static bool IsFontAssetBroken(FontAsset fa)
        {
            try
            {
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
```

- [ ] **Step 4: 不要になった `ProtectFontAsset` を削除**

`MD3Icon.cs` の `ProtectFontAsset`（現 4357-4370 行、`/// <summary>` コメントからメソッド本体まで）を削除する。

削除対象:
```csharp
        /// <summary>
        /// FontAsset とその内部 atlasTexture に HideAndDontSave を設定し、
        /// Resources.UnloadUnusedAssets() による破棄を防止する。
        /// </summary>
        internal static void ProtectFontAsset(FontAsset fa)
        {
            if (fa == null) return;
            fa.hideFlags = HideFlags.HideAndDontSave;
            if (fa.atlasTextures != null)
                foreach (var tex in fa.atlasTextures)
                    if (tex != null) tex.hideFlags = HideFlags.HideAndDontSave;
            if (fa.material != null)
                fa.material.hideFlags = HideFlags.HideAndDontSave;
        }
```

- [ ] **Step 5: `EnsureFilledFont` をストア経由に置換**

`MD3Icon.cs` の `EnsureFilledFont`（現 4404-4430 行、メソッド全体）を以下で置換:

置換後:
```csharp
        static void EnsureFilledFont()
        {
            if (s_filledFontAsset != null && !s_filledFontAsset) { s_filledFontAsset = null; s_filledFont = null; }
            if (s_filledFontAsset != null) return;

            if (s_filledFont == null)
            {
                var guids = AssetDatabase.FindAssets("MaterialSymbolsOutlinedFilled t:Font");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".ttf")) continue;
                    if (!path.Contains("MD3SDK") && !path.Contains("net.ajisaiflow.md3sdk") && !path.Contains("MD3SDKFonts")) continue;
                    s_filledFont = AssetDatabase.LoadAssetAtPath<Font>(path);
                    if (s_filledFont != null) break;
                }
            }

            if (s_filledFont != null)
                s_filledFontAsset = MD3FontAssetStore.GetOrCreate("icon-filled", s_filledFont, null);

            // Filled フォントが無い / 生成失敗 — Outlined フォントにフォールバック
            if (s_filledFontAsset == null)
            {
                EnsureFont();
                s_filledFont = s_font;
                s_filledFontAsset = s_fontAsset;
            }
        }
```

- [ ] **Step 6: コンパイル確認**

Unity を再コンパイル。Console を確認。
Expected: 0 エラー。`ProtectFontAsset` / `IsFontAssetBroken` の未定義参照が無いこと（あれば Task 2 の削除し残し、または他参照あり — grep で確認）。

- [ ] **Step 7: コミット**

```bash
cd C:/code/unity/unity-md3sdk
git add MD3Icon.cs
git commit -m "refactor: route MD3Icon fonts through MD3FontAssetStore, drop ProtectFontAsset"
```

---

## Task 4: `MD3FontManager` の race 回避コードを削除（必須）

**Files:**
- Modify: `unity-md3sdk/MD3FontManager.cs`（`MD3FontAutoSetup` 静的コンストラクタ / `OnAfterAssemblyReload` / `CheckAndDownload` / `RefreshAllWindows` コメント）

**前提:** Task 2 完了済み（`ClearFontCache` が `InvalidateAll` を呼ぶ状態）。
**重要:** `OnAfterAssemblyReload` はリロード毎に `ClearFontCache()` を呼ぶ。Task 2 適用後はこれが永続アセットを毎リロード削除してしまうため、削除は必須。

- [ ] **Step 1: 静的コンストラクタから `afterAssemblyReload` 購読を削除**

`MD3FontManager.cs` の `MD3FontAutoSetup()` 静的コンストラクタ（現 25-35 行）を以下で置換:

置換後:
```csharp
        static MD3FontAutoSetup()
        {
            // EditorApplication.delayCall で AssetDatabase 準備完了後に実行
            EditorApplication.delayCall += CheckAndDownload;
        }
```

- [ ] **Step 2: `OnAfterAssemblyReload` メソッドを削除**

`MD3FontManager.cs` の `OnAfterAssemblyReload`（現 37-41 行）を削除:

削除対象:
```csharp
        static void OnAfterAssemblyReload()
        {
            MD3Theme.ClearFontCache();
            MD3Icon.ClearCache();
        }
```

- [ ] **Step 3: `CheckAndDownload` 冒頭の `RefreshAllWindows` を削除**

`MD3FontManager.cs` の `CheckAndDownload`（現 43-48 行付近）の冒頭を以下で置換:

置換前:
```csharp
        static void CheckAndDownload()
        {
            // ドメインリロード後: FontAsset キャッシュをクリアし全ウィンドウを再描画
            // (static フィールドはリセットされるが、UI 要素が破棄済み FontAsset を参照し続ける)
            MD3FontManager.RefreshAllWindows();

            // 1. アイコンフォント (UI 表示に必須)
```

置換後:
```csharp
        static void CheckAndDownload()
        {
            // 1. アイコンフォント (UI 表示に必須)
```

- [ ] **Step 4: `RefreshAllWindows` の古いコメントを更新**

`MD3FontManager.cs` の `RefreshAllWindows` 内のコメント（現 616-618 行付近）を以下で置換:

置換前:
```csharp
            // 全 EditorWindow の rootVisualElement に対して FontAsset を再適用
            // Repaint() だけでは UI 要素が旧(破損した) FontAsset を参照し続け、
            // 一部の文字が透明になる問題が発生する
```

置換後:
```csharp
            // 全 EditorWindow の rootVisualElement に新しい FontAsset を再適用する。
            // (フォント設定変更後、新フォントを全 MD3 ウィンドウへ伝播させるため)
```

`RefreshAllWindows` メソッド本体・`InvalidateAll` 未追加（`ClearFontCache` 経由で呼ばれる）はそのまま。

- [ ] **Step 5: コンパイル確認**

Unity を再コンパイル。Console を確認。
Expected: 0 エラー。`OnAfterAssemblyReload` の未定義参照が無いこと。

- [ ] **Step 6: コミット**

```bash
cd C:/code/unity/unity-md3sdk
git add MD3FontManager.cs
git commit -m "refactor: drop domain-reload race workarounds from MD3FontAutoSetup"
```

---

## Task 5: `UnityAgentWindow` の歯抜け緩和ブロックを簡素化（必須）

**Files:**
- Modify: `unity-agent/Editor/Core/UnityAgentWindow.cs`（`CreateGUI()` 250-257 行）

**前提:** Task 2 完了済み。
**重要:** `CreateGUI()` の `ClearFontCache()` は Task 2 適用後、ウィンドウ生成毎に永続アセットを削除・再生成してしまうため、削除は必須。

- [ ] **Step 1: `unity-agent` にブランチを作成**

```bash
cd C:/code/unity/unity-agent
git checkout -b fix/fontasset-persistence
```

（既に同名ブランチがあれば `git checkout fix/fontasset-persistence`。）

- [ ] **Step 2: 緩和ブロックを簡素化**

`unity-agent/Editor/Core/UnityAgentWindow.cs` の `CreateGUI()` 内（現 250-257 行）を以下で置換:

置換前:
```csharp
            // Domain reload 後、MD3FontAutoSetup の delayCall を待たずに FontAsset を
            // 再生成する。ApplyTo 前に ClearFontCache して fresh な atlas を保証し、
            // ApplyTo 後に RefreshAllWindows で全 MD3 ウィンドウに同じ FontAsset を
            // 伝播させることで、リロード直後の UIRStylePainter.DrawTextInfo NullRef
            // と歯抜けテキストを防ぐ。
            MD3Theme.ClearFontCache();
            _theme.ApplyTo(rootVisualElement);
            MD3FontManager.RefreshAllWindows();
```

置換後:
```csharp
            // FontAsset は MD3FontAssetStore によりディスクアセットとして永続化されており
            // ドメインリロードを生き延びるため、テーマ (フォント定義含む) を適用するだけでよい。
            _theme.ApplyTo(rootVisualElement);
```

- [ ] **Step 3: 未使用 using の確認**

`MD3FontManager` が `UnityAgentWindow.cs` 内で他に使われていなければ、`using` ディレクティブの整理は不要（`MD3FontManager` は型名直接参照のため using 追加は元々無い想定）。コンパイルエラーが出た場合のみ対応する。

- [ ] **Step 4: コンパイル確認**

Unity を再コンパイル。Console を確認。
Expected: 0 エラー。

- [ ] **Step 5: コミット**

```bash
cd C:/code/unity/unity-agent
git add Editor/Core/UnityAgentWindow.cs
git commit -m "refactor: simplify font mitigation in UnityAgentWindow.CreateGUI

FontAsset is now persisted to disk by MD3FontAssetStore and survives
domain reload, so the ClearFontCache + RefreshAllWindows workaround is
no longer needed."
```

---

## Task 6: 機能検証

**Files:** なし（検証のみ）

**前提:** Task 1〜5 完了済み。

- [ ] **Step 1: 生成アセットの永続化を確認**

UnityAgent ウィンドウ（`UnityAgent > UnityAgent`）を開く。Project ウィンドウで `Assets/MD3SDKFonts/Generated/` を確認。
Expected: `MD3_FA_theme.asset`・`MD3_FA_icon.asset` 等が生成され、各アセットを展開すると `Atlas 0`（Texture2D）と `Material` がサブアセットとして含まれる。

- [ ] **Step 2: 永続化チェックスクリプトを実行**

Unity の任意の方法（`unity-agent` の RunEditorScript 系ツール、または一時的なメニュー項目）で以下を実行:

```csharp
var fa = AjisaiFlow.MD3SDK.Editor.MD3Theme.LoadFontAssetPublic();
UnityEngine.Debug.Log(
    $"[Verify] fontAsset persisted = {UnityEditor.AssetDatabase.Contains(fa)}, " +
    $"atlas persisted = {UnityEditor.AssetDatabase.Contains(fa.atlasTextures[0])}, " +
    $"fallbacks = {fa.fallbackFontAssetTable.Count}");
```

Expected: `fontAsset persisted = True, atlas persisted = True, fallbacks >= 1`。
（`AssetDatabase.Contains` が True ＝ ディスクアセット＝リロード耐性あり、が確認できる。）

- [ ] **Step 3: ドメインリロード後の歯抜け非再現を確認**

UnityAgent でメッセージを送信し、応答ストリーミング中〜直後にスクリプト再コンパイルを誘発する（`unity-agent` の `EditorStateTools` の強制ドメインリロード、または任意の `.cs` を保存）。
Expected: リロード後もチャット内テキスト（日本語・英数字・ツール呼び出し名）に歯抜け・文字化けが発生しない。`UIRStylePainter.DrawTextInfo` の NullReferenceException も Console に出ない。

- [ ] **Step 4: フォント設定変更時の再生成を確認**

`UnityAgent > MD3 SDK Settings`（または相当の設定画面）でアクティブフォントを変更する。
Expected: `Generated/` のアセットが再生成され、UI のフォントが正しく切り替わり、歯抜けが出ない。

- [ ] **Step 5: 検証結果を記録**

Step 1〜4 がすべて Expected どおりなら検証完了。いずれか失敗した場合は systematic-debugging スキルで原因を調査し、該当 Task に戻る。

- [ ] **Step 6: 設計書のステータスを更新してコミット**

`unity-md3sdk/docs/specs/2026-05-22-fontasset-persistence-design.html` の状態チップを「実装完了・検証済み」に更新し:

```bash
cd C:/code/unity/unity-md3sdk
git add docs/specs/2026-05-22-fontasset-persistence-design.html
git commit -m "docs: mark FontAsset persistence design as implemented"
```

---

## 完了後

両リポジトリの `fix/fontasset-persistence` ブランチに変更が乗る。superpowers:finishing-a-development-branch スキルでマージ / PR 方針を決定する。
