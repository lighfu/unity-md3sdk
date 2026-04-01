using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Noto Sans フォントの自動検出・ダウンロード・管理を行う。
    /// Fonts/ フォルダに配置されたフォントを MD3Theme がフォールバックチェーンとして使用する。
    /// </summary>
    /// <summary>
    /// Editor 起動時に推奨フォントが未インストールなら自動ダウンロードする。
    /// </summary>
    [InitializeOnLoad]
    static class MD3FontAutoSetup
    {
        static MD3FontAutoSetup()
        {
            // EditorApplication.delayCall で AssetDatabase 準備完了後に実行
            EditorApplication.delayCall += CheckAndDownload;
        }

        static void CheckAndDownload()
        {
            // ドメインリロード後: FontAsset キャッシュをクリアし全ウィンドウを再描画
            // (static フィールドはリセットされるが、UI 要素が破棄済み FontAsset を参照し続ける)
            MD3FontManager.RefreshAllWindows();

            // 1. アイコンフォント (UI 表示に必須)
            if (!MD3FontManager.IsIconFontAvailable)
            {
                Debug.Log("[MD3SDK] アイコンフォントを自動ダウンロードします...");
                MD3FontManager.DownloadIconFont(success =>
                {
                    if (success)
                    {
                        Debug.Log("[MD3SDK] アイコンフォントのインストールが完了しました");
                        MD3FontManager.RefreshAllWindows();
                    }
                    else
                        Debug.LogWarning("[MD3SDK] アイコンフォントのダウンロードに失敗しました。Window/紫陽花広場/MD3 SDK Settings から手動でインストールできます。");
                    CheckNotoSans();
                });
                return;
            }
            CheckNotoSans();
        }

        static void CheckNotoSans()
        {
            if (MD3FontManager.GetInstalledFonts().Count > 0)
            {
                CheckEmoji();
                return;
            }

            var recommended = MD3FontManager.DetectRecommendedFont();
            if (!recommended.HasValue)
            {
                CheckEmoji();
                return;
            }

            Debug.Log($"[MD3SDK] 推奨フォント ({recommended.Value.DisplayName}) を自動ダウンロードします...");
            MD3FontManager.DownloadFont(recommended.Value, success =>
            {
                if (success)
                {
                    Debug.Log($"[MD3SDK] {recommended.Value.DisplayName} のインストールが完了しました");
                    MD3FontManager.RefreshAllWindows();
                }
                else
                    Debug.LogWarning($"[MD3SDK] {recommended.Value.DisplayName} のダウンロードに失敗しました。Window/紫陽花広場/MD3 SDK Settings から手動でインストールできます。");
                CheckEmoji();
            });
        }

        static void CheckEmoji()
        {
            if (MD3FontManager.IsEmojiFontInstalled) return;

            Debug.Log("[MD3SDK] Emoji フォントを自動ダウンロードします...");
            MD3FontManager.DownloadEmojiFont(success =>
            {
                if (success)
                {
                    Debug.Log("[MD3SDK] Emoji フォントのインストールが完了しました");
                    MD3FontManager.RefreshAllWindows();
                }
                else
                    Debug.LogWarning("[MD3SDK] Emoji フォントのダウンロードに失敗しました。Window/紫陽花広場/MD3 SDK Settings から手動でインストールできます。");
            });
        }
    }

    public static class MD3FontManager
    {
        // ── ダウンロード元 ──
        const string CjkRepo = "notofonts/noto-cjk";
        const string CjkReleaseTag = "Sans2.004";
        const string GoogleFontsBase = "https://raw.githubusercontent.com/google/fonts/main/ofl/";
        const string EmojiUrl = GoogleFontsBase + "notoemoji/NotoEmoji%5Bwght%5D.ttf";
        const string EmojiFontFile = "NotoEmoji[wght].ttf";

        // ── アイコンフォント ──
        const string IconFontBaseUrl = "https://raw.githubusercontent.com/google/material-design-icons/master/variablefont/";
        const string IconFontFile = "MaterialSymbolsOutlined[FILL,GRAD,opsz,wght].ttf";
        const string IconFontUrl = IconFontBaseUrl + "MaterialSymbolsOutlined%5BFILL%2CGRAD%2Copsz%2Cwght%5D.ttf";
        const string IconCodepointsFile = "MaterialSymbolsOutlined[FILL,GRAD,opsz,wght].codepoints";
        const string IconCodepointsUrl = IconFontBaseUrl + "MaterialSymbolsOutlined%5BFILL%2CGRAD%2Copsz%2Cwght%5D.codepoints";

        /// <summary>ダウンロード方式</summary>
        public enum DownloadType { CjkZip, GoogleFontsTtf }

        public struct FontEntry
        {
            public string LangCode;
            public string DisplayName;
            public string Category;
            public string FontPrefix;
            public DownloadType Type;
            public string DownloadKey; // CjkZip: zip asset name, GoogleFontsTtf: folder/file path

            public FontEntry(string langCode, string displayName, string category,
                string fontPrefix, DownloadType type, string downloadKey)
            {
                LangCode = langCode;
                DisplayName = displayName;
                Category = category;
                FontPrefix = fontPrefix;
                Type = type;
                DownloadKey = downloadKey;
            }
        }

        /// <summary>サポートする全フォント</summary>
        public static readonly FontEntry[] SupportedFonts = new[]
        {
            // ── 基本 (Latin, Cyrillic, Greek) ──
            new FontEntry("und", "Latin / Cyrillic / Greek", "基本",
                "NotoSans", DownloadType.GoogleFontsTtf,
                "notosans/NotoSans%5Bwdth%2Cwght%5D.ttf"),

            // ── 東アジア (CJK) ──
            new FontEntry("ja", "日本語", "東アジア",
                "NotoSansJP", DownloadType.CjkZip, "16_NotoSansJP.zip"),
            new FontEntry("ko", "한국어", "東アジア",
                "NotoSansKR", DownloadType.CjkZip, "17_NotoSansKR.zip"),
            new FontEntry("zh-Hans", "简体中文", "東アジア",
                "NotoSansSC", DownloadType.CjkZip, "18_NotoSansSC.zip"),
            new FontEntry("zh-Hant", "繁體中文", "東アジア",
                "NotoSansTC", DownloadType.CjkZip, "19_NotoSansTC.zip"),
            new FontEntry("zh-HK", "繁體中文(香港)", "東アジア",
                "NotoSansHK", DownloadType.CjkZip, "20_NotoSansHK.zip"),

            // ── 南アジア ──
            new FontEntry("hi", "हिन्दी (Devanagari)", "南アジア",
                "NotoSansDevanagari", DownloadType.GoogleFontsTtf,
                "notosansdevanagari/NotoSansDevanagari%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("bn", "বাংলা (Bengali)", "南アジア",
                "NotoSansBengali", DownloadType.GoogleFontsTtf,
                "notosansbengali/NotoSansBengali%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("ta", "தமிழ் (Tamil)", "南アジア",
                "NotoSansTamil", DownloadType.GoogleFontsTtf,
                "notosanstamil/NotoSansTamil%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("te", "తెలుగు (Telugu)", "南アジア",
                "NotoSansTelugu", DownloadType.GoogleFontsTtf,
                "notosanstelugu/NotoSansTelugu%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("si", "සිංහල (Sinhala)", "南アジア",
                "NotoSansSinhala", DownloadType.GoogleFontsTtf,
                "notosanssinhala/NotoSansSinhala%5Bwdth%2Cwght%5D.ttf"),

            // ── 東南アジア ──
            new FontEntry("th", "ไทย (Thai)", "東南アジア",
                "NotoSansThai", DownloadType.GoogleFontsTtf,
                "notosansthai/NotoSansThai%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("km", "ភាសាខ្មែរ (Khmer)", "東南アジア",
                "NotoSansKhmer", DownloadType.GoogleFontsTtf,
                "notosanskhmer/NotoSansKhmer%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("lo", "ລາວ (Lao)", "東南アジア",
                "NotoSansLao", DownloadType.GoogleFontsTtf,
                "notosanslao/NotoSansLao%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("my", "မြန်မာ (Myanmar)", "東南アジア",
                "NotoSansMyanmar", DownloadType.GoogleFontsTtf,
                "notosansmyanmar/NotoSansMyanmar%5Bwdth%2Cwght%5D.ttf"),

            // ── 中東 ──
            new FontEntry("ar", "العربية (Arabic)", "中東",
                "NotoSansArabic", DownloadType.GoogleFontsTtf,
                "notosansarabic/NotoSansArabic%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("he", "עברית (Hebrew)", "中東",
                "NotoSansHebrew", DownloadType.GoogleFontsTtf,
                "notosanshebrew/NotoSansHebrew%5Bwdth%2Cwght%5D.ttf"),

            // ── その他 ──
            new FontEntry("ka", "ქართული (Georgian)", "その他",
                "NotoSansGeorgian", DownloadType.GoogleFontsTtf,
                "notosansgeorgian/NotoSansGeorgian%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("hy", "Հայերեն (Armenian)", "その他",
                "NotoSansArmenian", DownloadType.GoogleFontsTtf,
                "notosansarmenian/NotoSansArmenian%5Bwdth%2Cwght%5D.ttf"),
            new FontEntry("am", "አማርኛ (Ethiopic)", "その他",
                "NotoSansEthiopic", DownloadType.GoogleFontsTtf,
                "notosansethiopic/NotoSansEthiopic%5Bwdth%2Cwght%5D.ttf"),
        };

        static UnityWebRequest _activeRequest;

        public static bool IsDownloading => _activeRequest != null && !_activeRequest.isDone;
        public static float DownloadProgress => _activeRequest?.downloadProgress ?? 0f;

        /// <summary>カテゴリ一覧を返す (表示順)</summary>
        public static string[] GetCategories()
        {
            var seen = new List<string>();
            foreach (var e in SupportedFonts)
                if (!seen.Contains(e.Category)) seen.Add(e.Category);
            return seen.ToArray();
        }

        /// <summary>指定カテゴリのフォント一覧</summary>
        public static FontEntry[] GetFontsByCategory(string category)
        {
            return SupportedFonts.Where(f => f.Category == category).ToArray();
        }

        /// <summary>インストール済みフォントを検出</summary>
        public static List<FontEntry> GetInstalledFonts()
        {
            var result = new List<FontEntry>();
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return result;

            foreach (var entry in SupportedFonts)
            {
                if (IsFontFilePresent(fontsDir, entry))
                    result.Add(entry);
            }
            return result;
        }

        static bool IsFontFilePresent(string fontsDir, FontEntry entry)
        {
            if (entry.Type == DownloadType.CjkZip)
                return File.Exists(Path.Combine(fontsDir, $"{entry.FontPrefix}-Regular.otf"));

            // GoogleFontsTtf: variable font の場合ファイル名に [] が含まれる
            var fileName = Path.GetFileName(Uri.UnescapeDataString(entry.DownloadKey));
            return File.Exists(Path.Combine(fontsDir, fileName));
        }

        /// <summary>システム言語から推奨フォントを返す</summary>
        public static FontEntry? DetectRecommendedFont()
        {
            var culture = CultureInfo.CurrentUICulture;
            var lang = culture.TwoLetterISOLanguageName;
            var name = culture.Name;

            if (lang == "zh")
            {
                if (name.Contains("HK")) return Find("zh-HK");
                if (name.Contains("TW") || name.Contains("Hant")) return Find("zh-Hant");
                return Find("zh-Hans");
            }

            return Find(lang);
        }

        static FontEntry? Find(string langCode)
        {
            foreach (var e in SupportedFonts)
                if (e.LangCode == langCode) return e;
            return null;
        }

        /// <summary>アクティブフォント (EditorPrefs で永続化)</summary>
        public static string ActiveFontPrefix
        {
            get => EditorPrefs.GetString("MD3SDK_ActiveFont", "");
            set
            {
                EditorPrefs.SetString("MD3SDK_ActiveFont", value);
                MD3Theme.ClearFontCache();
            }
        }

        /// <summary>フォントをダウンロード</summary>
        public static void DownloadFont(FontEntry entry, Action<bool> onComplete = null)
        {
            if (IsDownloading) return;

            string url;
            if (entry.Type == DownloadType.CjkZip)
                url = $"https://github.com/{CjkRepo}/releases/download/{CjkReleaseTag}/{entry.DownloadKey}";
            else
                url = GoogleFontsBase + entry.DownloadKey;

            _activeRequest = UnityWebRequest.Get(url);
            _activeRequest.redirectLimit = 10;
            var op = _activeRequest.SendWebRequest();
            var capturedEntry = entry;
            op.completed += _ =>
            {
                bool success = false;
                try
                {
                    if (_activeRequest.result == UnityWebRequest.Result.Success)
                    {
                        if (capturedEntry.Type == DownloadType.CjkZip)
                            success = ExtractCjkFont(_activeRequest.downloadHandler.data, capturedEntry.FontPrefix);
                        else
                            success = SaveTtfFont(_activeRequest.downloadHandler.data, capturedEntry.DownloadKey);

                        if (success)
                        {
                            // アクティブフォントが未設定ならこのフォントをアクティブにする
                            if (string.IsNullOrEmpty(ActiveFontPrefix))
                                ActiveFontPrefix = capturedEntry.FontPrefix;
                            else
                                MD3Theme.ClearFontCache(); // フォールバック再構築
                            AssetDatabase.Refresh();
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MD3FontManager] Download failed: {_activeRequest.error}");
                    }
                }
                finally
                {
                    _activeRequest.Dispose();
                    _activeRequest = null;
                    onComplete?.Invoke(success);
                }
            };
        }

        /// <summary>フォントを削除</summary>
        public static void RemoveFont(FontEntry entry)
        {
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return;

            if (entry.Type == DownloadType.CjkZip)
            {
                foreach (var weight in new[] { "Regular", "Bold" })
                {
                    DeleteFileAndMeta(Path.Combine(fontsDir, $"{entry.FontPrefix}-{weight}.otf"));
                }
            }
            else
            {
                var fileName = Path.GetFileName(Uri.UnescapeDataString(entry.DownloadKey));
                DeleteFileAndMeta(Path.Combine(fontsDir, fileName));
            }

            if (ActiveFontPrefix == entry.FontPrefix)
                ActiveFontPrefix = "";

            AssetDatabase.Refresh();
        }

        // ── アイコンフォント ──

        /// <summary>ダウンロード済みアイコンフォントが Assets/MD3SDKFonts/ にあるか</summary>
        public static bool IsIconFontInstalled
        {
            get
            {
                var fontsDir = GetFontsDir();
                return fontsDir != null && File.Exists(Path.Combine(fontsDir, IconFontFile));
            }
        }

        /// <summary>アイコンフォントが利用可能か (ダウンロード済み or バンドル)</summary>
        public static bool IsIconFontAvailable
        {
            get
            {
                if (IsIconFontInstalled) return true;
                // バンドル検索 (Assets/ 開発環境 or VPM パッケージ)
                var guids = AssetDatabase.FindAssets("MaterialSymbolsOutlined t:Font");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".ttf") && !path.Contains("Filled") &&
                        (path.Contains("MD3SDK") || path.Contains("net.ajisaiflow.md3sdk")))
                        return true;
                }
                return false;
            }
        }

        /// <summary>アイコンフォント (.ttf + .codepoints) をダウンロード</summary>
        public static void DownloadIconFont(Action<bool> onComplete = null)
        {
            if (IsDownloading) return;

            // 1. フォント本体をダウンロード
            DownloadRawFile(IconFontUrl, IconFontFile, fontSuccess =>
            {
                if (!fontSuccess)
                {
                    onComplete?.Invoke(false);
                    return;
                }
                // 2. codepoints をダウンロード (アイコン名解決用)
                DownloadRawFile(IconCodepointsUrl, IconCodepointsFile, cpSuccess =>
                {
                    AssetDatabase.Refresh();
                    onComplete?.Invoke(cpSuccess);
                });
            });
        }

        /// <summary>単一ファイルを Assets/MD3SDKFonts/ にダウンロード</summary>
        static void DownloadRawFile(string url, string destFileName, Action<bool> onComplete)
        {
            _activeRequest = UnityWebRequest.Get(url);
            _activeRequest.redirectLimit = 10;
            var op = _activeRequest.SendWebRequest();
            op.completed += _ =>
            {
                bool success = false;
                try
                {
                    if (_activeRequest.result == UnityWebRequest.Result.Success)
                    {
                        var fontsDir = GetFontsDir();
                        if (fontsDir != null)
                        {
                            File.WriteAllBytes(Path.Combine(fontsDir, destFileName), _activeRequest.downloadHandler.data);
                            success = true;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MD3FontManager] Download failed ({destFileName}): {_activeRequest.error}");
                    }
                }
                finally
                {
                    _activeRequest.Dispose();
                    _activeRequest = null;
                    onComplete?.Invoke(success);
                }
            };
        }

        /// <summary>ダウンロード済みアイコンフォントを削除</summary>
        public static void RemoveIconFont()
        {
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return;
            DeleteFileAndMeta(Path.Combine(fontsDir, IconFontFile));
            DeleteFileAndMeta(Path.Combine(fontsDir, IconCodepointsFile));
            AssetDatabase.Refresh();
        }

        // ── 絵文字フォント ──

        public static bool IsEmojiFontInstalled
        {
            get
            {
                var fontsDir = GetFontsDir();
                return fontsDir != null && File.Exists(Path.Combine(fontsDir, EmojiFontFile));
            }
        }

        public static bool EmojiEnabled
        {
            get => EditorPrefs.GetBool("MD3SDK_EmojiEnabled", true);
            set
            {
                EditorPrefs.SetBool("MD3SDK_EmojiEnabled", value);
                MD3Theme.ClearFontCache();
            }
        }

        public static void DownloadEmojiFont(Action<bool> onComplete = null)
        {
            if (IsDownloading) return;

            _activeRequest = UnityWebRequest.Get(EmojiUrl);
            _activeRequest.redirectLimit = 10;
            var op = _activeRequest.SendWebRequest();
            op.completed += _ =>
            {
                bool success = false;
                try
                {
                    if (_activeRequest.result == UnityWebRequest.Result.Success)
                    {
                        var fontsDir = GetFontsDir();
                        if (fontsDir != null)
                        {
                            File.WriteAllBytes(Path.Combine(fontsDir, EmojiFontFile), _activeRequest.downloadHandler.data);
                            EmojiEnabled = true;
                            AssetDatabase.Refresh();
                            success = true;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MD3FontManager] Emoji download failed: {_activeRequest.error}");
                    }
                }
                finally
                {
                    _activeRequest.Dispose();
                    _activeRequest = null;
                    onComplete?.Invoke(success);
                }
            };
        }

        public static void RemoveEmojiFont()
        {
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return;

            DeleteFileAndMeta(Path.Combine(fontsDir, EmojiFontFile));
            EmojiEnabled = false;
            AssetDatabase.Refresh();
        }

        public static Font LoadEmojiFont()
        {
            if (!EmojiEnabled) return null;
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return null;
            var unityPath = ToUnityPath(Path.Combine(fontsDir, EmojiFontFile));
            return AssetDatabase.LoadAssetAtPath<Font>(unityPath);
        }

        // ── フォント読み込み ──

        public static Font LoadActiveFont()
        {
            var prefix = ActiveFontPrefix;
            if (string.IsNullOrEmpty(prefix)) return null;
            return LoadFontByPrefix(prefix);
        }

        public static Font LoadFontByPrefix(string prefix)
        {
            // CJK フォント (OTF)
            var guids = AssetDatabase.FindAssets($"{prefix}-Regular t:Font", new[] { FontsDirUnity });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<Font>(path);
            }

            // GoogleFonts TTF (variable font) — FontPrefix で検索
            guids = AssetDatabase.FindAssets($"{prefix} t:Font", new[] { FontsDirUnity });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<Font>(path);
            }

            return null;
        }

        /// <summary>インストール済みの全フォント (アクティブ除く) を読み込む</summary>
        public static List<Font> LoadAllFallbackFonts(string activePrefix)
        {
            var result = new List<Font>();
            foreach (var entry in GetInstalledFonts())
            {
                if (entry.FontPrefix == activePrefix) continue;
                var font = LoadFontByPrefix(entry.FontPrefix);
                if (font != null)
                    result.Add(font);
            }
            return result;
        }

        /// <summary>
        /// フォントキャッシュをクリアし、全 EditorWindow を再描画する。
        /// フォントダウンロード完了後に呼ぶとリアルタイムで UI が更新される。
        /// </summary>
        public static void RefreshAllWindows()
        {
            MD3Theme.ClearFontCache();
            MD3Icon.ClearCache();

            // 全 EditorWindow の rootVisualElement に対して FontAsset を再適用
            // Repaint() だけでは UI 要素が旧(破損した) FontAsset を参照し続け、
            // 一部の文字が透明になる問題が発生する
            var newFontAsset = MD3Theme.LoadFontAssetPublic();
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                var root = w.rootVisualElement;
                if (root == null) continue;

                // md3-dark/md3-light クラスを持つ要素にのみ FontAsset を再適用
                if (root.ClassListContains("md3-dark") || root.ClassListContains("md3-light"))
                {
                    if (newFontAsset != null)
                        root.style.unityFontDefinition = new StyleFontDefinition(newFontAsset);
                }

                w.Repaint();
            }
        }

        // ── 内部 ──

        static bool ExtractCjkFont(byte[] zipData, string fontPrefix)
        {
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return false;

            try
            {
                using (var stream = new MemoryStream(zipData))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    int extracted = 0;
                    foreach (var e in archive.Entries)
                    {
                        if (!e.FullName.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)) continue;
                        var name = Path.GetFileName(e.FullName);
                        if (!name.StartsWith(fontPrefix)) continue;
                        if (!name.Contains("Regular") && !name.Contains("Bold")) continue;

                        var dest = Path.Combine(fontsDir, name);
                        using (var src = e.Open())
                        using (var dst = File.Create(dest))
                            src.CopyTo(dst);
                        extracted++;
                    }
                    return extracted > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MD3FontManager] Extract failed: {ex.Message}");
                return false;
            }
        }

        static bool SaveTtfFont(byte[] data, string downloadKey)
        {
            var fontsDir = GetFontsDir();
            if (fontsDir == null) return false;

            var fileName = Path.GetFileName(Uri.UnescapeDataString(downloadKey));
            File.WriteAllBytes(Path.Combine(fontsDir, fileName), data);
            return true;
        }

        static void DeleteFileAndMeta(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            var meta = path + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
        }

        static string ToUnityPath(string absolutePath)
        {
            var p = absolutePath.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (p.StartsWith(dataPath))
                p = "Assets" + p.Substring(dataPath.Length);
            return p;
        }

        /// <summary>
        /// フォント保存先。VPM パッケージは読み取り専用のため、
        /// パッケージ外の Assets/MD3SDKFonts/ に保存する。
        /// </summary>
        const string FontsDirUnity = "Assets/MD3SDKFonts";

        public static string GetFontsDir()
        {
            var abs = Path.Combine(Application.dataPath, "MD3SDKFonts");
            if (!Directory.Exists(abs))
                Directory.CreateDirectory(abs);
            return abs;
        }
    }
}
