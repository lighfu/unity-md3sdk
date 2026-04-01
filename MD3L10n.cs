using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AjisaiFlow.MD3SDK.Editor
{
    internal static class MD3L10n
    {
        internal static readonly string[] LangCodes = { "auto", "ja", "en", "ko", "zh-CN", "zh-TW" };
        internal static readonly string[] LangNames = { "自動 (Auto)", "日本語", "English", "한국어", "中文(简体)", "中文(繁體)" };

        static string _lang;
        static Dictionary<string, Dictionary<string, string>> _translations;

        internal static string RawLang
        {
            get => _lang ?? (_lang = EditorPrefs.GetString("MD3SDK_Language", "auto"));
            set { _lang = value; EditorPrefs.SetString("MD3SDK_Language", value); }
        }

        internal static string CurrentLang
        {
            get
            {
                var raw = RawLang;
                return raw == "auto" ? DetectSystemLanguage() : raw;
            }
        }

        internal static int LangIndex
        {
            get
            {
                var raw = RawLang;
                for (int i = 0; i < LangCodes.Length; i++)
                    if (LangCodes[i] == raw) return i;
                return 0;
            }
            set
            {
                if (value >= 0 && value < LangCodes.Length)
                    RawLang = LangCodes[value];
            }
        }

        internal static string M(string ja)
        {
            if (CurrentLang == "ja") return ja;
            EnsureTranslations();
            if (_translations.TryGetValue(CurrentLang, out var dict))
                if (dict.TryGetValue(ja, out var t)) return t;
            return ja;
        }

        static string DetectSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.English: return "en";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified: return "zh-CN";
                case SystemLanguage.ChineseTraditional: return "zh-TW";
                default: return "en";
            }
        }

        static void EnsureTranslations()
        {
            if (_translations != null) return;
            var en = new Dictionary<string, string>();
            var ko = new Dictionary<string, string>();
            var zhCN = new Dictionary<string, string>();
            var zhTW = new Dictionary<string, string>();
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = en, ["ko"] = ko, ["zh-CN"] = zhCN, ["zh-TW"] = zhTW
            };

            void T(string ja, string e, string k, string cn, string tw)
            {
                en[ja] = e; ko[ja] = k; zhCN[ja] = cn; zhTW[ja] = tw;
            }

            // ── セクションラベル ──
            T("言語", "Language", "언어", "语言", "語言");
            T("アイコンフォント", "Icon Font", "아이콘 폰트", "图标字体", "圖示字體");
            T("フォント管理", "Font Management", "폰트 관리", "字体管理", "字體管理");
            T("絵文字", "Emoji", "이모지", "表情符号", "表情符號");
            T("プレビュー", "Preview", "미리보기", "预览", "預覽");

            // ── ボタン / チップ ──
            T("ダウンロード", "Download", "다운로드", "下载", "下載");
            T("インストール済み", "Installed", "설치됨", "已安装", "已安裝");
            T("使用中", "Active", "사용 중", "使用中", "使用中");
            T("使用する", "Use", "사용", "使用", "使用");
            T("削除", "Remove", "삭제", "删除", "刪除");

            // ── 推奨フォント ──
            T("推奨:", "Recommended:", "추천:", "推荐:", "推薦:");

            // ── アイコンフォント ステータス ──
            T("アイコンフォントを削除しました",
                "Icon font removed", "아이콘 폰트를 삭제했습니다",
                "已删除图标字体", "已刪除圖示字體");
            T("アイコンフォントをダウンロード中...",
                "Downloading icon font...", "아이콘 폰트 다운로드 중...",
                "正在下载图标字体...", "正在下載圖示字體...");
            T("アイコンフォントのインストールが完了しました",
                "Icon font installed successfully", "아이콘 폰트 설치가 완료되었습니다",
                "图标字体安装完成", "圖示字體安裝完成");
            T("アイコンフォントのダウンロードに失敗しました",
                "Icon font download failed", "아이콘 폰트 다운로드에 실패했습니다",
                "图标字体下载失败", "圖示字體下載失敗");

            // ── 絵文字 ステータス ──
            T("絵文字フォントを有効にしました",
                "Emoji font enabled", "이모지 폰트를 활성화했습니다",
                "已启用表情符号字体", "已啟用表情符號字體");
            T("絵文字フォントを無効にしました",
                "Emoji font disabled", "이모지 폰트를 비활성화했습니다",
                "已禁用表情符号字体", "已停用表情符號字體");
            T("絵文字フォントを削除しました",
                "Emoji font removed", "이모지 폰트를 삭제했습니다",
                "已删除表情符号字体", "已刪除表情符號字體");

            // ── フォーマット文字列 (string.Format で使用) ──
            T("{0} をダウンロード中...",
                "Downloading {0}...", "{0} 다운로드 중...",
                "正在下载 {0}...", "正在下載 {0}...");
            T("{0} のインストールが完了しました",
                "{0} installed successfully", "{0} 설치가 완료되었습니다",
                "{0} 安装完成", "{0} 安裝完成");
            T("{0} のダウンロードに失敗しました",
                "{0} download failed", "{0} 다운로드에 실패했습니다",
                "{0} 下载失败", "{0} 下載失敗");
            T("{0} をプライマリにしました",
                "{0} set as primary", "{0}을(를) 기본으로 설정했습니다",
                "已将 {0} 设为主字体", "已將 {0} 設為主字體");
            T("{0} を削除しました",
                "{0} removed", "{0}을(를) 삭제했습니다",
                "已删除 {0}", "已刪除 {0}");

            // ── フォントカテゴリ ──
            T("基本", "Basic", "기본", "基本", "基本");
            T("東アジア", "East Asia", "동아시아", "东亚", "東亞");
            T("南アジア", "South Asia", "남아시아", "南亚", "南亞");
            T("東南アジア", "Southeast Asia", "동남아시아", "东南亚", "東南亞");
            T("中東", "Middle East", "중동", "中东", "中東");
            T("その他", "Other", "기타", "其他", "其他");
        }
    }
}
