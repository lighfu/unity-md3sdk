using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static AjisaiFlow.MD3SDK.Editor.MD3L10n;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SDKSettingsWindow : EditorWindow
    {
        const string Version = "0.7.0";

        MD3Theme _theme;
        VisualElement _iconContainer;
        VisualElement _fontListContainer;
        VisualElement _emojiContainer;
        MD3Text _statusLabel;

        [MenuItem("Window/紫陽花広場/MD3 SDK Settings")]
        static void Open()
        {
            var w = GetWindow<MD3SDKSettingsWindow>();
            w.titleContent = new GUIContent("MD3 SDK Settings");
            w.minSize = new Vector2(420, 360);
        }

        void CreateGUI()
        {
            _theme = MD3Theme.Auto();

            var themeSheet = MD3Theme.LoadThemeStyleSheet();
            var compSheet = MD3Theme.LoadComponentsStyleSheet();
            if (themeSheet != null) rootVisualElement.styleSheets.Add(themeSheet);
            if (compSheet != null) rootVisualElement.styleSheets.Add(compSheet);

            _theme.ApplyTo(rootVisualElement);

            var scroll = new ScrollView();
            scroll.Grow();
            scroll.Padding(MD3Spacing.L, MD3Spacing.L);
            rootVisualElement.Add(scroll);

            var root = new MD3Column(gap: MD3Spacing.M);
            scroll.Add(root);

            // ═══ バージョン ═══
            root.Add(new MD3SectionLabel("MD3 SDK"));
            root.Add(new MD3Text($"Version {Version}", MD3TextStyle.Body, _theme.OnSurfaceVariant));

            root.Add(new MD3Spacer(MD3Spacing.S));
            root.Add(new MD3Divider());
            root.Add(new MD3Spacer(MD3Spacing.S));

            // ═══ 言語 ═══
            root.Add(new MD3SectionLabel(M("言語")));
            {
                var langRow = new MD3Row(gap: MD3Spacing.S);
                langRow.style.alignItems = Align.Center;
                langRow.style.flexWrap = Wrap.Wrap;
                var langNames = MD3L10n.LangNames;
                for (int i = 0; i < langNames.Length; i++)
                {
                    var idx = i;
                    var selected = idx == MD3L10n.LangIndex;
                    var chip = new MD3Chip(langNames[i], selected: selected);
                    chip.RegisterCallback<ClickEvent>(_ =>
                    {
                        MD3L10n.LangIndex = idx;
                        Rebuild();
                    });
                    langRow.Add(chip);
                }
                root.Add(langRow);
            }

            root.Add(new MD3Spacer(MD3Spacing.S));
            root.Add(new MD3Divider());
            root.Add(new MD3Spacer(MD3Spacing.S));

            // ═══ アイコンフォント ═══
            root.Add(new MD3SectionLabel(M("アイコンフォント")));

            _iconContainer = new MD3Column(gap: MD3Spacing.S);
            root.Add(_iconContainer);
            RefreshIconSection();

            root.Add(new MD3Spacer(MD3Spacing.S));
            root.Add(new MD3Divider());
            root.Add(new MD3Spacer(MD3Spacing.S));

            // ═══ フォント管理 ═══
            root.Add(new MD3SectionLabel(M("フォント管理")));

            // 推奨フォント
            var recommended = MD3FontManager.DetectRecommendedFont();
            if (recommended.HasValue)
            {
                var recRow = new MD3Row(gap: MD3Spacing.S);
                recRow.style.alignItems = Align.Center;
                recRow.Add(new MD3Text($"{M("推奨:")} {recommended.Value.DisplayName}", MD3TextStyle.BodySmall, _theme.OnSurfaceVariant));

                var installed = MD3FontManager.GetInstalledFonts();
                bool recInstalled = installed.Exists(f => f.FontPrefix == recommended.Value.FontPrefix);
                if (!recInstalled)
                {
                    var recBtn = new MD3Button(M("ダウンロード"), MD3ButtonStyle.Filled, size: MD3ButtonSize.Small);
                    var recEntry = recommended.Value;
                    recBtn.clicked += () => StartDownload(recEntry);
                    recRow.Add(recBtn);
                }
                else
                {
                    recRow.Add(new MD3Chip(M("インストール済み"), selected: true));
                }
                root.Add(recRow);
            }

            root.Add(new MD3Spacer(MD3Spacing.XS));

            // カテゴリ別フォント一覧
            _fontListContainer = new MD3Column(gap: MD3Spacing.S);
            root.Add(_fontListContainer);
            RefreshFontList();

            root.Add(new MD3Spacer(MD3Spacing.S));
            root.Add(new MD3Divider());
            root.Add(new MD3Spacer(MD3Spacing.S));

            // ═══ 絵文字フォント ═══
            root.Add(new MD3SectionLabel(M("絵文字")));

            _emojiContainer = new MD3Column(gap: MD3Spacing.S);
            root.Add(_emojiContainer);
            RefreshEmojiSection();

            root.Add(new MD3Spacer(MD3Spacing.S));
            root.Add(new MD3Divider());
            root.Add(new MD3Spacer(MD3Spacing.S));

            // ═══ プレビュー ═══
            root.Add(new MD3SectionLabel(M("プレビュー")));
            root.Add(new MD3Text("Latin: The quick brown fox jumps over the lazy dog", MD3TextStyle.Body));
            root.Add(new MD3Text("日本語: あいうえお カタカナ 漢字表示テスト", MD3TextStyle.Body));
            root.Add(new MD3Text("한국어: 안녕하세요 세계", MD3TextStyle.Body));
            root.Add(new MD3Text("简体中文: 你好世界 测试文本", MD3TextStyle.Body));
            root.Add(new MD3Text("繁體中文: 你好世界 測試文本", MD3TextStyle.Body));
            root.Add(new MD3Text("العربية: مرحبا بالعالم", MD3TextStyle.Body));
            root.Add(new MD3Text("עברית: שלום עולם", MD3TextStyle.Body));
            root.Add(new MD3Text("ไทย: สวัสดีชาวโลก", MD3TextStyle.Body));
            root.Add(new MD3Text("हिन्दी: नमस्ते दुनिया", MD3TextStyle.Body));
            root.Add(new MD3Text("ქართული: გამარჯობა მსოფლიო", MD3TextStyle.Body));
            root.Add(new MD3Text("Emoji: 😀😎🥳👍❤️🌸⭐🎮🎵🍣🗻🐱", MD3TextStyle.Body));
            root.Add(new MD3Text("Mixed: 東京 Seoul 北京 🎉 مرحبا สวัสดี", MD3TextStyle.Body));

            // ステータス
            root.Add(new MD3Spacer(MD3Spacing.S));
            _statusLabel = new MD3Text("", MD3TextStyle.BodySmall, _theme.OnSurfaceVariant);
            root.Add(_statusLabel);
        }

        void Rebuild()
        {
            rootVisualElement.Clear();
            CreateGUI();
        }

        void ReapplyTheme()
        {
            _theme = MD3Theme.Auto();
            _theme.ApplyTo(rootVisualElement);
        }

        void RefreshIconSection()
        {
            _iconContainer.Clear();
            var row = new MD3Row(gap: MD3Spacing.S);
            row.style.alignItems = Align.Center;
            row.Add(new MD3Text("Material Symbols Outlined", MD3TextStyle.Body));
            row.Add(new MD3Spacer());

            if (MD3FontManager.IsIconFontAvailable)
            {
                row.Add(new MD3Chip(M("インストール済み"), selected: true));

                // ダウンロード版のみ削除可能 (バンドル版は削除不可)
                if (MD3FontManager.IsIconFontInstalled)
                {
                    var removeBtn = new MD3Button(M("削除"), MD3ButtonStyle.Text, size: MD3ButtonSize.Small);
                    removeBtn.clicked += () =>
                    {
                        MD3FontManager.RemoveIconFont();
                        _statusLabel.Text = M("アイコンフォントを削除しました");
                        RefreshIconSection();
                    };
                    row.Add(removeBtn);
                }
            }
            else
            {
                var dlBtn = new MD3Button(M("ダウンロード"), MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
                dlBtn.clicked += () =>
                {
                    if (MD3FontManager.IsDownloading) return;
                    _statusLabel.Text = M("アイコンフォントをダウンロード中...");
                    _statusLabel.schedule.Execute(() =>
                    {
                        if (MD3FontManager.IsDownloading)
                        {
                            int pct = Mathf.RoundToInt(MD3FontManager.DownloadProgress * 100);
                            _statusLabel.Text = $"{M("アイコンフォントをダウンロード中...")} {pct}%";
                        }
                    }).Every(200).Until(() => !MD3FontManager.IsDownloading);

                    MD3FontManager.DownloadIconFont(success =>
                    {
                        if (success)
                        {
                            MD3FontManager.RefreshAllWindows();
                            Rebuild();
                        }
                        else
                        {
                            _statusLabel.Text = M("アイコンフォントのダウンロードに失敗しました");
                        }
                    });
                };
                row.Add(dlBtn);
            }

            _iconContainer.Add(row);
        }

        void RefreshFontList()
        {
            _fontListContainer.Clear();
            var installed = MD3FontManager.GetInstalledFonts();
            var activePrefix = MD3FontManager.ActiveFontPrefix;
            var categories = MD3FontManager.GetCategories();

            foreach (var category in categories)
            {
                var catLabel = new MD3Text(M(category), MD3TextStyle.TitleSmall);
                catLabel.style.marginTop = MD3Spacing.XS;
                _fontListContainer.Add(catLabel);

                var entries = MD3FontManager.GetFontsByCategory(category);
                foreach (var entry in entries)
                {
                    var isInstalled = installed.Exists(f => f.FontPrefix == entry.FontPrefix);
                    var isActive = entry.FontPrefix == activePrefix;

                    var row = new MD3Row(gap: MD3Spacing.S);
                    row.style.alignItems = Align.Center;
                    row.style.paddingLeft = MD3Spacing.M;

                    var nameLabel = new MD3Text(entry.DisplayName, MD3TextStyle.BodySmall);
                    nameLabel.style.minWidth = 160;
                    row.Add(nameLabel);

                    row.Add(new MD3Spacer());

                    if (isInstalled)
                    {
                        if (isActive)
                        {
                            row.Add(new MD3Chip(M("使用中"), selected: true));
                        }
                        else
                        {
                            var useBtn = new MD3Button(M("使用する"), MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
                            var e = entry;
                            useBtn.clicked += () =>
                            {
                                MD3FontManager.ActiveFontPrefix = e.FontPrefix;
                                _statusLabel.Text = string.Format(M("{0} をプライマリにしました"), e.DisplayName);
                                ReapplyTheme();
                                RefreshFontList();
                            };
                            row.Add(useBtn);
                        }

                        var removeBtn = new MD3Button(M("削除"), MD3ButtonStyle.Text, size: MD3ButtonSize.Small);
                        var entry2 = entry;
                        removeBtn.clicked += () =>
                        {
                            MD3FontManager.RemoveFont(entry2);
                            _statusLabel.Text = string.Format(M("{0} を削除しました"), entry2.DisplayName);
                            ReapplyTheme();
                            RefreshFontList();
                        };
                        row.Add(removeBtn);
                    }
                    else
                    {
                        var dlBtn = new MD3Button(M("ダウンロード"), MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
                        var e = entry;
                        dlBtn.clicked += () => StartDownload(e);
                        row.Add(dlBtn);
                    }

                    _fontListContainer.Add(row);
                }
            }
        }

        void RefreshEmojiSection()
        {
            _emojiContainer.Clear();
            var row = new MD3Row(gap: MD3Spacing.S);
            row.style.alignItems = Align.Center;

            row.Add(new MD3Text("Noto Emoji", MD3TextStyle.Body));
            row.Add(new MD3Spacer());

            if (MD3FontManager.IsEmojiFontInstalled)
            {
                var toggle = new MD3Switch(MD3FontManager.EmojiEnabled);
                toggle.changed += v =>
                {
                    MD3FontManager.EmojiEnabled = v;
                    _statusLabel.Text = v ? M("絵文字フォントを有効にしました") : M("絵文字フォントを無効にしました");
                    ReapplyTheme();
                };
                row.Add(toggle);

                var removeBtn = new MD3Button(M("削除"), MD3ButtonStyle.Text, size: MD3ButtonSize.Small);
                removeBtn.clicked += () =>
                {
                    MD3FontManager.RemoveEmojiFont();
                    _statusLabel.Text = M("絵文字フォントを削除しました");
                    ReapplyTheme();
                    RefreshEmojiSection();
                };
                row.Add(removeBtn);
            }
            else
            {
                var dlBtn = new MD3Button(M("ダウンロード"), MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
                dlBtn.clicked += () =>
                {
                    if (MD3FontManager.IsDownloading) return;
                    _statusLabel.Text = string.Format(M("{0} をダウンロード中..."), "Noto Emoji");
                    _statusLabel.schedule.Execute(() =>
                    {
                        if (MD3FontManager.IsDownloading)
                        {
                            int pct = Mathf.RoundToInt(MD3FontManager.DownloadProgress * 100);
                            _statusLabel.Text = $"{string.Format(M("{0} をダウンロード中..."), "Noto Emoji")} {pct}%";
                        }
                    }).Every(200).Until(() => !MD3FontManager.IsDownloading);

                    MD3FontManager.DownloadEmojiFont(success =>
                    {
                        if (success)
                        {
                            MD3FontManager.RefreshAllWindows();
                            Rebuild();
                        }
                        else
                        {
                            _statusLabel.Text = string.Format(M("{0} のダウンロードに失敗しました"), "Noto Emoji");
                        }
                    });
                };
                row.Add(dlBtn);
            }

            _emojiContainer.Add(row);
        }

        void StartDownload(MD3FontManager.FontEntry entry)
        {
            if (MD3FontManager.IsDownloading) return;

            _statusLabel.Text = string.Format(M("{0} をダウンロード中..."), entry.DisplayName);

            _statusLabel.schedule.Execute(() =>
            {
                if (MD3FontManager.IsDownloading)
                {
                    int pct = Mathf.RoundToInt(MD3FontManager.DownloadProgress * 100);
                    _statusLabel.Text = $"{string.Format(M("{0} をダウンロード中..."), entry.DisplayName)} {pct}%";
                }
            }).Every(200).Until(() => !MD3FontManager.IsDownloading);

            MD3FontManager.DownloadFont(entry, success =>
            {
                if (success)
                {
                    MD3FontManager.RefreshAllWindows();
                    Rebuild();
                }
                else
                {
                    _statusLabel.Text = string.Format(M("{0} のダウンロードに失敗しました"), entry.DisplayName);
                }
            });
        }
    }
}
