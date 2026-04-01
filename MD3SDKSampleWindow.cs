using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SDKSampleWindow : EditorWindow
    {
        MD3Theme _theme;
        bool _isDark;
        readonly List<Action<MD3Theme>> _themeCallbacks = new List<Action<MD3Theme>>();

        VisualElement _sidebar;
        ScrollView _contentScroll;
        MD3Fab _floatingFab;
        int _currentPage = -1;
        int _layoutCallbackCount;
        readonly List<VisualElement> _sidebarItems = new List<VisualElement>();

        static readonly string[] PageNames =
        {
            "Theme", "Typography", "Layout", "Buttons", "Inputs", "Selection",
            "Display", "Shapes", "Data", "Navigation", "Feedback", "Animation", "Progress"
        };
        static readonly string[] PageIcons =
        {
            MD3Icon.Palette, MD3Icon.FormatSize, MD3Icon.GridView, MD3Icon.SmartButton,
            MD3Icon.Tune, MD3Icon.Checklist,
            MD3Icon.Widgets, MD3Icon.Star, MD3Icon.TableChart, MD3Icon.NearMe, MD3Icon.Feedback, MD3Icon.Animation, MD3Icon.ProgressActivity
        };

        [MenuItem("Window/紫陽花広場/MD3 SDK Sample")]
        public static void ShowWindow()
        {
            var w = GetWindow<MD3SDKSampleWindow>("MD3 SDK");
            w.minSize = new Vector2(640, 400);
        }

        void CreateGUI()
        {
            // Clear old tree to prevent accumulation on domain reload
            rootVisualElement.Clear();
            _themeCallbacks.Clear();
            _sidebarItems.Clear();
            _currentPage = -1;

            _isDark = EditorGUIUtility.isProSkin;
            _theme = _isDark ? MD3Theme.Dark() : MD3Theme.Light();

            var themeSheet = MD3Theme.LoadThemeStyleSheet();
            var compSheet = MD3Theme.LoadComponentsStyleSheet();
            if (themeSheet != null && !rootVisualElement.styleSheets.Contains(themeSheet))
                rootVisualElement.styleSheets.Add(themeSheet);
            if (compSheet != null && !rootVisualElement.styleSheets.Contains(compSheet))
                rootVisualElement.styleSheets.Add(compSheet);

            _theme.ApplyTo(rootVisualElement);
            BuildLayout();
            ShowPage(0);
        }

        void BuildLayout()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Row;

            // ── Sidebar ──
            _sidebar = new VisualElement();
            _sidebar.style.width = 160;
            _sidebar.style.flexShrink = 0;
            _sidebar.style.borderRightWidth = 1;
            _sidebar.style.borderRightColor = _theme.OutlineVariant;
            _sidebar.style.paddingTop = 8;
            _sidebar.style.paddingBottom = 8;
            _themeCallbacks.Add(t => _sidebar.style.borderRightColor = t.OutlineVariant);
            rootVisualElement.Add(_sidebar);

            // Sidebar title
            var sideTitle = new Label("MD3 SDK");
            sideTitle.style.fontSize = 14;
            sideTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sideTitle.style.color = _theme.Primary;
            sideTitle.style.paddingLeft = 16;
            sideTitle.style.paddingRight = 16;
            sideTitle.style.paddingBottom = 12;
            sideTitle.style.paddingTop = 4;
            _themeCallbacks.Add(t => sideTitle.style.color = t.Primary);
            _sidebar.Add(sideTitle);

            // Sidebar items
            for (int i = 0; i < PageNames.Length; i++)
            {
                var idx = i;
                var item = new VisualElement();
                item.style.height = 36;
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;
                item.style.paddingLeft = 16;
                item.style.paddingRight = 12;
                item.style.marginLeft = 8;
                item.style.marginRight = 8;
                item.style.borderTopLeftRadius = 18;
                item.style.borderTopRightRadius = 18;
                item.style.borderBottomLeftRadius = 18;
                item.style.borderBottomRightRadius = 18;
                item.style.cursor = StyleKeyword.None;

                var icon = MD3Icon.Create(PageIcons[i], 20f, _theme.OnSurfaceVariant);
                icon.style.width = 24;
                item.Add(icon);

                var label = new Label(PageNames[i]);
                label.style.fontSize = 13;
                label.style.color = _theme.OnSurface;
                label.style.marginLeft = 8;
                label.pickingMode = PickingMode.Ignore;
                item.Add(label);

                item.RegisterCallback<ClickEvent>(_ => ShowPage(idx));
                item.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (_currentPage != idx)
                        item.style.backgroundColor = _theme.HoverOverlay(Color.clear, _theme.OnSurface);
                });
                item.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (_currentPage != idx)
                        item.style.backgroundColor = Color.clear;
                });

                _themeCallbacks.Add(t =>
                {
                    icon.style.color = _currentPage == idx ? t.OnPrimaryContainer : t.OnSurfaceVariant;
                    label.style.color = _currentPage == idx ? t.OnPrimaryContainer : t.OnSurface;
                    item.style.backgroundColor = _currentPage == idx ? t.SecondaryContainer : Color.clear;
                });

                _sidebarItems.Add(item);
                _sidebar.Add(item);
            }

            // ── Content area ──
            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.style.flexGrow = 1;
            _contentScroll.style.paddingTop = 16;
            _contentScroll.style.paddingBottom = 16;
            _contentScroll.style.paddingLeft = 24;
            _contentScroll.style.paddingRight = 24;
            rootVisualElement.Add(_contentScroll);

            _layoutCallbackCount = _themeCallbacks.Count;
        }

        void ShowPage(int index)
        {
            if (index == _currentPage) return;

            // Remove floating FAB if present
            if (_floatingFab != null)
            {
                _floatingFab.RemoveFromHierarchy();
                _floatingFab = null;
            }

            _currentPage = index;

            // Update sidebar selection
            for (int i = 0; i < _sidebarItems.Count; i++)
            {
                bool sel = i == index;
                _sidebarItems[i].style.backgroundColor = sel ? _theme.SecondaryContainer : Color.clear;
                // icon + label children
                if (_sidebarItems[i].childCount >= 2)
                {
                    _sidebarItems[i][0].style.color = sel ? _theme.OnPrimaryContainer : _theme.OnSurfaceVariant;
                    _sidebarItems[i][1].style.color = sel ? _theme.OnPrimaryContainer : _theme.OnSurface;
                }
            }

            // Clear and rebuild content
            if (_themeCallbacks.Count > _layoutCallbackCount)
                _themeCallbacks.RemoveRange(_layoutCallbackCount, _themeCallbacks.Count - _layoutCallbackCount);
            _contentScroll.Clear();
            _contentScroll.scrollOffset = Vector2.zero;

            switch (index)
            {
                case 0: Build_Theme(_contentScroll); break;
                case 1: Build_Typography(_contentScroll); break;
                case 2: Build_Layout(_contentScroll); break;
                case 3: Build_Buttons(_contentScroll); break;
                case 4: Build_Inputs(_contentScroll); break;
                case 5: Build_Selection(_contentScroll); break;
                case 6: Build_Display(_contentScroll); break;
                case 7: Build_Shapes(_contentScroll); break;
                case 8: Build_Data(_contentScroll); break;
                case 9: Build_Navigation(_contentScroll); break;
                case 10: Build_Feedback(_contentScroll); break;
                case 11: Build_Animation(_contentScroll); break;
                case 12: Build_Progress(_contentScroll); break;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Page Builders
        // ═══════════════════════════════════════════════════════

        void Build_Theme(VisualElement c)
        {
            c.Add(new MD3Text("Theme", MD3TextStyle.HeadlineMedium));

            var themeRow = Row();
            themeRow.style.marginTop = 16;
            var themeLabel = new Label(_isDark ? "Dark Theme" : "Light Theme");
            themeLabel.style.color = _theme.OnSurface;
            themeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            themeLabel.style.flexGrow = 1;
            themeRow.Add(themeLabel);
            _themeCallbacks.Add(t =>
            {
                themeLabel.text = t.IsDark ? "Dark Theme" : "Light Theme";
                themeLabel.style.color = t.OnSurface;
            });

            var themeSwitch = new MD3Switch(_isDark);
            themeSwitch.changed += v =>
            {
                _isDark = v;
                _theme = _isDark ? MD3Theme.Dark() : MD3Theme.Light();
                ApplyThemeColors();
            };
            themeRow.Add(themeSwitch);
            c.Add(themeRow);

            // Color palette preview
            c.Add(Spacer(24));
            c.Add(new MD3Text("Color Palette", MD3TextStyle.TitleMedium));
            c.Add(Spacer(8));
            var colors = new (string name, Func<MD3Theme, Color> get)[]
            {
                ("Primary", t => t.Primary), ("OnPrimary", t => t.OnPrimary),
                ("PrimaryContainer", t => t.PrimaryContainer), ("OnPrimaryContainer", t => t.OnPrimaryContainer),
                ("Secondary", t => t.Secondary), ("OnSecondary", t => t.OnSecondary),
                ("SecondaryContainer", t => t.SecondaryContainer), ("OnSecondaryContainer", t => t.OnSecondaryContainer),
                ("Tertiary", t => t.Tertiary), ("OnTertiary", t => t.OnTertiary),
                ("Surface", t => t.Surface), ("OnSurface", t => t.OnSurface),
                ("SurfaceVariant", t => t.SurfaceVariant), ("OnSurfaceVariant", t => t.OnSurfaceVariant),
                ("Error", t => t.Error), ("OnError", t => t.OnError),
                ("Outline", t => t.Outline), ("OutlineVariant", t => t.OutlineVariant),
                ("SurfContainerLowest", t => t.SurfaceContainerLowest),
                ("SurfContainerLow", t => t.SurfaceContainerLow),
                ("SurfContainer", t => t.SurfaceContainer),
                ("SurfContainerHigh", t => t.SurfaceContainerHigh),
                ("SurfContainerHighest", t => t.SurfaceContainerHighest),
            };
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var (name, get) in colors)
            {
                var swatch = new VisualElement();
                swatch.style.width = 120;
                swatch.style.height = 40;
                swatch.style.borderTopLeftRadius = 8;
                swatch.style.borderTopRightRadius = 8;
                swatch.style.borderBottomLeftRadius = 8;
                swatch.style.borderBottomRightRadius = 8;
                swatch.style.marginRight = 4;
                swatch.style.marginBottom = 4;
                swatch.style.justifyContent = Justify.Center;
                swatch.style.alignItems = Align.Center;
                swatch.style.backgroundColor = get(_theme);
                var sl = new Label(name);
                sl.style.fontSize = 9;
                sl.style.color = name.StartsWith("On") ? get(_theme) : _theme.OnSurface;
                sl.pickingMode = PickingMode.Ignore;
                swatch.Add(sl);
                grid.Add(swatch);
                _themeCallbacks.Add(t =>
                {
                    swatch.style.backgroundColor = get(t);
                    sl.style.color = name.StartsWith("On") ? get(t) : t.OnSurface;
                });
            }
            c.Add(grid);

            // ── Seed Color Palette ──
            c.Add(Spacer(24));
            c.Add(new MD3Text("Seed Color Palette", MD3TextStyle.TitleMedium));
            c.Add(new MD3Text("Generate a complete theme from a single color.", MD3TextStyle.BodySmall));
            c.Add(Spacer(8));

            var seedRow = Row();
            var seedColors = new (string name, Color color)[]
            {
                ("Purple", new Color(0.40f, 0.31f, 0.64f)),
                ("Blue",   new Color(0.15f, 0.40f, 0.85f)),
                ("Green",  new Color(0.20f, 0.60f, 0.30f)),
                ("Red",    new Color(0.80f, 0.20f, 0.20f)),
                ("Orange", new Color(0.90f, 0.55f, 0.15f)),
                ("Teal",   new Color(0.00f, 0.55f, 0.55f)),
                ("Pink",   new Color(0.85f, 0.30f, 0.55f)),
            };

            var seedPreviewGrid = new VisualElement();
            seedPreviewGrid.style.flexDirection = FlexDirection.Row;
            seedPreviewGrid.style.flexWrap = Wrap.Wrap;
            seedPreviewGrid.style.marginTop = 8;

            Color currentSeed = seedColors[0].color;

            System.Action<Color> updatePreview = (sc) =>
            {
                currentSeed = sc;
                var seedTheme = MD3Theme.FromSeedColor(sc, _isDark);
                seedPreviewGrid.Clear();
                var previewColors = new (string n, Color c)[]
                {
                    ("Primary", seedTheme.Primary), ("OnPrimary", seedTheme.OnPrimary),
                    ("PriContainer", seedTheme.PrimaryContainer), ("Secondary", seedTheme.Secondary),
                    ("Tertiary", seedTheme.Tertiary), ("Surface", seedTheme.Surface),
                    ("SurfVariant", seedTheme.SurfaceVariant), ("Outline", seedTheme.Outline),
                    ("Error", seedTheme.Error),
                };
                foreach (var (pn, pc) in previewColors)
                {
                    var sw = new VisualElement();
                    sw.style.width = 80; sw.style.height = 36;
                    sw.Radius(6);
                    sw.style.marginRight = 3; sw.style.marginBottom = 3;
                    sw.style.justifyContent = Justify.Center;
                    sw.style.alignItems = Align.Center;
                    sw.style.backgroundColor = pc;
                    var lb = new Label(pn);
                    lb.style.fontSize = 8;
                    lb.style.color = seedTheme.OnSurface;
                    lb.pickingMode = PickingMode.Ignore;
                    sw.Add(lb);
                    seedPreviewGrid.Add(sw);
                }
            };

            foreach (var (sName, sColor) in seedColors)
            {
                var btn = new MD3Button(sName, MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
                btn.style.marginRight = 6;
                btn.style.marginBottom = 6;
                var sc = sColor;
                btn.clicked += () => updatePreview(sc);
                seedRow.Add(btn);
            }
            c.Add(seedRow);

            var applyRow = Row();
            var applyBtn = new MD3Button("Apply Seed Theme", MD3ButtonStyle.Filled, size: MD3ButtonSize.Small);
            var resetBtn = new MD3Button("Reset to Default", MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
            resetBtn.style.marginLeft = 8;
            applyBtn.clicked += () =>
            {
                _theme = MD3Theme.FromSeedColor(currentSeed, _isDark);
                _theme.ApplyTo(rootVisualElement);
                ApplyThemeColors();
            };
            resetBtn.clicked += () =>
            {
                _theme = _isDark ? MD3Theme.Dark() : MD3Theme.Light();
                _theme.ApplyTo(rootVisualElement);
                ApplyThemeColors();
            };
            applyRow.Add(applyBtn);
            applyRow.Add(resetBtn);
            c.Add(applyRow);
            c.Add(seedPreviewGrid);
        }

        void Build_Typography(VisualElement c)
        {
            c.Add(new MD3Text("Typography", MD3TextStyle.HeadlineMedium));
            c.Add(Spacer(12));

            var styles = new (string text, MD3TextStyle style)[]
            {
                ("Display Large", MD3TextStyle.DisplayLarge),
                ("Display Medium", MD3TextStyle.DisplayMedium),
                ("Display Small", MD3TextStyle.DisplaySmall),
                ("Headline Large", MD3TextStyle.HeadlineLarge),
                ("Headline Medium", MD3TextStyle.HeadlineMedium),
                ("Headline Small", MD3TextStyle.HeadlineSmall),
                ("Title Large", MD3TextStyle.TitleLarge),
                ("Title Medium", MD3TextStyle.TitleMedium),
                ("Title Small", MD3TextStyle.TitleSmall),
                ("Body", MD3TextStyle.Body),
                ("Body Small", MD3TextStyle.BodySmall),
                ("Label Large", MD3TextStyle.LabelLarge),
                ("Label Caption", MD3TextStyle.LabelCaption),
                ("Label Annotation", MD3TextStyle.LabelAnnotation),
            };
            foreach (var (text, style) in styles)
            {
                var row = Row();
                var nameLabel = new Label(style.ToString());
                nameLabel.style.width = 140;
                nameLabel.style.fontSize = 11;
                nameLabel.style.color = _theme.OnSurfaceVariant;
                nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                nameLabel.style.flexShrink = 0;
                _themeCallbacks.Add(t => nameLabel.style.color = t.OnSurfaceVariant);
                row.Add(nameLabel);
                row.Add(new MD3Text(text, style));
                c.Add(row);
            }

            c.Add(Spacer(16));
            AddSection(c, "Section Label");
            c.Add(new MD3SectionLabel("Section Heading Example"));
            c.Add(new MD3SectionLabel("Another Section"));
        }

        void Build_Layout(VisualElement c)
        {
            c.Add(new MD3Text("Layout", MD3TextStyle.HeadlineMedium));

            // ── MD3SplitPane ──
            AddSection(c, "MD3SplitPane");
            c.Add(new MD3Text("Drag the handle to resize panels.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var hSplit = new MD3SplitPane(MD3SplitDirection.Horizontal, 0.4f, 80f, 80f);
            hSplit.style.height = 150;
            hSplit.Radius(MD3Radius.M).ClipContent();
            hSplit.style.borderTopWidth = 1; hSplit.style.borderBottomWidth = 1;
            hSplit.style.borderLeftWidth = 1; hSplit.style.borderRightWidth = 1;
            hSplit.style.borderTopColor = _theme.OutlineVariant;
            hSplit.style.borderBottomColor = _theme.OutlineVariant;
            hSplit.style.borderLeftColor = _theme.OutlineVariant;
            hSplit.style.borderRightColor = _theme.OutlineVariant;
            _themeCallbacks.Add(t =>
            {
                hSplit.style.borderTopColor = t.OutlineVariant;
                hSplit.style.borderBottomColor = t.OutlineVariant;
                hSplit.style.borderLeftColor = t.OutlineVariant;
                hSplit.style.borderRightColor = t.OutlineVariant;
            });

            var leftPanel = new MD3Center();
            leftPanel.style.backgroundColor = _theme.SurfaceContainerLow;
            leftPanel.Add(new MD3Text("Left Panel", MD3TextStyle.Body));
            _themeCallbacks.Add(t => leftPanel.style.backgroundColor = t.SurfaceContainerLow);
            hSplit.First.Add(leftPanel);

            var rightPanel = new MD3Center();
            rightPanel.style.backgroundColor = _theme.SurfaceContainer;
            rightPanel.Add(new MD3Text("Right Panel", MD3TextStyle.Body));
            _themeCallbacks.Add(t => rightPanel.style.backgroundColor = t.SurfaceContainer);
            hSplit.Second.Add(rightPanel);

            c.Add(hSplit);
            c.Add(new MD3Spacer(MD3Spacing.S));

            var vSplit = new MD3SplitPane(MD3SplitDirection.Vertical, 0.5f, 40f, 40f);
            vSplit.style.height = 150;
            vSplit.Radius(MD3Radius.M).ClipContent();
            vSplit.style.borderTopWidth = 1; vSplit.style.borderBottomWidth = 1;
            vSplit.style.borderLeftWidth = 1; vSplit.style.borderRightWidth = 1;
            vSplit.style.borderTopColor = _theme.OutlineVariant;
            vSplit.style.borderBottomColor = _theme.OutlineVariant;
            vSplit.style.borderLeftColor = _theme.OutlineVariant;
            vSplit.style.borderRightColor = _theme.OutlineVariant;
            _themeCallbacks.Add(t =>
            {
                vSplit.style.borderTopColor = t.OutlineVariant;
                vSplit.style.borderBottomColor = t.OutlineVariant;
                vSplit.style.borderLeftColor = t.OutlineVariant;
                vSplit.style.borderRightColor = t.OutlineVariant;
            });

            var topPanel = new MD3Center();
            topPanel.style.backgroundColor = _theme.SurfaceContainerLow;
            topPanel.Add(new MD3Text("Top Panel", MD3TextStyle.Body));
            _themeCallbacks.Add(t => topPanel.style.backgroundColor = t.SurfaceContainerLow);
            vSplit.First.Add(topPanel);

            var bottomPanel = new MD3Center();
            bottomPanel.style.backgroundColor = _theme.SurfaceContainer;
            bottomPanel.Add(new MD3Text("Bottom Panel", MD3TextStyle.Body));
            _themeCallbacks.Add(t => bottomPanel.style.backgroundColor = t.SurfaceContainer);
            vSplit.Second.Add(bottomPanel);

            c.Add(vSplit);

            // ── Spacing Tokens ──
            AddSection(c, "Spacing Tokens");
            c.Add(new MD3Text("4px grid system — use MD3Spacing constants instead of raw px values.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var spacingTokens = new (string name, float value)[]
            {
                ("XXS", MD3Spacing.XXS), ("XS", MD3Spacing.XS), ("S", MD3Spacing.S),
                ("M", MD3Spacing.M), ("L", MD3Spacing.L), ("XL", MD3Spacing.XL),
                ("XXL", MD3Spacing.XXL), ("XXXL", MD3Spacing.XXXL),
            };
            foreach (var (name, value) in spacingTokens)
            {
                var tokenRow = new MD3Row(gap: MD3Spacing.S);
                tokenRow.style.marginBottom = MD3Spacing.XS;

                var label = new Label($"MD3Spacing.{name}");
                label.style.width = 140;
                label.style.fontSize = 12;
                label.style.color = _theme.OnSurfaceVariant;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                _themeCallbacks.Add(t => label.style.color = t.OnSurfaceVariant);
                tokenRow.Add(label);

                var bar = new VisualElement();
                bar.style.width = value;
                bar.style.height = 20;
                bar.style.backgroundColor = _theme.Primary;
                bar.Radius(MD3Radius.XS);
                _themeCallbacks.Add(t => bar.style.backgroundColor = t.Primary);
                tokenRow.Add(bar);

                var px = new Label($"{value}px");
                px.style.fontSize = 11;
                px.style.color = _theme.OnSurfaceVariant;
                px.style.unityTextAlign = TextAnchor.MiddleLeft;
                _themeCallbacks.Add(t => px.style.color = t.OnSurfaceVariant);
                tokenRow.Add(px);

                c.Add(tokenRow);
            }

            // ── Radius Tokens ──
            AddSection(c, "Radius Tokens");
            var radiusTokens = new (string name, float value)[]
            {
                ("None", MD3Radius.None), ("XS", MD3Radius.XS), ("S", MD3Radius.S),
                ("M", MD3Radius.M), ("L", MD3Radius.L), ("XL", MD3Radius.XL),
                ("XXL", MD3Radius.XXL), ("Full", MD3Radius.Full),
            };
            var radiusRow = new MD3Row(gap: MD3Spacing.S, wrap: true);
            foreach (var (name, value) in radiusTokens)
            {
                var cell = new MD3Column(gap: MD3Spacing.XS, alignItems: Align.Center);
                cell.style.width = 64;

                var box = new VisualElement();
                box.Size(48, 48);
                box.Radius(value);
                box.style.backgroundColor = _theme.PrimaryContainer;
                _themeCallbacks.Add(t => box.style.backgroundColor = t.PrimaryContainer);
                cell.Add(box);

                var label = new Label(name);
                label.style.fontSize = 10;
                label.style.color = _theme.OnSurfaceVariant;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                _themeCallbacks.Add(t => label.style.color = t.OnSurfaceVariant);
                cell.Add(label);

                radiusRow.Add(cell);
            }
            c.Add(radiusRow);

            // ── MD3Row ──
            AddSection(c, "MD3Row");
            c.Add(new MD3Text("Horizontal layout with gap and alignment.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var rowDemo = new MD3Row(gap: MD3Spacing.S);
            rowDemo.Add(new MD3Button("First", MD3ButtonStyle.Filled));
            rowDemo.Add(new MD3Button("Second", MD3ButtonStyle.Tonal));
            rowDemo.Add(new MD3Spacer());
            rowDemo.Add(new MD3Button("End", MD3ButtonStyle.Outlined));
            c.Add(rowDemo);

            // ── MD3Column ──
            AddSection(c, "MD3Column");
            c.Add(new MD3Text("Vertical layout with gap.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var colDemo = new MD3Column(gap: MD3Spacing.M)
                .Padding(MD3Spacing.L)
                .Radius(MD3Radius.M);
            colDemo.style.backgroundColor = _theme.SurfaceContainerLow;
            colDemo.style.borderTopWidth = 1;
            colDemo.style.borderBottomWidth = 1;
            colDemo.style.borderLeftWidth = 1;
            colDemo.style.borderRightWidth = 1;
            colDemo.style.borderTopColor = _theme.OutlineVariant;
            colDemo.style.borderBottomColor = _theme.OutlineVariant;
            colDemo.style.borderLeftColor = _theme.OutlineVariant;
            colDemo.style.borderRightColor = _theme.OutlineVariant;
            _themeCallbacks.Add(t =>
            {
                colDemo.style.backgroundColor = t.SurfaceContainerLow;
                colDemo.style.borderTopColor = t.OutlineVariant;
                colDemo.style.borderBottomColor = t.OutlineVariant;
                colDemo.style.borderLeftColor = t.OutlineVariant;
                colDemo.style.borderRightColor = t.OutlineVariant;
            });
            colDemo.Add(new MD3Text("Column Item 1", MD3TextStyle.TitleMedium));
            colDemo.Add(new MD3Divider());
            colDemo.Add(new MD3Text("Column Item 2", MD3TextStyle.Body));
            colDemo.Add(new MD3Text("Column Item 3", MD3TextStyle.BodySmall));
            c.Add(colDemo);

            // ── MD3Grid ──
            AddSection(c, "MD3Grid");
            c.Add(new MD3Text("Fixed-column grid layout (flex-wrap based).", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var gridDemo = new MD3Grid(columns: 3, gap: MD3Spacing.S);
            for (int i = 0; i < 6; i++)
            {
                var card = new MD3Card($"Card {i + 1}", "Grid item", MD3CardStyle.Outlined);
                gridDemo.Add(card);
            }
            c.Add(gridDemo);

            var gridDemo2 = new MD3Grid(columns: 2, gap: MD3Spacing.M);
            gridDemo2.style.marginTop = MD3Spacing.M;
            for (int i = 0; i < 4; i++)
            {
                var card = new MD3Card($"Wide {i + 1}", "2-column grid", MD3CardStyle.Filled);
                gridDemo2.Add(card);
            }
            c.Add(gridDemo2);

            c.Add(new MD3Text("FixedCellWidth — cells stay at exact width, remaining space unused.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.XS));
            var gridFixed = new MD3Grid(MD3GridMode.FixedCellWidth, cellWidth: 120f, gap: MD3Spacing.S);
            gridFixed.style.marginTop = MD3Spacing.M;
            for (int i = 0; i < 8; i++)
            {
                var card = new MD3Card($"Item {i + 1}", "120px fixed", MD3CardStyle.Outlined);
                gridFixed.Add(card);
            }
            c.Add(gridFixed);

            c.Add(new MD3Text("DynamicCellWidth — min 120px, cells stretch to fill container.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.XS));
            var gridDynamic = new MD3Grid(MD3GridMode.DynamicCellWidth, cellWidth: 120f, gap: MD3Spacing.S);
            gridDynamic.style.marginTop = MD3Spacing.M;
            for (int i = 0; i < 8; i++)
            {
                var card = new MD3Card($"Item {i + 1}", "120px+ dynamic", MD3CardStyle.Filled);
                gridDynamic.Add(card);
            }
            c.Add(gridDynamic);

            // ── Extension Methods ──
            AddSection(c, "Extension Methods");
            c.Add(new MD3Text("Chainable .Padding(), .Margin(), .Size(), .Grow(), .Radius()", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var extRow = new MD3Row(gap: MD3Spacing.M);

            var box1 = new VisualElement()
                .Size(80, 80)
                .Padding(MD3Spacing.S)
                .Radius(MD3Radius.S);
            box1.style.backgroundColor = _theme.PrimaryContainer;
            var box1Label = new Label("S pad\nXS radius");
            box1Label.style.fontSize = 10;
            box1Label.style.color = _theme.OnPrimaryContainer;
            box1Label.style.whiteSpace = WhiteSpace.Normal;
            _themeCallbacks.Add(t => { box1.style.backgroundColor = t.PrimaryContainer; box1Label.style.color = t.OnPrimaryContainer; });
            box1.Add(box1Label);
            extRow.Add(box1);

            var box2 = new VisualElement()
                .Size(80, 80)
                .Padding(MD3Spacing.M)
                .Radius(MD3Radius.L);
            box2.style.backgroundColor = _theme.SecondaryContainer;
            var box2Label = new Label("M pad\nL radius");
            box2Label.style.fontSize = 10;
            box2Label.style.color = _theme.OnSecondaryContainer;
            box2Label.style.whiteSpace = WhiteSpace.Normal;
            _themeCallbacks.Add(t => { box2.style.backgroundColor = t.SecondaryContainer; box2Label.style.color = t.OnSecondaryContainer; });
            box2.Add(box2Label);
            extRow.Add(box2);

            var box3 = new VisualElement()
                .Size(80, 80)
                .Padding(MD3Spacing.L)
                .Radius(MD3Radius.Full);
            box3.style.backgroundColor = _theme.TertiaryContainer;
            var box3Label = new Label("L pad\nFull");
            box3Label.style.fontSize = 10;
            box3Label.style.color = _theme.OnTertiaryContainer;
            box3Label.style.whiteSpace = WhiteSpace.Normal;
            _themeCallbacks.Add(t => { box3.style.backgroundColor = t.TertiaryContainer; box3Label.style.color = t.OnTertiaryContainer; });
            box3.Add(box3Label);
            extRow.Add(box3);

            c.Add(extRow);

            // ── MD3Center ──
            AddSection(c, "MD3Center");
            c.Add(new MD3Text("Centers children both horizontally and vertically.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var centerDemo = new MD3Center();
            centerDemo.style.height = 120;
            centerDemo.style.backgroundColor = _theme.SurfaceContainerLow;
            centerDemo.Radius(MD3Radius.M);
            _themeCallbacks.Add(t => centerDemo.style.backgroundColor = t.SurfaceContainerLow);
            var centerContent = new MD3Column(gap: MD3Spacing.XS, alignItems: Align.Center);
            centerContent.Add(new MD3Text("Centered!", MD3TextStyle.TitleMedium));
            centerContent.Add(new MD3Text("Both axes", MD3TextStyle.BodySmall));
            centerDemo.Add(centerContent);
            c.Add(centerDemo);

            // ── MD3Stack ──
            AddSection(c, "MD3Stack");
            c.Add(new MD3Text("Overlays children (ZStack). First child sizes, rest are overlaid.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var stackDemo = new MD3Stack();
            // Base layer
            var stackBase = new VisualElement().Size(200, 120).Radius(MD3Radius.M);
            stackBase.style.backgroundColor = _theme.PrimaryContainer;
            _themeCallbacks.Add(t => stackBase.style.backgroundColor = t.PrimaryContainer);
            stackDemo.Add(stackBase);
            // Bottom-right overlay
            var stackBadge = new MD3Button("Overlay", MD3ButtonStyle.Filled, size: MD3ButtonSize.Small);
            stackDemo.AddOverlay(stackBadge, MD3StackAlignment.BottomRight);
            stackBadge.Margin(MD3Spacing.S);
            // Center overlay
            var stackLabel = new MD3Text("Base Layer", MD3TextStyle.TitleMedium);
            stackDemo.AddOverlay(stackLabel, MD3StackAlignment.Center);
            c.Add(stackDemo);

            // ── MD3Constrained ──
            AddSection(c, "MD3Constrained");
            c.Add(new MD3Text("Max-width container, centered horizontally.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var constrainedOuter = new VisualElement();
            constrainedOuter.style.backgroundColor = _theme.SurfaceContainerLow;
            constrainedOuter.Radius(MD3Radius.M).Padding(MD3Spacing.S);
            _themeCallbacks.Add(t => constrainedOuter.style.backgroundColor = t.SurfaceContainerLow);
            var constrained = new MD3Constrained(320f).Padding(MD3Spacing.L).Radius(MD3Radius.S);
            constrained.style.backgroundColor = _theme.SurfaceContainer;
            _themeCallbacks.Add(t => constrained.style.backgroundColor = t.SurfaceContainer);
            constrained.Add(new MD3Text("Max 320px wide, always centered.", MD3TextStyle.Body));
            constrainedOuter.Add(constrained);
            c.Add(constrainedOuter);

            // ── Composed Example ──
            AddSection(c, "Composed Example");
            c.Add(new MD3Text("Real-world layout using only tokens — no raw px.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var composed = new MD3Column(gap: MD3Spacing.M)
                .Padding(MD3Spacing.L)
                .Radius(MD3Radius.M);
            composed.style.backgroundColor = _theme.SurfaceContainer;
            _themeCallbacks.Add(t => composed.style.backgroundColor = t.SurfaceContainer);

            var header = new MD3Row(gap: MD3Spacing.S);
            header.Add(new MD3Avatar("A", 40f));
            var headerText = new MD3Column(gap: MD3Spacing.XXS);
            headerText.Add(new MD3Text("Alice Johnson", MD3TextStyle.TitleMedium));
            headerText.Add(new MD3Text("Product Designer", MD3TextStyle.BodySmall));
            header.Add(headerText);
            header.Add(new MD3Spacer());
            header.Add(new MD3IconButton(MD3Icon.MoreVert, MD3IconButtonStyle.Standard));
            composed.Add(header);

            composed.Add(new MD3Divider());

            composed.Add(new MD3Text("Working on the new dashboard redesign. The layout system makes it easy to build consistent spacing without pixel values.", MD3TextStyle.Body));

            var actions = new MD3Row(gap: MD3Spacing.S);
            actions.Add(new MD3Spacer());
            actions.Add(new MD3Button("Message", MD3ButtonStyle.Outlined));
            actions.Add(new MD3Button("Follow", MD3ButtonStyle.Filled));
            composed.Add(actions);

            c.Add(composed);
        }

        void Build_Buttons(VisualElement c)
        {
            c.Add(new MD3Text("Buttons", MD3TextStyle.HeadlineMedium));

            // Buttons
            AddSection(c, "Button Styles");
            var btnRow = Row();
            btnRow.Add(CreateButton("Filled", MD3ButtonStyle.Filled));
            btnRow.Add(CreateButton("Tonal", MD3ButtonStyle.Tonal));
            btnRow.Add(CreateButton("Outlined", MD3ButtonStyle.Outlined));
            btnRow.Add(CreateButton("Text", MD3ButtonStyle.Text));
            c.Add(btnRow);

            var disabledRow = Row();
            var disabledBtn = new MD3Button("Disabled", MD3ButtonStyle.Filled);
            disabledBtn.IsDisabled = true;
            disabledRow.Add(disabledBtn);
            c.Add(disabledRow);

            // Button Sizes
            AddSection(c, "Button Sizes");
            var sizeRow = Row();
            sizeRow.Add(new MD3Button("Small", MD3ButtonStyle.Filled, size: MD3ButtonSize.Small));
            sizeRow.style.marginBottom = 4;
            var medBtn = new MD3Button("Medium", MD3ButtonStyle.Filled); medBtn.style.marginLeft = 8; sizeRow.Add(medBtn);
            var lgBtn = new MD3Button("Large", MD3ButtonStyle.Filled, size: MD3ButtonSize.Large); lgBtn.style.marginLeft = 8; sizeRow.Add(lgBtn);
            c.Add(sizeRow);

            var sizeRow2 = Row();
            sizeRow2.Add(new MD3Button("Small", MD3ButtonStyle.Outlined, icon: MD3Icon.Add, size: MD3ButtonSize.Small));
            var medBtn2 = new MD3Button("Medium", MD3ButtonStyle.Outlined, icon: MD3Icon.Add); medBtn2.style.marginLeft = 8; sizeRow2.Add(medBtn2);
            var lgBtn2 = new MD3Button("Large", MD3ButtonStyle.Outlined, icon: MD3Icon.Add, size: MD3ButtonSize.Large); lgBtn2.style.marginLeft = 8; sizeRow2.Add(lgBtn2);
            c.Add(sizeRow2);

            // Icon Button Sizes
            AddSection(c, "Icon Button Sizes");
            var iconSizeRow = Row();
            iconSizeRow.Add(new MD3IconButton(MD3Icon.Settings, MD3IconButtonStyle.Filled, MD3IconButtonSize.Small));
            var medIcon = new MD3IconButton(MD3Icon.Settings, MD3IconButtonStyle.Filled); medIcon.style.marginLeft = 8; iconSizeRow.Add(medIcon);
            var lgIcon = new MD3IconButton(MD3Icon.Settings, MD3IconButtonStyle.Filled, MD3IconButtonSize.Large); lgIcon.style.marginLeft = 8; iconSizeRow.Add(lgIcon);
            c.Add(iconSizeRow);

            var iconSizeRow2 = Row();
            iconSizeRow2.Add(new MD3IconButton(MD3Icon.Favorite, MD3IconButtonStyle.Outlined, MD3IconButtonSize.Small));
            var medIcon2 = new MD3IconButton(MD3Icon.Favorite, MD3IconButtonStyle.Outlined); medIcon2.style.marginLeft = 8; iconSizeRow2.Add(medIcon2);
            var lgIcon2 = new MD3IconButton(MD3Icon.Favorite, MD3IconButtonStyle.Outlined, MD3IconButtonSize.Large); lgIcon2.style.marginLeft = 8; iconSizeRow2.Add(lgIcon2);
            c.Add(iconSizeRow2);

            // Buttons with icons
            AddSection(c, "Buttons with Icons");
            var iconBtnRow = Row();
            var addBtn = new MD3Button("Add", MD3ButtonStyle.Filled, icon: MD3Icon.Add);
            addBtn.style.marginRight = 8;
            addBtn.clicked += () => Debug.Log("[MD3] Icon Button: Add");
            iconBtnRow.Add(addBtn);
            var editBtn = new MD3Button("Edit", MD3ButtonStyle.Tonal, icon: MD3Icon.Edit);
            editBtn.style.marginRight = 8;
            editBtn.clicked += () => Debug.Log("[MD3] Icon Button: Edit");
            iconBtnRow.Add(editBtn);
            var delBtn = new MD3Button("Delete", MD3ButtonStyle.Outlined, icon: MD3Icon.Delete);
            delBtn.clicked += () => Debug.Log("[MD3] Icon Button: Delete");
            iconBtnRow.Add(delBtn);
            c.Add(iconBtnRow);

            // Loading button
            AddSection(c, "Loading Button");
            var loadRow = Row();
            var loadBtn = new MD3Button("Submit", MD3ButtonStyle.Filled);
            loadBtn.style.marginRight = 8;
            loadBtn.clicked += () =>
            {
                loadBtn.IsLoading = true;
                loadBtn.schedule.Execute(() => loadBtn.IsLoading = false).ExecuteLater(2000);
            };
            loadRow.Add(loadBtn);
            var loadHint = new Label("Click to see loading state");
            loadHint.style.color = _theme.OnSurfaceVariant;
            loadHint.style.unityTextAlign = TextAnchor.MiddleLeft;
            _themeCallbacks.Add(t => loadHint.style.color = t.OnSurfaceVariant);
            loadRow.Add(loadHint);
            c.Add(loadRow);

            // Icon Buttons
            AddSection(c, "Icon Buttons");
            var iconRow = Row();
            iconRow.Add(CreateIconButton(MD3Icon.Star, MD3IconButtonStyle.Standard, "Standard"));
            iconRow.Add(CreateIconButton(MD3Icon.Favorite, MD3IconButtonStyle.Filled, "Filled"));
            iconRow.Add(CreateIconButton(MD3Icon.Notifications, MD3IconButtonStyle.Tonal, "Tonal"));
            iconRow.Add(CreateIconButton(MD3Icon.Settings, MD3IconButtonStyle.Outlined, "Outlined"));
            c.Add(iconRow);

            // Toggle Icon Buttons
            AddSection(c, "Toggle Icon Buttons");
            var toggleRow = Row();

            var favToggle = new MD3IconButton(MD3Icon.Favorite, MD3IconButtonStyle.Standard)
                .MakeToggle(MD3Icon.Favorite, MD3Icon.Favorite);
            favToggle.tooltip = "Standard";
            favToggle.style.marginRight = 8;
            favToggle.toggled += v => Debug.Log($"[MD3] Toggle Standard: {v}");
            toggleRow.Add(favToggle);

            var favFilled = new MD3IconButton(MD3Icon.Favorite, MD3IconButtonStyle.Filled)
                .MakeToggle(MD3Icon.Favorite, MD3Icon.Favorite, selected: true);
            favFilled.tooltip = "Filled";
            favFilled.style.marginRight = 8;
            favFilled.toggled += v => Debug.Log($"[MD3] Toggle Filled: {v}");
            toggleRow.Add(favFilled);

            var bookToggle = new MD3IconButton(MD3Icon.Star, MD3IconButtonStyle.Tonal)
                .MakeToggle(MD3Icon.Star, MD3Icon.Star);
            bookToggle.tooltip = "Tonal";
            bookToggle.style.marginRight = 8;
            bookToggle.toggled += v => Debug.Log($"[MD3] Toggle Tonal: {v}");
            toggleRow.Add(bookToggle);

            var outToggle = new MD3IconButton(MD3Icon.Star, MD3IconButtonStyle.Outlined)
                .MakeToggle(MD3Icon.Star, MD3Icon.Star, selected: true);
            outToggle.tooltip = "Outlined";
            outToggle.toggled += v => Debug.Log($"[MD3] Toggle Outlined: {v}");
            toggleRow.Add(outToggle);

            c.Add(toggleRow);

            // FAB
            AddSection(c, "FAB");
            var fabRow = Row();
            var fabSmall = new MD3Fab(MD3Icon.Add, null, MD3FabSize.Small);
            fabSmall.clicked += () => Debug.Log("[MD3] FAB: Small");
            fabSmall.style.marginRight = 8;
            fabRow.Add(fabSmall);
            var fabStd = new MD3Fab(MD3Icon.Add);
            fabStd.clicked += () => Debug.Log("[MD3] FAB: Standard");
            fabStd.style.marginRight = 8;
            fabRow.Add(fabStd);
            var fabExt = new MD3Fab(MD3Icon.Edit, "Compose");
            fabExt.clicked += () => Debug.Log("[MD3] FAB: Extended");
            fabRow.Add(fabExt);
            c.Add(fabRow);

            // Floating FAB + Speed Dial
            _floatingFab = new MD3Fab(MD3Icon.Add).MakeFloating();
            _floatingFab.SetSpeedDial(
                (MD3Icon.Edit, "Edit", () => Debug.Log("[MD3] SpeedDial: Edit")),
                (MD3Icon.Mail, "Mail", () => Debug.Log("[MD3] SpeedDial: Mail")),
                (MD3Icon.Star, "Star", () => Debug.Log("[MD3] SpeedDial: Star"))
            );
            rootVisualElement.Add(_floatingFab);

            // Split Button
            AddSection(c, "Split Button");
            var splitRow = Row();
            splitRow.Add(new MD3SplitButton("Save",
                () => Debug.Log("[MD3] SplitButton: Main"),
                () => Debug.Log("[MD3] SplitButton: Dropdown")));
            c.Add(splitRow);
        }

        void Build_Inputs(VisualElement c)
        {
            c.Add(new MD3Text("Inputs", MD3TextStyle.HeadlineMedium));

            // TextField
            AddSection(c, "TextField");
            var outlinedField = new MD3TextField("Outlined Label", MD3TextFieldStyle.Outlined);
            outlinedField.style.marginBottom = 12;
            c.Add(outlinedField);
            var filledField = new MD3TextField("Filled Label", MD3TextFieldStyle.Filled);
            filledField.style.marginBottom = 12;
            c.Add(filledField);

            // TextField with icons
            AddSection(c, "TextField — Icons");
            var iconField = new MD3TextField("Search", MD3TextFieldStyle.Outlined,
                leadingIcon: MD3Icon.Search, trailingIcon: MD3Icon.Close);
            iconField.trailingIconClicked += () => { iconField.Value = ""; };
            iconField.style.marginBottom = 12;
            c.Add(iconField);

            // TextField with error
            AddSection(c, "TextField — Error");
            var errorField = new MD3TextField("Email", MD3TextFieldStyle.Outlined,
                helperText: "Enter a valid email address");
            errorField.changed += v =>
            {
                bool invalid = !string.IsNullOrEmpty(v) && !v.Contains("@");
                errorField.HasError = invalid;
                errorField.ErrorText = invalid ? "Invalid email format" : null;
            };
            errorField.style.marginBottom = 12;
            c.Add(errorField);

            // TextField with counter
            AddSection(c, "TextField — Counter");
            var counterField = new MD3TextField("Username", MD3TextFieldStyle.Outlined,
                helperText: "Max 20 characters", maxLength: 20);
            counterField.style.marginBottom = 12;
            c.Add(counterField);

            // Multiline TextField
            AddSection(c, "TextField — Multiline");
            var multiField = new MD3TextField("Description", MD3TextFieldStyle.Outlined,
                helperText: "Write a short description", multiline: true);
            multiField.style.marginBottom = 12;
            c.Add(multiField);

            // Plain TextField
            AddSection(c, "TextField — Plain");
            var plainField = new MD3TextField("", MD3TextFieldStyle.Plain,
                placeholder: "Type something...");
            plainField.style.marginBottom = 12;
            c.Add(plainField);

            // Plain with border
            var plainBordered = new MD3TextField("", MD3TextFieldStyle.Plain,
                placeholder: "With border");
            plainBordered.BorderWidth = 1f;
            plainBordered.BorderRadius = 8f;
            plainBordered.CustomBorderColor = _theme.OutlineVariant;
            plainBordered.CustomFocusBorderColor = _theme.Primary;
            plainBordered.style.marginBottom = 12;
            _themeCallbacks.Add(t =>
            {
                plainBordered.CustomBorderColor = t.OutlineVariant;
                plainBordered.CustomFocusBorderColor = t.Primary;
            });
            c.Add(plainBordered);

            // Pill-shaped (chat-like)
            var pillField = new MD3TextField("", MD3TextFieldStyle.Plain,
                placeholder: "Send a message...", trailingIcon: MD3Icon.Send);
            pillField.BorderWidth = 1f;
            pillField.BorderRadius = 9999f;
            pillField.CustomBorderColor = _theme.OutlineVariant;
            pillField.CustomFocusBorderColor = _theme.Primary;
            pillField.CustomBackgroundColor = _theme.SurfaceContainerLow;
            pillField.trailingIconClicked += () => Debug.Log($"[MD3] Send: {pillField.Value}");
            pillField.style.marginBottom = 12;
            _themeCallbacks.Add(t =>
            {
                pillField.CustomBorderColor = t.OutlineVariant;
                pillField.CustomFocusBorderColor = t.Primary;
                pillField.CustomBackgroundColor = t.SurfaceContainerLow;
            });
            c.Add(pillField);

            // Custom styled
            var customField = new MD3TextField("", MD3TextFieldStyle.Plain,
                placeholder: "Custom styled", leadingIcon: MD3Icon.Search);
            customField.BorderWidth = 2f;
            customField.BorderRadius = 16f;
            customField.CustomBorderColor = _theme.SecondaryContainer;
            customField.CustomFocusBorderColor = _theme.Secondary;
            customField.CustomBackgroundColor = _theme.SurfaceContainerLowest;
            customField.style.marginBottom = 12;
            _themeCallbacks.Add(t =>
            {
                customField.CustomBorderColor = t.SecondaryContainer;
                customField.CustomFocusBorderColor = t.Secondary;
                customField.CustomBackgroundColor = t.SurfaceContainerLowest;
            });
            c.Add(customField);

            // Slider
            AddSection(c, "Slider");
            var sliderRow = Row();
            var sliderLabel = new Label("0.50");
            sliderLabel.style.color = _theme.OnSurface;
            sliderLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            sliderLabel.style.width = 40;
            sliderLabel.style.marginLeft = 8;
            _themeCallbacks.Add(t => sliderLabel.style.color = t.OnSurface);
            var slider = new MD3Slider(0.5f);
            slider.style.flexGrow = 1;
            slider.changed += v => sliderLabel.text = v.ToString("F2");
            sliderRow.Add(slider);
            sliderRow.Add(sliderLabel);
            c.Add(sliderRow);

            var steppedRow = Row();
            var steppedLabel = new Label("50");
            steppedLabel.style.color = _theme.OnSurface;
            steppedLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            steppedLabel.style.width = 40;
            steppedLabel.style.marginLeft = 8;
            _themeCallbacks.Add(t => steppedLabel.style.color = t.OnSurface);
            var steppedSlider = new MD3Slider(50f, 0f, 100f, 10f);
            steppedSlider.style.flexGrow = 1;
            steppedSlider.changed += v => steppedLabel.text = v.ToString("F0");
            steppedRow.Add(steppedSlider);
            steppedRow.Add(steppedLabel);
            c.Add(steppedRow);

            // NumberField
            AddSection(c, "Number Field");
            var numRow = new MD3Row(gap: MD3Spacing.M);
            var intField = new MD3NumberField("Quantity", 1f, 0f, 100f, 1f, intMode: true);
            intField.style.width = 180;
            intField.changed += v => Debug.Log($"[MD3] Int: {v}");
            numRow.Add(intField);
            var floatField = new MD3NumberField("Scale", 1.0f, 0.1f, 10f, 0.1f);
            floatField.style.width = 180;
            floatField.changed += v => Debug.Log($"[MD3] Float: {v}");
            numRow.Add(floatField);
            c.Add(numRow);

            // Dropdown
            AddSection(c, "Dropdown");
            var dropdown = new MD3Dropdown("Fruit",
                new[] { "Apple", "Banana", "Cherry", "Date" }, 0,
                idx => Debug.Log($"[MD3] Dropdown: {idx}"));
            c.Add(dropdown);

            // DatePicker
            AddSection(c, "Date Picker");
            var dateRow = new MD3Row(gap: MD3Spacing.M);
            var datePicker = new MD3DatePicker("Start Date");
            datePicker.style.width = 220;
            datePicker.changed += d => Debug.Log($"[MD3] Date: {d:yyyy/MM/dd}");
            dateRow.Add(datePicker);
            var datePicker2 = new MD3DatePicker("End Date", new System.DateTime(2026, 12, 31));
            datePicker2.style.width = 220;
            datePicker2.changed += d => Debug.Log($"[MD3] Date2: {d:yyyy/MM/dd}");
            dateRow.Add(datePicker2);
            c.Add(dateRow);

            // SearchBar
            AddSection(c, "Search Bar");
            var allFruits = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry", "Date", "Fig", "Grape" };
            MD3SearchBar searchBar = null;
            searchBar = new MD3SearchBar("Search fruits...", query =>
            {
                if (string.IsNullOrEmpty(query))
                {
                    searchBar.SetSuggestions(Array.Empty<string>());
                    return;
                }
                var q = query.ToLowerInvariant();
                var matches = Array.FindAll(allFruits, f => f.ToLowerInvariant().Contains(q));
                searchBar.SetSuggestions(matches);
            });
            searchBar.suggestionSelected += idx => Debug.Log($"[MD3] SearchBar selected: {idx}");
            c.Add(searchBar);
        }

        void Build_Selection(VisualElement c)
        {
            c.Add(new MD3Text("Selection", MD3TextStyle.HeadlineMedium));

            // Chips
            AddSection(c, "Chips");
            var chipRow = Row();
            chipRow.Add(new MD3Chip("Filter A", selected: true));
            chipRow.Add(new MD3Chip("Filter B"));
            chipRow.Add(new MD3Chip("Closeable", closeable: true));
            c.Add(chipRow);

            // Switch
            AddSection(c, "Switch");
            var switchRow = Row();
            var switchLabel = new Label("Enable feature");
            switchLabel.style.color = _theme.OnSurface;
            switchLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            switchLabel.style.flexGrow = 1;
            switchRow.Add(switchLabel);
            _themeCallbacks.Add(t => switchLabel.style.color = t.OnSurface);
            switchRow.Add(new MD3Switch(true));
            c.Add(switchRow);

            // Checkbox
            AddSection(c, "Checkbox");
            var cbRow = Row();
            var cbPairs = new[] { ("Checked", true, false), ("Unchecked", false, false), ("Disabled", true, true) };
            foreach (var (text, val, disabled) in cbPairs)
            {
                var cb = new MD3Checkbox(val, text);
                if (disabled) cb.IsDisabled = true;
                cb.style.marginRight = 16;
                cbRow.Add(cb);
            }
            c.Add(cbRow);

            // Radio
            AddSection(c, "Radio");
            var radioRow = Row();
            var radios = new MD3Radio[3];
            var radioLabels = new[] { "Option A", "Option B", "Option C" };
            for (int i = 0; i < 3; i++)
            {
                var radio = new MD3Radio(i == 0, radioLabels[i]);
                radios[i] = radio;
                var idx = i;
                radio.changed += v =>
                {
                    if (!v) return;
                    for (int j = 0; j < radios.Length; j++)
                        if (j != idx) radios[j].Selected = false;
                };
                radio.style.marginRight = 16;
                radioRow.Add(radio);
            }
            c.Add(radioRow);

            // SegmentedButton
            AddSection(c, "Segmented Button");
            c.Add(new MD3SegmentedButton(new[] { "Day", "Week", "Month" }, 0,
                idx => Debug.Log($"[MD3] Segment: {idx}")));
        }

        void Build_Display(VisualElement c)
        {
            c.Add(new MD3Text("Display", MD3TextStyle.HeadlineMedium));

            // Cards
            AddSection(c, "Cards");
            var cardElevated = new MD3Card("Elevated Card", "Surface container high background with subtle shadow.", MD3CardStyle.Elevated);
            cardElevated.style.marginBottom = 8;
            c.Add(cardElevated);
            var cardFilled = new MD3Card("Filled Card", "Surface variant background.", MD3CardStyle.Filled);
            cardFilled.style.marginBottom = 8;
            c.Add(cardFilled);
            c.Add(new MD3Card("Outlined Card", "Surface background with outline border.", MD3CardStyle.Outlined));

            // Clickable Cards
            AddSection(c, "Clickable Cards");
            var clickElevated = new MD3Card("Clickable Elevated", "Hover and click me!", MD3CardStyle.Elevated);
            clickElevated.clicked += () => Debug.Log("[MD3] Card: Clickable Elevated");
            clickElevated.style.marginBottom = 8;
            c.Add(clickElevated);
            var clickFilled = new MD3Card("Clickable Filled", "Hover and click me!", MD3CardStyle.Filled);
            clickFilled.clicked += () => Debug.Log("[MD3] Card: Clickable Filled");
            clickFilled.style.marginBottom = 8;
            c.Add(clickFilled);
            var clickOutlined = new MD3Card("Clickable Outlined", "Hover and click me!", MD3CardStyle.Outlined);
            clickOutlined.clicked += () => Debug.Log("[MD3] Card: Clickable Outlined");
            c.Add(clickOutlined);

            // Image
            AddSection(c, "Image");
            var imgRow = new MD3Row(gap: MD3Spacing.M);
            var img1 = new MD3Image(null, 120, 80, MD3ImageFit.Cover, MD3Radius.M);
            img1.style.backgroundColor = _theme.SurfaceContainerHighest;
            _themeCallbacks.Add(t => img1.style.backgroundColor = t.SurfaceContainerHighest);
            imgRow.Add(img1);
            var img2 = new MD3Image(null, 120, 80, MD3ImageFit.Contain, MD3Radius.M);
            img2.style.backgroundColor = _theme.SurfaceContainerHighest;
            _themeCallbacks.Add(t => img2.style.backgroundColor = t.SurfaceContainerHighest);
            imgRow.Add(img2);
            var img3 = new MD3Image(null, 80, 80, radius: MD3Radius.Full);
            img3.style.backgroundColor = _theme.SurfaceContainerHighest;
            _themeCallbacks.Add(t => img3.style.backgroundColor = t.SurfaceContainerHighest);
            imgRow.Add(img3);
            var imgHint = new MD3Text("(Set .Texture to display)", MD3TextStyle.LabelCaption);
            imgRow.Add(imgHint);
            c.Add(imgRow);

            // Thumbnail
            AddSection(c, "Thumbnail");
            var thumbRow = new MD3Row(gap: MD3Spacing.S);
            for (int ti = 0; ti < 5; ti++)
            {
                var thumb = new MD3Thumbnail(null, 64, $"Asset {ti + 1}", MD3Radius.S);
                thumb.clicked += () => Debug.Log("[MD3] Thumbnail clicked");
                thumbRow.Add(thumb);
            }
            c.Add(thumbRow);

            // Image Card
            AddSection(c, "Image Card");
            var cardRow = new MD3Row(gap: MD3Spacing.M, wrap: true);
            var imgCard1 = new MD3ImageCard("Avatar Preview", "Main avatar texture with custom shader.");
            imgCard1.style.width = 220;
            imgCard1.clicked += () => Debug.Log("[MD3] ImageCard 1");
            cardRow.Add(imgCard1);
            var imgCard2 = new MD3ImageCard("Material", "lilToon standard material.");
            imgCard2.style.width = 220;
            imgCard2.clicked += () => Debug.Log("[MD3] ImageCard 2");
            cardRow.Add(imgCard2);
            c.Add(cardRow);

            // Badge
            AddSection(c, "Badge");
            var badgeRow = Row();
            badgeRow.style.height = 40;
            badgeRow.Add(BadgeHost("Dot", new MD3Badge()));
            badgeRow.Add(BadgeHost("Count", new MD3Badge(5)));
            badgeRow.Add(BadgeHost("99+", new MD3Badge(150)));
            badgeRow.Add(BadgeHost("Text", new MD3Badge("New")));
            c.Add(badgeRow);

            // Tag
            AddSection(c, "Tag");
            var tagRow = Row();
            tagRow.Add(new MD3Tag("New"));
            var tag2 = new MD3Tag("Featured"); tag2.style.marginLeft = 8; tagRow.Add(tag2);
            var tag3 = new MD3Tag("Sale"); tag3.style.marginLeft = 8; tagRow.Add(tag3);
            c.Add(tagRow);

            // Avatar
            AddSection(c, "Avatar");
            var avatarRow = Row();
            avatarRow.Add(new MD3Avatar("A", 32f));
            var av2 = new MD3Avatar("BK", 40f); av2.style.marginLeft = 8; avatarRow.Add(av2);
            var av3 = new MD3Avatar("Z", 56f); av3.style.marginLeft = 8; avatarRow.Add(av3);
            c.Add(avatarRow);

            // ListItem
            AddSection(c, "List Items");
            c.Add(new MD3ListItem("Alice Johnson", "Last seen 5 min ago", "A", "Online"));
            c.Add(new MD3ListItem("Bob Smith", "Offline since yesterday", "B"));
            c.Add(new MD3ListItem("Simple headline item"));

            // Banner
            AddSection(c, "Banner");
            c.Add(new MD3Banner("Your connection was lost. Check your network and try again.",
                MD3Icon.Warning, "Retry", () => Debug.Log("[MD3] Banner: Retry")));

            // EmptyState
            AddSection(c, "Empty State");
            c.Add(new MD3EmptyState("No results found",
                "Try adjusting your search or filters to find what you're looking for.", MD3Icon.Search));

            // Divider
            AddSection(c, "Divider");
            c.Add(new MD3Divider());
            var divLabel = new Label("With 16px inset:");
            divLabel.style.color = _theme.OnSurfaceVariant;
            divLabel.style.fontSize = 12;
            divLabel.style.marginTop = 8;
            divLabel.style.marginBottom = 4;
            _themeCallbacks.Add(t => divLabel.style.color = t.OnSurfaceVariant);
            c.Add(divLabel);
            c.Add(new MD3Divider(16f));
        }

        void Build_Shapes(VisualElement c)
        {
            c.Add(new MD3Text("Shapes", MD3TextStyle.HeadlineMedium));

            // All presets (excluding Custom)
            var allShapes = new[]
            {
                MD3Shape.Circle, MD3Shape.Star, MD3Shape.Sakura, MD3Shape.Heart,
                MD3Shape.Hexagon, MD3Shape.Diamond, MD3Shape.Triangle, MD3Shape.Pentagon,
                MD3Shape.Octagon, MD3Shape.Cross, MD3Shape.Clover, MD3Shape.Flower,
                MD3Shape.Gear, MD3Shape.Shield, MD3Shape.Drop,
            };

            // ── All Presets ──
            AddSection(c, "All Presets");
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginBottom = 8;

            foreach (var shape in allShapes)
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 16;
                col.style.marginBottom = 12;
                col.style.width = 80;
                col.Add(new MD3ShapedAvatar(shape, 64f));
                var lbl = new Label(shape.ToString());
                lbl.style.fontSize = 11;
                lbl.style.marginTop = 4;
                lbl.style.color = _theme.OnSurfaceVariant;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                _themeCallbacks.Add(t => lbl.style.color = t.OnSurfaceVariant);
                col.Add(lbl);
                grid.Add(col);
            }
            c.Add(grid);

            // ── Rotating ──
            AddSection(c, "Rotating");
            var rotGrid = new VisualElement();
            rotGrid.style.flexDirection = FlexDirection.Row;
            rotGrid.style.flexWrap = Wrap.Wrap;
            rotGrid.style.marginBottom = 8;

            var rotSamples = new (MD3Shape shape, float speed)[]
            {
                (MD3Shape.Star, 24f), (MD3Shape.Sakura, 18f), (MD3Shape.Heart, -15f),
                (MD3Shape.Gear, 10f), (MD3Shape.Flower, 20f), (MD3Shape.Clover, -12f),
            };
            foreach (var (shape, speed) in rotSamples)
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 16;
                col.style.marginBottom = 12;
                col.style.width = 80;
                col.Add(new MD3ShapedAvatar(shape, 64f, rotationSpeed: speed));
                var lbl = new Label($"{shape}\n{speed:+0;-0}\u00b0/s");
                lbl.style.fontSize = 11;
                lbl.style.marginTop = 4;
                lbl.style.color = _theme.OnSurfaceVariant;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                lbl.style.whiteSpace = WhiteSpace.Normal;
                _themeCallbacks.Add(t => lbl.style.color = t.OnSurfaceVariant);
                col.Add(lbl);
                rotGrid.Add(col);
            }
            c.Add(rotGrid);

            // ── Sizes ──
            AddSection(c, "Sizes");
            var sizeRow = Row();
            foreach (var sz in new[] { 32f, 48f, 64f, 80f, 110f })
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 16;
                col.Add(new MD3ShapedAvatar(MD3Shape.Star, sz, rotationSpeed: 20f));
                var lbl = new Label($"{sz}px");
                lbl.style.fontSize = 11;
                lbl.style.marginTop = 4;
                lbl.style.color = _theme.OnSurfaceVariant;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                _themeCallbacks.Add(t => lbl.style.color = t.OnSurfaceVariant);
                col.Add(lbl);
                sizeRow.Add(col);
            }
            c.Add(sizeRow);

            // ── Custom Parameters ──
            AddSection(c, "Custom Parameters");
            var customRow = Row();
            var customs = new (string name, MD3ShapeParams p)[]
            {
                ("Star 4pt",    new MD3ShapeParams { VertexCount = 4, InnerRatio = 0.4f, Roundness = 0.3f }),
                ("Star 8pt",    new MD3ShapeParams { VertexCount = 8, InnerRatio = 0.5f, Roundness = 0.2f }),
                ("Star sharp",  new MD3ShapeParams { VertexCount = 5, InnerRatio = 0.2f, Roundness = 0f }),
                ("Star round",  new MD3ShapeParams { VertexCount = 5, InnerRatio = 0.38f, Roundness = 0.8f }),
                ("Hex sharp",   new MD3ShapeParams { VertexCount = 6, InnerRatio = 1f, Roundness = 0f }),
                ("Hex round",   new MD3ShapeParams { VertexCount = 6, InnerRatio = 1f, Roundness = 0.7f }),
            };
            foreach (var (name, p) in customs)
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 16;
                col.style.width = 80;
                col.Add(new MD3ShapedAvatar(MD3Shape.Custom, 64f, customParams: p));
                var lbl = new Label(name);
                lbl.style.fontSize = 10;
                lbl.style.marginTop = 4;
                lbl.style.color = _theme.OnSurfaceVariant;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                _themeCallbacks.Add(t => lbl.style.color = t.OnSurfaceVariant);
                col.Add(lbl);
                customRow.Add(col);
            }
            c.Add(customRow);

            // ── Morphing ──
            AddSection(c, "Morphing");
            var morphShapes = new[]
            {
                MD3Shape.Circle, MD3Shape.Star, MD3Shape.Sakura, MD3Shape.Heart,
                MD3Shape.Hexagon, MD3Shape.Diamond, MD3Shape.Clover, MD3Shape.Flower,
                MD3Shape.Gear, MD3Shape.Drop,
            };
            int morphIdx = 0;
            var morphAvatar = new MD3ShapedAvatar(MD3Shape.Circle, 110f);
            var morphLabel = new Label("Circle");
            morphLabel.style.fontSize = 12;
            morphLabel.style.marginTop = 8;
            morphLabel.style.color = _theme.OnSurface;
            morphLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _themeCallbacks.Add(t => morphLabel.style.color = t.OnSurface);

            var morphCol = new VisualElement();
            morphCol.style.alignItems = Align.Center;
            morphCol.style.marginBottom = 12;
            morphCol.Add(morphAvatar);
            morphCol.Add(morphLabel);
            c.Add(morphCol);

            // Button row for morph targets
            var morphBtnRow = new VisualElement();
            morphBtnRow.style.flexDirection = FlexDirection.Row;
            morphBtnRow.style.flexWrap = Wrap.Wrap;
            morphBtnRow.style.marginBottom = 8;

            foreach (var shape in morphShapes)
            {
                var btn = new MD3Button(shape.ToString(), MD3ButtonStyle.Tonal);
                btn.style.marginRight = 6;
                btn.style.marginBottom = 6;
                var targetShape = shape;
                btn.clicked += () =>
                {
                    morphAvatar.MorphTo(targetShape, 600f, MD3Easing.EaseInOut);
                    morphLabel.text = targetShape.ToString();
                };
                morphBtnRow.Add(btn);
            }
            c.Add(morphBtnRow);

            // Auto-cycle morph demo
            var autoAvatar = new MD3ShapedAvatar(MD3Shape.Circle, 80f);
            var autoLabel = new Label("Auto cycle");
            autoLabel.style.fontSize = 11;
            autoLabel.style.marginTop = 4;
            autoLabel.style.color = _theme.OnSurfaceVariant;
            autoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _themeCallbacks.Add(t => autoLabel.style.color = t.OnSurfaceVariant);

            var autoCol = new VisualElement();
            autoCol.style.alignItems = Align.Center;
            autoCol.style.marginTop = 8;
            autoCol.Add(autoAvatar);
            autoCol.Add(autoLabel);
            c.Add(autoCol);

            // Schedule auto-cycling morphs
            int autoIdx = 0;
            autoAvatar.schedule.Execute(() =>
            {
                autoIdx = (autoIdx + 1) % morphShapes.Length;
                autoAvatar.MorphTo(morphShapes[autoIdx], 800f, MD3Easing.EaseInOut);
                autoLabel.text = $"Auto: {morphShapes[autoIdx]}";
            }).Every(2000).StartingIn(1000);
        }

        void Build_Data(VisualElement c)
        {
            c.Add(new MD3Text("Data", MD3TextStyle.HeadlineMedium));

            // ── Virtual List ──
            AddSection(c, "Virtual List");
            c.Add(new MD3Text("10,000 items with virtual scrolling — only visible items are in the DOM.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var items = new List<string>();
            for (int i = 0; i < 10000; i++)
                items.Add($"Item {i + 1} — Virtual list row");

            var virtualList = new MD3VirtualList<string>(48f);
            virtualList.SetData(items, (data, element, index) =>
            {
                if (element is MD3ListItem li)
                    li.SetHeadline(data);
            }, () => new MD3ListItem(""));
            virtualList.style.height = 300;
            virtualList.style.borderTopWidth = 1;
            virtualList.style.borderBottomWidth = 1;
            virtualList.style.borderTopColor = _theme.OutlineVariant;
            virtualList.style.borderBottomColor = _theme.OutlineVariant;
            _themeCallbacks.Add(t =>
            {
                virtualList.style.borderTopColor = t.OutlineVariant;
                virtualList.style.borderBottomColor = t.OutlineVariant;
            });
            virtualList.itemClicked += (idx, data) => Debug.Log($"[MD3] VirtualList clicked: {data}");
            c.Add(virtualList);
            c.Add(new MD3Spacer(MD3Spacing.M));

            // ── Table ──
            AddSection(c, "Table");
            c.Add(new MD3Text("Display-only table with header and zebra striping.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var table = new MD3Table()
                .AddColumn("Name", width: 180)
                .AddColumn("Type", width: 120)
                .AddColumn("Size", width: 80, align: TextAnchor.MiddleRight);
            table.AddRow("Body.mesh", "SkinnedMesh", "1,234");
            table.AddRow("Hair.mesh", "SkinnedMesh", "567");
            table.AddRow("Outfit.mat", "Material", "32");
            table.AddRow("Texture.png", "Texture2D", "4,096");
            table.AddRow("Icon.png", "Sprite", "128");
            table.style.height = 300;
            table.Radius(MD3Radius.M).ClipContent();
            c.Add(table);

            // ── Table with custom cells ──
            AddSection(c, "Table — Custom Cells");
            c.Add(new MD3Text("Cells can contain any VisualElement.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var customTable = new MD3Table()
                .AddColumn("Item", width: 160)
                .AddColumn("Tag", width: 100)
                .AddColumn("Rating");
            customTable.AddRow(
                new Label("Featured Item") { style = { unityFontStyleAndWeight = FontStyle.Bold } },
                new MD3Tag("New"),
                new MD3Badge(5));
            customTable.AddRow(
                new Label("Regular Item"),
                new MD3Tag("Sale"),
                new MD3Badge(12));
            customTable.AddRow(
                new Label("Another Item"),
                new MD3Chip("Filter", selected: true),
                new MD3Badge("99+"));
            customTable.style.height = 220;
            customTable.Radius(MD3Radius.M).ClipContent();
            c.Add(customTable);

            // ── Data Table ──
            AddSection(c, "Data Table");
            c.Add(new MD3Text("Sortable columns (click header) and row selection.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var dataTable = new MD3DataTable();
            dataTable.SelectionMode = MD3SelectionMode.Single;
            dataTable.AddColumn("Component", width: 160, sortable: true);
            dataTable.AddColumn("Count", width: 80, align: TextAnchor.MiddleRight, sortable: true);
            dataTable.AddColumn("Status", width: 100, sortable: true);
            dataTable.AddRow("PhysBone", "12", "Active");
            dataTable.AddRow("Constraint", "8", "Active");
            dataTable.AddRow("Collider", "24", "Warning");
            dataTable.AddRow("Contact", "6", "Active");
            dataTable.AddRow("Renderer", "3", "Active");
            dataTable.AddRow("MeshFilter", "5", "Active");
            dataTable.style.height = 360;
            dataTable.Radius(MD3Radius.M).ClipContent();
            dataTable.sortChanged += (col, asc) => Debug.Log($"[MD3] Sort: column={col} asc={asc}");
            dataTable.selectionChanged += indices => Debug.Log($"[MD3] Selected: {string.Join(",", indices)}");
            c.Add(dataTable);

            // ── Data Table (Multi-select) ──
            AddSection(c, "Data Table — Multi Select");
            var multiTable = new MD3DataTable();
            multiTable.SelectionMode = MD3SelectionMode.Multi;
            multiTable.AddColumn("Bone", width: 180, sortable: true);
            multiTable.AddColumn("Weight", width: 80, align: TextAnchor.MiddleRight, sortable: true);
            multiTable.AddColumn("Parent", width: 140);
            multiTable.AddRow("Hips", "1.00", "Armature");
            multiTable.AddRow("Spine", "1.00", "Hips");
            multiTable.AddRow("Chest", "0.85", "Spine");
            multiTable.AddRow("Head", "1.00", "Neck");
            multiTable.AddRow("LeftArm", "1.00", "LeftShoulder");
            multiTable.AddRow("RightArm", "1.00", "RightShoulder");
            multiTable.style.height = 360;
            multiTable.Radius(MD3Radius.M).ClipContent();
            multiTable.selectionChanged += indices => Debug.Log($"[MD3] Multi: {string.Join(",", indices)}");
            c.Add(multiTable);
        }

        void Build_Navigation(VisualElement c)
        {
            c.Add(new MD3Text("Navigation", MD3TextStyle.HeadlineMedium));

            // Toolbar
            AddSection(c, "Toolbar");
            var toolbar = new MD3Toolbar()
                .AddLabel("Document")
                .AddSpacer()
                .Add(new MD3IconButton(MD3Icon.ContentCut, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .Add(new MD3IconButton(MD3Icon.ContentCopy, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .Add(new MD3IconButton(MD3Icon.ContentPaste, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .AddDivider()
                .Add(new MD3IconButton(MD3Icon.Undo, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .Add(new MD3IconButton(MD3Icon.Refresh, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .AddDivider()
                .Add(new MD3IconButton(MD3Icon.MoreVert, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small));
            c.Add(toolbar);

            var toolbar2 = new MD3Toolbar()
                .Add(new MD3IconButton(MD3Icon.FormatSize, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small))
                .AddDivider()
                .Add(new MD3Button("Insert", MD3ButtonStyle.Tonal, icon: MD3Icon.Add, size: MD3ButtonSize.Small))
                .Add(new MD3Button("Export", MD3ButtonStyle.Outlined, icon: MD3Icon.Save, size: MD3ButtonSize.Small))
                .AddSpacer()
                .Add(new MD3Chip("Filter A", selected: true))
                .Add(new MD3Chip("Filter B"));
            toolbar2.style.marginTop = MD3Spacing.S;
            c.Add(toolbar2);

            // TopAppBar
            AddSection(c, "Top App Bar");
            var appBar = new MD3TopAppBar("My Application", MD3Icon.Menu);
            appBar.navigationClicked += () => Debug.Log("[MD3] AppBar: Nav");
            var appBarAction = new MD3IconButton(MD3Icon.MoreVert, MD3IconButtonStyle.Standard);
            appBarAction.clicked += () => Debug.Log("[MD3] AppBar: More");
            appBar.Actions.Add(appBarAction);
            c.Add(appBar);

            // Tabs
            AddSection(c, "Tabs");
            c.Add(new MD3TabBar(new[] { "Home", "Profile", "Settings" }, 0,
                idx => Debug.Log($"[MD3] Tab: {idx}")));

            // NavBar
            AddSection(c, "Navigation Bar");
            c.Add(new MD3NavBar(
                new[] { (MD3Icon.Home, "Home"), (MD3Icon.Star, "Starred"), (MD3Icon.Mail, "Mail"), (MD3Icon.Settings, "Settings") },
                0, idx => Debug.Log($"[MD3] NavBar: {idx}")));

            // NavRail
            AddSection(c, "Navigation Rail");
            c.Add(new MD3NavRail(
                new[] { (MD3Icon.Home, "Home"), (MD3Icon.Mail, "Mail"), (MD3Icon.Settings, "Settings") },
                0, idx => Debug.Log($"[MD3] NavRail: {idx}")));

            // NavDrawer
            AddSection(c, "Navigation Drawer");
            c.Add(new MD3NavDrawer(
                new[] { (MD3Icon.Home, "Home", 0), (MD3Icon.Mail, "Inbox", 24), (MD3Icon.Star, "Starred", 3), (MD3Icon.Settings, "Settings", 0) },
                0, idx => Debug.Log($"[MD3] NavDrawer: {idx}")));

            // Foldout
            AddSection(c, "Foldout");
            var foldout1 = new MD3Foldout("Expanded Section", expanded: true);
            var fc1 = new Label("This content is visible when expanded.\nYou can put any elements here.");
            fc1.style.color = _theme.OnSurface;
            fc1.style.marginTop = 8;
            fc1.style.marginBottom = 8;
            _themeCallbacks.Add(t => fc1.style.color = t.OnSurface);
            foldout1.Content.Add(fc1);
            c.Add(foldout1);

            var foldout2 = new MD3Foldout("Collapsed Section");
            var fc2 = new Label("Hidden content revealed on expand.");
            fc2.style.color = _theme.OnSurface;
            fc2.style.marginTop = 8;
            fc2.style.marginBottom = 8;
            _themeCallbacks.Add(t => fc2.style.color = t.OnSurface);
            foldout2.Content.Add(fc2);
            c.Add(foldout2);

            // Stepper
            AddSection(c, "Stepper");
            var stepperLabels = new[] { "Account", "Details", "Review", "Done" };
            var stepper = new MD3Stepper(stepperLabels, 1);
            stepper.style.marginBottom = 12;
            c.Add(stepper);
            var stepperBtnRow = Row();
            var prevBtn = new MD3Button("Previous", MD3ButtonStyle.Outlined);
            prevBtn.style.marginRight = 8;
            prevBtn.clicked += () =>
            {
                if (stepper.CurrentStep > 0) stepper.CurrentStep--;
            };
            stepperBtnRow.Add(prevBtn);
            var nextBtn = new MD3Button("Next", MD3ButtonStyle.Filled);
            nextBtn.clicked += () =>
            {
                if (stepper.CurrentStep < stepperLabels.Length - 1) stepper.CurrentStep++;
            };
            stepperBtnRow.Add(nextBtn);
            c.Add(stepperBtnRow);
        }

        void Build_Feedback(VisualElement c)
        {
            c.Add(new MD3Text("Feedback", MD3TextStyle.HeadlineMedium));

            // Snackbar
            AddSection(c, "Snackbar");
            var snackbarRow = Row();
            var snackbarBtn = new MD3Button("Show Snackbar", MD3ButtonStyle.Tonal);
            snackbarBtn.clicked += () =>
            {
                var snackbar = new MD3Snackbar("Item deleted", "Undo",
                    () => Debug.Log("[MD3] Snackbar: Undo"));
                snackbar.Show(rootVisualElement);
            };
            snackbarRow.Add(snackbarBtn);
            c.Add(snackbarRow);

            // Tooltip
            AddSection(c, "Tooltip");
            var tooltipRow = Row();
            var plainTipBtn = new MD3Button("Hover: Plain Tooltip", MD3ButtonStyle.Outlined);
            MD3Tooltip _activePlainTip = null;
            plainTipBtn.RegisterCallback<MouseEnterEvent>(e =>
            {
                _activePlainTip = MD3Tooltip.Plain("Brief info");
                _activePlainTip.ShowAt(plainTipBtn);
            });
            plainTipBtn.RegisterCallback<MouseLeaveEvent>(e =>
            {
                _activePlainTip?.Hide();
                _activePlainTip = null;
            });
            tooltipRow.Add(plainTipBtn);

            var richTipBtn = new MD3Button("Click: Rich Tooltip", MD3ButtonStyle.Outlined);
            richTipBtn.style.marginLeft = 8;
            MD3Tooltip _activeRichTip = null;
            richTipBtn.clicked += () =>
            {
                if (_activeRichTip != null) { _activeRichTip.Hide(); _activeRichTip = null; return; }
                _activeRichTip = MD3Tooltip.Rich("Rich Tooltip",
                    "This tooltip shows detailed information with an optional action button.",
                    "Learn More", () => Debug.Log("[MD3] Tooltip action"));
                _activeRichTip.ShowAt(richTipBtn);
            };
            tooltipRow.Add(richTipBtn);
            c.Add(tooltipRow);

            // Dialog
            AddSection(c, "Dialog");
            var dialogRow = Row();
            var dialogBtn = new MD3Button("Show Dialog", MD3ButtonStyle.Filled);
            dialogBtn.clicked += () =>
            {
                var dialog = new MD3Dialog("Delete Item?",
                    "This action cannot be undone. The item will be permanently removed.",
                    "Delete", "Cancel",
                    () => Debug.Log("[MD3] Dialog: Confirmed"),
                    () => Debug.Log("[MD3] Dialog: Dismissed"));
                dialog.Show(rootVisualElement);
            };
            dialogRow.Add(dialogBtn);
            c.Add(dialogRow);

            // MenuItem
            AddSection(c, "Menu Items");
            var menuCard = new MD3Card("", "", MD3CardStyle.Outlined);
            menuCard.style.paddingTop = 4;
            menuCard.style.paddingBottom = 4;
            menuCard.style.paddingLeft = 0;
            menuCard.style.paddingRight = 0;
            var menuItems = new[] { ("Cut", "Ctrl+X"), ("Copy", "Ctrl+C"), ("Paste", "Ctrl+V") };
            foreach (var (label, shortcut) in menuItems)
            {
                var mi = new MD3MenuItem(label, shortcut);
                mi.clicked += () => Debug.Log($"[MD3] MenuItem: {label}");
                menuCard.Add(mi);
            }
            menuCard.Add(new MD3Divider());
            var mi4 = new MD3MenuItem("Select All", "Ctrl+A");
            mi4.clicked += () => Debug.Log("[MD3] MenuItem: Select All");
            menuCard.Add(mi4);
            c.Add(menuCard);

            // DialogRadio
            AddSection(c, "Dialog Radio");
            var drCard = new MD3Card("", "", MD3CardStyle.Outlined);
            drCard.style.paddingTop = 4;
            drCard.style.paddingBottom = 4;
            drCard.style.paddingLeft = 0;
            drCard.style.paddingRight = 0;
            var dialogRadios = new MD3DialogRadio[3];
            var drLabels = new[] { "Option Alpha", "Option Beta", "Option Gamma" };
            for (int i = 0; i < 3; i++)
            {
                var dr = new MD3DialogRadio(drLabels[i], i == 0);
                dialogRadios[i] = dr;
                var idx = i;
                dr.changed += v =>
                {
                    if (!v) return;
                    for (int j = 0; j < dialogRadios.Length; j++)
                        if (j != idx) dialogRadios[j].Selected = false;
                };
                drCard.Add(dr);
            }
            c.Add(drCard);

            // BottomSheet
            AddSection(c, "Bottom Sheet");
            var bottomSheetRow = Row();
            var bsBtn = new MD3Button("Show Bottom Sheet", MD3ButtonStyle.Tonal);
            bsBtn.clicked += () =>
            {
                var sheet = new MD3BottomSheet("Select an option");
                sheet.Content.Add(new MD3ListItem("Share", "Share this item with others", MD3Icon.Share));
                sheet.Content.Add(new MD3ListItem("Link", "Copy link to clipboard", MD3Icon.Link));
                sheet.Content.Add(new MD3ListItem("Edit", "Modify this item", MD3Icon.Edit));
                sheet.Content.Add(new MD3ListItem("Delete", "Remove this item permanently", MD3Icon.Delete));
                sheet.dismissed += () => Debug.Log("[MD3] BottomSheet: Dismissed");
                sheet.Show(rootVisualElement);
            };
            bottomSheetRow.Add(bsBtn);
            c.Add(bottomSheetRow);

            // ContextMenu
            AddSection(c, "Context Menu");
            var ctxRow = Row();
            var ctxBtn = new MD3Button("Show Context Menu", MD3ButtonStyle.Tonal);
            ctxBtn.clicked += () =>
            {
                var menu = new MD3ContextMenu(new (string, Action)[]
                {
                    ("Cut", () => Debug.Log("[MD3] ContextMenu: Cut")),
                    ("Copy", () => Debug.Log("[MD3] ContextMenu: Copy")),
                    ("Paste", () => Debug.Log("[MD3] ContextMenu: Paste")),
                });
                menu.ShowAt(ctxBtn);
            };
            ctxRow.Add(ctxBtn);

            var ctxRightClickBtn = new MD3Button("Right-Click Me", MD3ButtonStyle.Outlined);
            ctxRightClickBtn.style.marginLeft = 8;
            ctxRightClickBtn.RegisterCallback<ContextClickEvent>(e =>
            {
                var menu = new MD3ContextMenu(new (string, Action)[]
                {
                    ("Inspect", () => Debug.Log("[MD3] ContextMenu: Inspect")),
                    ("Rename", () => Debug.Log("[MD3] ContextMenu: Rename")),
                    ("Delete", () => Debug.Log("[MD3] ContextMenu: Delete")),
                });
                var local = e.localMousePosition;
                var world = ctxRightClickBtn.LocalToWorld(local);
                var rootWorld = rootVisualElement.worldBound;
                menu.ShowAtPosition(rootVisualElement, world.x - rootWorld.x, world.y - rootWorld.y);
            });
            ctxRow.Add(ctxRightClickBtn);
            c.Add(ctxRow);

            // SideSheet
            AddSection(c, "Side Sheet");
            var sideSheetRow = Row();
            var ssBtn = new MD3Button("Show Side Sheet", MD3ButtonStyle.Tonal);
            ssBtn.clicked += () =>
            {
                var sheet = new MD3SideSheet("Settings");
                sheet.Content.Add(new MD3ListItem("Account", "Manage your account settings", MD3Icon.Person));
                sheet.Content.Add(new MD3ListItem("Notifications", "Configure notification preferences", MD3Icon.Notifications));
                sheet.Content.Add(new MD3ListItem("Privacy", "Review privacy settings", MD3Icon.Lock));
                sheet.Content.Add(new MD3ListItem("About", "Application information", MD3Icon.Info));
                sheet.dismissed += () => Debug.Log("[MD3] SideSheet: Dismissed");
                sheet.Show(rootVisualElement);
            };
            sideSheetRow.Add(ssBtn);
            c.Add(sideSheetRow);

            // FullScreenDialog
            AddSection(c, "Full Screen Dialog");
            var fsdRow = Row();
            var fsdBtn = new MD3Button("Show Full Screen Dialog", MD3ButtonStyle.Filled);
            fsdBtn.clicked += () =>
            {
                var dialog = new MD3FullScreenDialog("New Document", "Save", () =>
                {
                    Debug.Log("[MD3] FullScreenDialog: Save");
                });
                dialog.Content.Add(new MD3TextField("Title", MD3TextFieldStyle.Outlined));
                dialog.Content.Add(Spacer(16));
                dialog.Content.Add(new MD3TextField("Description", MD3TextFieldStyle.Outlined));
                dialog.dismissed += () => Debug.Log("[MD3] FullScreenDialog: Dismissed");
                dialog.Show(rootVisualElement);
            };
            fsdRow.Add(fsdBtn);
            c.Add(fsdRow);
        }

        void Build_Animation(VisualElement c)
        {
            c.Add(new MD3Text("Animation", MD3TextStyle.HeadlineMedium));

            // ── Transitions ──
            AddSection(c, "Transitions");
            c.Add(new MD3Text("Show/hide elements with animated transitions.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var transTarget = new MD3Card("Animated Card", "This card animates in and out.", MD3CardStyle.Filled);

            var transRow = new MD3Row(gap: MD3Spacing.S, wrap: true);
            transRow.style.marginBottom = MD3Spacing.S;

            var fadeInBtn = new MD3Button("Fade In", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            fadeInBtn.clicked += () => MD3Transition.FadeIn(transTarget);
            transRow.Add(fadeInBtn);

            var fadeOutBtn = new MD3Button("Fade Out", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            fadeOutBtn.clicked += () => MD3Transition.FadeOut(transTarget);
            transRow.Add(fadeOutBtn);

            var slideDownBtn = new MD3Button("Slide Down", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            slideDownBtn.clicked += () => MD3Transition.SlideDown(transTarget);
            transRow.Add(slideDownBtn);

            var slideUpBtn = new MD3Button("Slide Up", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            slideUpBtn.clicked += () => MD3Transition.SlideUp(transTarget);
            transRow.Add(slideUpBtn);

            var scaleInBtn = new MD3Button("Scale In", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            scaleInBtn.clicked += () => MD3Transition.ScaleIn(transTarget);
            transRow.Add(scaleInBtn);

            var scaleOutBtn = new MD3Button("Scale Out", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            scaleOutBtn.clicked += () => MD3Transition.ScaleOut(transTarget);
            transRow.Add(scaleOutBtn);

            c.Add(transRow);
            c.Add(transTarget);

            // ── Expand / Collapse ──
            AddSection(c, "Expand / Collapse");
            c.Add(new MD3Text("Height-based show/hide animation.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var expandTarget = new MD3Card("Expandable Content", "This section expands and collapses with a smooth height animation.", MD3CardStyle.Outlined);

            var expandRow = new MD3Row(gap: MD3Spacing.S);
            expandRow.style.marginBottom = MD3Spacing.S;

            var expandBtn = new MD3Button("Expand", MD3ButtonStyle.Filled, icon: MD3Icon.ExpandMore, size: MD3ButtonSize.Small);
            expandBtn.clicked += () => MD3Transition.Expand(expandTarget, 80f, 300f);
            expandRow.Add(expandBtn);

            var collapseBtn = new MD3Button("Collapse", MD3ButtonStyle.Outlined, icon: MD3Icon.ExpandLess, size: MD3ButtonSize.Small);
            collapseBtn.clicked += () => MD3Transition.Collapse(expandTarget, 300f);
            expandRow.Add(collapseBtn);

            c.Add(expandRow);
            c.Add(expandTarget);

            // ── Crossfade ──
            AddSection(c, "Crossfade");
            c.Add(new MD3Text("Swap two elements with a simultaneous fade.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var crossA = new MD3Card("Card A", "Currently visible.", MD3CardStyle.Elevated);
            var crossB = new MD3Card("Card B", "Swapped in!", MD3CardStyle.Filled);
            crossB.style.display = DisplayStyle.None;

            var crossStack = new MD3Stack();
            crossStack.Add(crossA);
            crossStack.style.height = 80;
            crossB.style.position = Position.Absolute;
            crossB.style.top = 0; crossB.style.left = 0;
            crossB.style.right = 0; crossB.style.bottom = 0;
            crossStack.hierarchy.Add(crossB);

            var crossBtn = new MD3Button("Swap", MD3ButtonStyle.Filled, icon: MD3Icon.Refresh, size: MD3ButtonSize.Small);
            bool showingA = true;
            crossBtn.clicked += () =>
            {
                if (showingA)
                    MD3Transition.Crossfade(crossA, crossB);
                else
                    MD3Transition.Crossfade(crossB, crossA);
                showingA = !showingA;
            };
            crossBtn.style.marginBottom = MD3Spacing.S;
            c.Add(crossBtn);
            c.Add(crossStack);

            // ── MD3Animate.Float ──
            AddSection(c, "Value Animation");
            c.Add(new MD3Text("Animate any float value with easing.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var animBar = new VisualElement();
            animBar.style.height = 32;
            animBar.style.width = 32;
            animBar.style.position = Position.Relative;
            animBar.style.backgroundColor = _theme.Primary;
            animBar.Radius(MD3Radius.S);
            _themeCallbacks.Add(t => animBar.style.backgroundColor = t.Primary);

            var animTrack = new VisualElement();
            animTrack.style.height = 40;
            animTrack.style.backgroundColor = _theme.SurfaceContainerLow;
            animTrack.Radius(MD3Radius.M).Padding(MD3Spacing.XS);
            animTrack.style.alignItems = Align.FlexStart;
            animTrack.style.justifyContent = Justify.Center;
            _themeCallbacks.Add(t => animTrack.style.backgroundColor = t.SurfaceContainerLow);
            animTrack.Add(animBar);

            var allEasings = new (string name, MD3Easing easing)[]
            {
                ("Linear", MD3Easing.Linear),
                ("EaseIn", MD3Easing.EaseIn),
                ("EaseOut", MD3Easing.EaseOut),
                ("EaseInOut", MD3Easing.EaseInOut),
                ("EaseOutBack", MD3Easing.EaseOutBack),
                ("EaseInBack", MD3Easing.EaseInBack),
                ("EaseOutCubic", MD3Easing.EaseOutCubic),
                ("EaseInOutCubic", MD3Easing.EaseInOutCubic),
                ("EaseOutQuart", MD3Easing.EaseOutQuart),
                ("EaseOutElastic", MD3Easing.EaseOutElastic),
                ("EaseOutBounce", MD3Easing.EaseOutBounce),
            };

            var easingRow = new MD3Row(gap: MD3Spacing.XS, wrap: true);
            easingRow.style.marginBottom = MD3Spacing.S;
            foreach (var (eName, eVal) in allEasings)
            {
                var btn = new MD3Button(eName, MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
                var easing = eVal;
                btn.clicked += () =>
                {
                    float trackW = animTrack.resolvedStyle.width;
                    if (float.IsNaN(trackW) || trackW <= 0) trackW = 300f;
                    float maxLeft = trackW - 40f;
                    float startLeft = animBar.resolvedStyle.left;
                    if (float.IsNaN(startLeft)) startLeft = 0f;
                    float endLeft = startLeft < maxLeft * 0.5f ? maxLeft : 0f;
                    MD3Animate.Float(animBar, startLeft, endLeft, 600f, easing,
                        v => animBar.style.left = v);
                };
                easingRow.Add(btn);
            }
            c.Add(easingRow);
            c.Add(animTrack);

            // ── Custom Easing ──
            AddSection(c, "Custom Easing");
            c.Add(new MD3Text("Pass any Func<float,float> as easing.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var customBar = new VisualElement();
            customBar.style.height = 32;
            customBar.style.width = 32;
            customBar.style.position = Position.Relative;
            customBar.style.backgroundColor = _theme.Tertiary;
            customBar.Radius(MD3Radius.S);
            _themeCallbacks.Add(t => customBar.style.backgroundColor = t.Tertiary);

            var customTrack = new VisualElement();
            customTrack.style.height = 40;
            customTrack.style.backgroundColor = _theme.SurfaceContainerLow;
            customTrack.Radius(MD3Radius.M).Padding(MD3Spacing.XS);
            customTrack.style.alignItems = Align.FlexStart;
            customTrack.style.justifyContent = Justify.Center;
            _themeCallbacks.Add(t => customTrack.style.backgroundColor = t.SurfaceContainerLow);
            customTrack.Add(customBar);

            var customEaseBtn = new MD3Button("sin(t*PI) ease", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            customEaseBtn.clicked += () =>
            {
                float trackW = customTrack.resolvedStyle.width;
                if (float.IsNaN(trackW) || trackW <= 0) trackW = 300f;
                float maxLeft = trackW - 40f;
                float startLeft = customBar.resolvedStyle.left;
                if (float.IsNaN(startLeft)) startLeft = 0f;
                float endLeft = startLeft < maxLeft * 0.5f ? maxLeft : 0f;
                MD3Animate.Float(customBar, startLeft, endLeft, 800f,
                    t => Mathf.Sin(t * Mathf.PI * 0.5f),
                    v => customBar.style.left = v);
            };
            customEaseBtn.style.marginBottom = MD3Spacing.S;
            c.Add(customEaseBtn);
            c.Add(customTrack);

            // ── Keyframes ──
            AddSection(c, "Keyframes");
            c.Add(new MD3Text("Multi-segment animation with per-segment easing.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var kfBox = new VisualElement();
            kfBox.style.width = 48;
            kfBox.style.height = 48;
            kfBox.style.backgroundColor = _theme.Secondary;
            kfBox.Radius(MD3Radius.M);
            _themeCallbacks.Add(t => kfBox.style.backgroundColor = t.Secondary);

            var kfBtn = new MD3Button("Squash & Bounce", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            kfBtn.clicked += () =>
            {
                MD3Animate.Keyframes(kfBox, 800f, new[]
                {
                    new MD3Keyframe(0f,    1f),
                    new MD3Keyframe(0.2f,  0.6f,  MD3Easing.EaseIn),
                    new MD3Keyframe(0.4f,  1.2f,  MD3Easing.EaseOut),
                    new MD3Keyframe(0.6f,  0.9f,  MD3Easing.EaseInOut),
                    new MD3Keyframe(0.8f,  1.05f, MD3Easing.EaseOut),
                    new MD3Keyframe(1f,    1f,    MD3Easing.EaseInOut),
                }, v =>
                {
                    kfBox.style.scale = new UnityEngine.UIElements.Scale(new Vector3(v, v, 1f));
                });
            };
            kfBtn.style.marginBottom = MD3Spacing.S;
            c.Add(kfBtn);
            c.Add(kfBox);
            c.Add(new MD3Spacer(MD3Spacing.S));

            // ── Spring ──
            AddSection(c, "Spring Physics");
            c.Add(new MD3Text("Physics-based animation with stiffness/damping.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var springBox = new VisualElement();
            springBox.style.width = 48;
            springBox.style.height = 48;
            springBox.style.position = Position.Relative;
            springBox.style.backgroundColor = _theme.Primary;
            springBox.Radius(9999);
            _themeCallbacks.Add(t => springBox.style.backgroundColor = t.Primary);

            var springTrack = new VisualElement();
            springTrack.style.height = 56;
            springTrack.style.backgroundColor = _theme.SurfaceContainerLow;
            springTrack.Radius(MD3Radius.M).Padding(MD3Spacing.XS);
            springTrack.style.alignItems = Align.FlexStart;
            springTrack.style.justifyContent = Justify.Center;
            _themeCallbacks.Add(t => springTrack.style.backgroundColor = t.SurfaceContainerLow);
            springTrack.Add(springBox);

            var springRow = new MD3Row(gap: MD3Spacing.S, wrap: true);
            springRow.style.marginBottom = MD3Spacing.S;

            MD3AnimationHandle springHandle = null;
            float springLeft = 0f;

            var springConfigs = new (string name, float stiffness, float damping)[]
            {
                ("Gentle", 80f, 10f),
                ("Bouncy", 300f, 8f),
                ("Stiff", 400f, 20f),
                ("Wobbly", 120f, 4f),
            };
            foreach (var (sName, stiff, damp) in springConfigs)
            {
                var btn = new MD3Button(sName, MD3ButtonStyle.Outlined, size: MD3ButtonSize.Small);
                var s = stiff; var d = damp;
                btn.clicked += () =>
                {
                    springHandle?.Cancel();
                    float trackW = springTrack.resolvedStyle.width;
                    if (float.IsNaN(trackW) || trackW <= 0) trackW = 300f;
                    float maxLeft = trackW - 56f;
                    float endLeft = springLeft < maxLeft * 0.5f ? maxLeft : 0f;
                    float from = springLeft;
                    springHandle = MD3Animate.Spring(springBox, from, endLeft,
                        v => { springLeft = v; springBox.style.left = v; },
                        stiffness: s, damping: d);
                };
                springRow.Add(btn);
            }
            c.Add(springRow);
            c.Add(springTrack);

            // ── Fluent Tween ──
            AddSection(c, "Fluent Tween Builder");
            c.Add(new MD3Text("Chain animations with .Then() / .With(), multi-property, yoyo, repeat.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var tweenBox = new VisualElement();
            tweenBox.style.width = 48;
            tweenBox.style.height = 48;
            tweenBox.style.backgroundColor = _theme.TertiaryContainer;
            tweenBox.Radius(MD3Radius.M);
            _themeCallbacks.Add(t => tweenBox.style.backgroundColor = t.TertiaryContainer);

            var tweenRow = new MD3Row(gap: MD3Spacing.S, wrap: true);
            tweenRow.style.marginBottom = MD3Spacing.S;

            MD3AnimationHandle tweenHandle = null;
            Action resetTweenBox = () =>
            {
                tweenHandle?.Cancel();
                tweenBox.style.scale = new UnityEngine.UIElements.Scale(Vector3.one);
                tweenBox.style.rotate = new Rotate(0f);
                tweenBox.style.opacity = 1f;
            };

            // Multi-property
            var mpBtn = new MD3Button("Multi-Property", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            mpBtn.clicked += () =>
            {
                resetTweenBox();
                tweenHandle = MD3Animate.Tween(tweenBox)
                    .Duration(600f)
                    .Ease(MD3Easing.EaseOutBack)
                    .Scale(1f, 1.3f)
                    .Rotate(0f, 180f)
                    .Opacity(1f, 0.5f)
                    .Then()
                        .Duration(400f)
                        .Ease(MD3Easing.EaseInOut)
                        .Scale(1.3f, 1f)
                        .Rotate(180f, 360f)
                        .Opacity(0.5f, 1f)
                    .OnComplete(() =>
                    {
                        tweenBox.style.rotate = new Rotate(0f);
                    })
                    .Start();
            };
            tweenRow.Add(mpBtn);

            // Yoyo repeat
            var yoyoBtn = new MD3Button("Yoyo x3", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            yoyoBtn.clicked += () =>
            {
                resetTweenBox();
                tweenHandle = MD3Animate.Tween(tweenBox)
                    .Duration(300f)
                    .Ease(MD3Easing.EaseInOut)
                    .Scale(1f, 1.4f)
                    .Repeat(6).Yoyo()
                    .Start();
            };
            tweenRow.Add(yoyoBtn);

            // Sequence (.Then chain)
            var seqBtn = new MD3Button("Sequence", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            seqBtn.clicked += () =>
            {
                resetTweenBox();
                tweenHandle = MD3Animate.Tween(tweenBox)
                    .Duration(300f).Ease(MD3Easing.EaseOutBack)
                    .Scale(1f, 1.5f)
                    .Then()
                        .Duration(200f).Ease(MD3Easing.EaseIn)
                        .Scale(1.5f, 0.8f)
                    .Then()
                        .Duration(250f).Ease(MD3Easing.EaseOutBounce)
                        .Scale(0.8f, 1f)
                    .Start();
            };
            tweenRow.Add(seqBtn);

            // Spring tween
            var springTweenBtn = new MD3Button("Spring Tween", MD3ButtonStyle.Tonal, size: MD3ButtonSize.Small);
            springTweenBtn.clicked += () =>
            {
                resetTweenBox();
                tweenHandle = MD3Animate.Tween(tweenBox)
                    .Spring(1f, 1.5f, v =>
                        tweenBox.style.scale = new UnityEngine.UIElements.Scale(new Vector3(v, v, 1f)),
                        stiffness: 200f, damping: 6f)
                    .Then()
                        .Spring(1.5f, 1f, v =>
                            tweenBox.style.scale = new UnityEngine.UIElements.Scale(new Vector3(v, v, 1f)),
                            stiffness: 300f, damping: 12f)
                    .Start();
            };
            tweenRow.Add(springTweenBtn);

            c.Add(tweenRow);
            c.Add(tweenBox);

            // ── Ripple ──
            AddSection(c, "Ripple");
            c.Add(new MD3Text("Click anywhere on the surface to see the ripple effect.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var rippleSurface = new VisualElement();
            rippleSurface.style.height = 100;
            rippleSurface.style.backgroundColor = _theme.SurfaceContainerHigh;
            rippleSurface.Radius(MD3Radius.M);
            rippleSurface.style.overflow = Overflow.Hidden;
            rippleSurface.style.alignItems = Align.Center;
            rippleSurface.style.justifyContent = Justify.Center;
            _themeCallbacks.Add(t => rippleSurface.style.backgroundColor = t.SurfaceContainerHigh);

            var rippleHint = new MD3Text("Click me", MD3TextStyle.Body);
            rippleHint.pickingMode = PickingMode.Ignore;
            rippleSurface.Add(rippleHint);

            rippleSurface.RegisterCallback<MouseDownEvent>(e =>
                MD3Ripple.Spawn(rippleSurface, e, _theme?.OnSurface ?? Color.white));
            c.Add(rippleSurface);
        }

        void Build_Progress(VisualElement c)
        {
            c.Add(new MD3Text("Progress", MD3TextStyle.HeadlineMedium));

            // Spinner
            AddSection(c, "Spinner");
            var spinnerRow = Row();
            spinnerRow.style.paddingTop = 8;
            spinnerRow.style.paddingBottom = 8;
            var sp1 = new MD3Spinner(32f); sp1.style.marginRight = 24; spinnerRow.Add(sp1);
            var sp2 = new MD3Spinner(48f); sp2.style.marginRight = 24; spinnerRow.Add(sp2);
            spinnerRow.Add(new MD3Spinner(64f));
            c.Add(spinnerRow);

            // LinearProgress
            AddSection(c, "Linear Progress");
            var lp1 = new MD3LinearProgress(0.65f);
            lp1.style.marginBottom = 16;
            c.Add(lp1);
            var lp2 = new MD3LinearProgress(0f, true);
            c.Add(lp2);

            // Sine Wave Linear Progress
            AddSection(c, "Linear Progress — Sine Wave");
            var lpWave1 = new MD3LinearProgress(0.6f, progressStyle: MD3LinearProgressStyle.SineWave);
            lpWave1.style.height = 12;
            lpWave1.style.marginBottom = 12;
            lpWave1.Radius(6);
            lpWave1.style.overflow = Overflow.Hidden;
            c.Add(lpWave1);
            c.Add(new MD3Text("60% determinate", MD3TextStyle.LabelCaption));
            c.Add(new MD3Spacer(MD3Spacing.M));

            var lpWave2 = new MD3LinearProgress(0f, true, MD3LinearProgressStyle.SineWave);
            lpWave2.style.height = 12;
            lpWave2.style.marginBottom = 12;
            lpWave2.Radius(6);
            lpWave2.style.overflow = Overflow.Hidden;
            c.Add(lpWave2);
            c.Add(new MD3Text("Indeterminate", MD3TextStyle.LabelCaption));
            c.Add(new MD3Spacer(MD3Spacing.M));

            var lpWave3 = new MD3LinearProgress(0f, true, MD3LinearProgressStyle.SineWave);
            lpWave3.style.height = 24;
            lpWave3.Radius(12);
            lpWave3.style.overflow = Overflow.Hidden;
            c.Add(lpWave3);
            c.Add(new MD3Text("Tall wave (24px)", MD3TextStyle.LabelCaption));

            // Track Bar
            AddSection(c, "Linear Progress — Track Bar");
            var lpTrack1 = new MD3LinearProgress(0.5f, progressStyle: MD3LinearProgressStyle.TrackBar);
            lpTrack1.style.height = 8;
            lpTrack1.style.marginBottom = 12;
            c.Add(lpTrack1);
            c.Add(new MD3Text("50% determinate", MD3TextStyle.LabelCaption));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var lpTrack2 = new MD3LinearProgress(0f, true, MD3LinearProgressStyle.TrackBar);
            lpTrack2.style.height = 8;
            c.Add(lpTrack2);
            c.Add(new MD3Text("Indeterminate", MD3TextStyle.LabelCaption));

            // Track Wave
            AddSection(c, "Linear Progress — Track Wave");

            var twValues = new[] { 0.15f, 0.4f, 0.65f, 0.85f, 1f };
            foreach (var tv in twValues)
            {
                var lpTw = new MD3LinearProgress(tv, progressStyle: MD3LinearProgressStyle.TrackWave);
                lpTw.style.height = 10;
                lpTw.style.marginBottom = 4;
                c.Add(lpTw);
                c.Add(new MD3Text($"{(int)(tv * 100)}%", MD3TextStyle.LabelCaption));
                c.Add(new MD3Spacer(MD3Spacing.S));
            }

            var lpTw2 = new MD3LinearProgress(0f, true, MD3LinearProgressStyle.TrackWave);
            lpTw2.style.height = 10;
            c.Add(lpTw2);
            c.Add(new MD3Text("Indeterminate", MD3TextStyle.LabelCaption));

            // CircularProgress
            AddSection(c, "Circular Progress");
            var cpRow = Row();
            cpRow.style.paddingTop = 8;
            cpRow.style.paddingBottom = 8;
            cpRow.Add(new MD3CircularProgress(0.25f, 64f));
            var cp2 = new MD3CircularProgress(0.7f, 64f); cp2.style.marginLeft = 24; cpRow.Add(cp2);
            var cp3 = new MD3CircularProgress(0f, 64f, true); cp3.style.marginLeft = 24; cpRow.Add(cp3);
            c.Add(cpRow);

            // Circular Progress — Wavy
            AddSection(c, "Circular Progress — Wavy");
            var cpwRow = Row();
            cpwRow.style.paddingTop = 8;
            cpwRow.style.paddingBottom = 8;
            cpwRow.Add(new MD3CircularProgress(0.4f, 64f, circStyle: MD3CircularProgressStyle.Wavy));
            var cpw2 = new MD3CircularProgress(0.75f, 64f, circStyle: MD3CircularProgressStyle.Wavy);
            cpw2.style.marginLeft = 24; cpwRow.Add(cpw2);
            var cpw3 = new MD3CircularProgress(0f, 64f, true, MD3CircularProgressStyle.Wavy);
            cpw3.style.marginLeft = 24; cpwRow.Add(cpw3);
            c.Add(cpwRow);

            // Circular Progress — Wavy Fill (no rotation, fills 0→100%)
            AddSection(c, "Circular Progress — Wavy Fill");
            c.Add(new MD3Text("No rotation. Value fills from top. Wave animates continuously.", MD3TextStyle.BodySmall));
            c.Add(new MD3Spacer(MD3Spacing.S));

            var cpwfRow = Row();
            cpwfRow.style.paddingTop = 8;
            cpwfRow.style.paddingBottom = 8;

            var fillValues = new[] { 0.15f, 0.4f, 0.65f, 0.9f, 1f };
            foreach (var fv in fillValues)
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 16;
                col.Add(new MD3CircularProgress(fv, 56f, circStyle: MD3CircularProgressStyle.WavyFill));
                var lbl = new Label($"{(int)(fv * 100)}%");
                lbl.style.fontSize = 11;
                lbl.style.marginTop = 4;
                lbl.style.color = _theme.OnSurfaceVariant;
                _themeCallbacks.Add(t => lbl.style.color = t.OnSurfaceVariant);
                col.Add(lbl);
                cpwfRow.Add(col);
            }
            c.Add(cpwfRow);

            // Loading Animations
            AddSection(c, "Loading Animations");

            // Size slider
            var loadSizeRow = Row();
            var loadSizeLabel = new Label("Size: 72");
            loadSizeLabel.style.color = _theme.OnSurfaceVariant;
            loadSizeLabel.style.width = 60;
            loadSizeLabel.style.fontSize = 13;
            loadSizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            loadSizeLabel.style.marginLeft = 8;
            _themeCallbacks.Add(t => loadSizeLabel.style.color = t.OnSurfaceVariant);
            var loadSizeSlider = new MD3Slider(72f, 32f, 128f, 4f);
            loadSizeSlider.style.flexGrow = 1;
            loadSizeRow.Add(loadSizeSlider);
            loadSizeRow.Add(loadSizeLabel);
            c.Add(loadSizeRow);

            var loadingGrid = new VisualElement();
            loadingGrid.style.flexDirection = FlexDirection.Row;
            loadingGrid.style.flexWrap = Wrap.Wrap;
            loadingGrid.style.marginTop = 8;

            var loadingStyles = new[]
            {
                ("Expressive", MD3LoadingStyle.Expressive),
                ("Expressive Ringed", MD3LoadingStyle.ExpressiveRinged),
                ("Bouncing Dots", MD3LoadingStyle.BouncingDots),
                ("Pulse Ring", MD3LoadingStyle.PulseRing),
                ("Wave Bars", MD3LoadingStyle.WaveBars),
                ("Rotating Dots", MD3LoadingStyle.RotatingDots),
                ("Sine Wave", MD3LoadingStyle.SineWave),
                ("DNA Helix", MD3LoadingStyle.DnaHelix),
                ("Orbit", MD3LoadingStyle.Orbit),
                ("Ripple", MD3LoadingStyle.Ripple),
                ("Typing Dots", MD3LoadingStyle.TypingDots),
            };

            Action<float> rebuildLoadingGrid = size =>
            {
                loadingGrid.Clear();
                float cellW = Mathf.Max(size + 24f, 100f);
                foreach (var (name, loadStyle) in loadingStyles)
                {
                    var cell = new VisualElement();
                    cell.style.width = cellW;
                    cell.style.alignItems = Align.Center;
                    cell.style.marginRight = 16;
                    cell.style.marginBottom = 16;

                    cell.Add(new MD3Loading(loadStyle, size));

                    var loadLabel = new Label(name);
                    loadLabel.style.color = _theme.OnSurfaceVariant;
                    loadLabel.style.fontSize = 12;
                    loadLabel.style.marginTop = 8;
                    loadLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    _themeCallbacks.Add(t => loadLabel.style.color = t.OnSurfaceVariant);
                    cell.Add(loadLabel);
                    loadingGrid.Add(cell);
                }
            };
            rebuildLoadingGrid(72f);

            loadSizeSlider.changed += v =>
            {
                loadSizeLabel.text = $"Size: {v:F0}";
                rebuildLoadingGrid(v);
            };

            c.Add(loadingGrid);

            // Skeleton
            AddSection(c, "Skeleton");
            var skeletonRow = Row();
            skeletonRow.style.paddingTop = 8;
            skeletonRow.Add(new MD3Skeleton(56f, 56f, true));
            var skCol = new VisualElement();
            skCol.style.marginLeft = 16;
            skCol.style.flexGrow = 1;
            skCol.Add(new MD3Skeleton(280f, 18f));
            var sk2 = new MD3Skeleton(200f, 18f);
            sk2.style.marginTop = 10;
            skCol.Add(sk2);
            var sk3 = new MD3Skeleton(240f, 14f);
            sk3.style.marginTop = 10;
            skCol.Add(sk3);
            skeletonRow.Add(skCol);
            c.Add(skeletonRow);
        }

        // ═══════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════

        void ApplyThemeColors()
        {
            _theme.ApplyTo(rootVisualElement);
            foreach (var cb in _themeCallbacks)
                cb(_theme);
        }

        VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 8;
            row.style.alignItems = Align.Center;
            return row;
        }

        VisualElement Spacer(float height)
        {
            var s = new VisualElement();
            s.style.height = height;
            s.style.flexShrink = 0;
            return s;
        }

        void AddSection(VisualElement parent, string title)
        {
            var label = new Label(title);
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = _theme.Primary;
            label.style.marginTop = 16;
            label.style.marginBottom = 8;
            parent.Add(label);
            _themeCallbacks.Add(t => label.style.color = t.Primary);
        }

        MD3Button CreateButton(string label, MD3ButtonStyle style)
        {
            var btn = new MD3Button(label, style);
            btn.style.marginRight = 8;
            btn.style.marginBottom = 4;
            btn.clicked += () => Debug.Log($"[MD3] Button: {label}");
            return btn;
        }

        MD3IconButton CreateIconButton(string icon, MD3IconButtonStyle style, string tooltip)
        {
            var btn = new MD3IconButton(icon, style);
            btn.style.marginRight = 8;
            btn.tooltip = tooltip;
            btn.clicked += () => Debug.Log($"[MD3] IconButton: {tooltip}");
            return btn;
        }

        VisualElement BadgeHost(string label, MD3Badge badge)
        {
            var host = new VisualElement();
            host.style.width = 60;
            host.style.height = 32;
            host.style.marginRight = 16;
            host.style.backgroundColor = _theme.SurfaceVariant;
            host.style.borderTopLeftRadius = 8;
            host.style.borderTopRightRadius = 8;
            host.style.borderBottomLeftRadius = 8;
            host.style.borderBottomRightRadius = 8;
            host.style.justifyContent = Justify.Center;
            host.style.alignItems = Align.Center;

            var lbl = new Label(label);
            lbl.style.color = _theme.OnSurfaceVariant;
            lbl.style.fontSize = 11;
            lbl.pickingMode = PickingMode.Ignore;
            host.Add(lbl);
            host.Add(badge);

            _themeCallbacks.Add(t =>
            {
                host.style.backgroundColor = t.SurfaceVariant;
                lbl.style.color = t.OnSurfaceVariant;
            });
            return host;
        }
    }
}
