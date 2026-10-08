# MD3 SDK

[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-blue)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Unity Editor 向け Material Design 3 UI Toolkit コンポーネントライブラリ。
70 以上のコンポーネント、HCT カラーシステム、ダーク/ライトテーマ、アニメーション、アイコン、多言語対応を提供します。

<!-- スクリーンショット: MD3 SDK Sample Window のキャプチャをここに配置 -->
<!-- ![MD3 SDK Sample](docs/screenshot.png) -->

## インストール

### VPM (ALCOM / VCC) - 推奨

1. 以下の VPM リポジトリ URL を ALCOM または VCC に追加:
   ```
   https://lighfu.github.io/vpm/index.json
   ```
2. パッケージ一覧から「MD3 SDK」をインストール

### Git URL

Unity Package Manager で「Add package from git URL」を選択:
```
https://github.com/lighfu/unity-md3sdk.git
```

### 手動インストール

このリポジトリをダウンロードし、Unity プロジェクトの `Packages/` フォルダ内に配置してください。

## Quick Start

```csharp
using AjisaiFlow.MD3SDK.Editor;
using UnityEditor;
using UnityEngine.UIElements;

public class MyWindow : EditorWindow
{
    [MenuItem("Window/My MD3 Window")]
    static void Open() => GetWindow<MyWindow>("My Window");

    void CreateGUI()
    {
        // テーマ適用 (Dark/Light 自動判定)
        var theme = MD3Theme.Auto();
        rootVisualElement.styleSheets.Add(MD3Theme.LoadThemeStyleSheet());
        rootVisualElement.styleSheets.Add(MD3Theme.LoadComponentsStyleSheet());
        theme.ApplyTo(rootVisualElement);

        // レイアウト
        var column = new MD3Column { style = { paddingTop = 16, paddingLeft = 16, paddingRight = 16 } };
        rootVisualElement.Add(column);

        // ボタン
        var button = new MD3Button("Click Me", MD3ButtonStyle.Filled);
        button.clicked += () => UnityEngine.Debug.Log("Clicked!");
        column.Add(button);

        // テキストフィールド
        var textField = new MD3TextField("Name", MD3TextFieldStyle.Outlined);
        column.Add(textField);

        // スイッチ
        var sw = new MD3Switch("Enable Feature");
        column.Add(sw);
    }
}
```

## コンポーネント一覧

### Actions
| コンポーネント | 説明 |
|---|---|
| `MD3Button` | Filled / Tonal / Outlined / Text スタイル、アイコン・ローディング対応 |
| `MD3IconButton` | アイコンのみのボタン |
| `MD3Fab` | Floating Action Button |
| `MD3SplitButton` | メインアクション + ドロップダウンの分割ボタン |
| `MD3SegmentedButton` | セグメント選択ボタン |

### Inputs
| コンポーネント | 説明 |
|---|---|
| `MD3TextField` | Outlined / Filled / Plain テキスト入力 |
| `MD3NumberField` | 数値入力 |
| `MD3SearchBar` | 検索バー |
| `MD3Dropdown` | ドロップダウンメニュー |
| `MD3DatePicker` | 日付選択 |
| `MD3Slider` | スライダー |

### Selection
| コンポーネント | 説明 |
|---|---|
| `MD3Checkbox` | チェックボックス |
| `MD3Radio` | ラジオボタン |
| `MD3Switch` | トグルスイッチ |
| `MD3Chip` | フィルタ / 選択チップ |

### Display
| コンポーネント | 説明 |
|---|---|
| `MD3Text` | テーマ対応テキスト |
| `MD3Icon` | Material Symbols アイコン (4,200+) |
| `MD3Badge` | バッジ |
| `MD3Tag` | タグ |
| `MD3Avatar` | アバター |
| `MD3ShapedAvatar` | 任意形状クリッピング + 回転アニメーション付きアバター (15 プリセット) |
| `MD3Thumbnail` | サムネイル |
| `MD3Image` / `MD3ImageCard` | 画像表示 |
| `MD3Card` | カード |
| `MD3ListItem` | リストアイテム |
| `MD3DataTable` / `MD3Table` | データテーブル |
| `MD3VirtualList` | 大量データ向け仮想スクロールリスト |

### Navigation
| コンポーネント | 説明 |
|---|---|
| `MD3Tab` | タブ |
| `MD3NavBarItem` | ナビゲーションバー |
| `MD3NavRailItem` | ナビゲーションレール |
| `MD3NavDrawerItem` | ナビゲーションドロワー |
| `MD3MenuItem` | メニューアイテム |
| `MD3Toolbar` / `MD3TopAppBar` | ツールバー / トップアプリバー |

### Feedback
| コンポーネント | 説明 |
|---|---|
| `MD3Dialog` / `MD3DialogRadio` | ダイアログ |
| `MD3FullScreenDialog` | フルスクリーンダイアログ |
| `MD3BottomSheet` / `MD3SideSheet` | シート |
| `MD3ContextMenu` | コンテキストメニュー |
| `MD3Snackbar` / `MD3Banner` | 通知 |
| `MD3Tooltip` | ツールチップ |
| `MD3EmptyState` | 空状態表示 |

### Progress
| コンポーネント | 説明 |
|---|---|
| `MD3CircularProgress` | 円形プログレス |
| `MD3LinearProgress` | 線形プログレス |
| `MD3Loading` | ローディングインジケーター (11 種) |
| `MD3Spinner` | スピナー |
| `MD3Skeleton` | スケルトンローダー |
| `MD3SuccessCheck` | 成功チェックアニメーション |
| `MD3Stepper` | ステッパー |

### Layout
| コンポーネント | 説明 |
|---|---|
| `MD3Column` / `MD3Row` | Flexbox レイアウト |
| `MD3Grid` | グリッドレイアウト |
| `MD3Stack` / `MD3Center` | スタック / センタリング |
| `MD3ScrollColumn` | スクロール付きカラム |
| `MD3SplitPane` | 分割パネル |
| `MD3Layout` / `MD3Constrained` | レイアウトヘルパー |
| `MD3Spacer` / `MD3Spacing` | スペーシング |
| `MD3Divider` / `MD3SectionLabel` | 区切り線 / セクションラベル |
| `MD3Foldout` | 折りたたみ |

### Theme & Animation
| コンポーネント | 説明 |
|---|---|
| `MD3Theme` | ダーク / ライト / カスタムテーマ管理 |
| `MD3Palette` / `MD3HCT` | HCT カラーシステム + シードカラーからの自動パレット生成 |
| `MD3Elevation` | エレベーション (影) |
| `MD3Ripple` | リップルエフェクト |
| `MD3Transition` | トランジション |
| `MD3Animate` | アニメーションシステム (14 種 Easing, Spring, Keyframe, Tween Builder) |

### System
| コンポーネント | 説明 |
|---|---|
| `MD3FontManager` | フォント自動ダウンロード・管理 |
| `MD3L10n` | 多言語対応 (日/英/韓/中) |

## テーマ

### ダーク / ライトテーマ

`MD3Theme.Auto()` は Unity Editor のテーマ設定を検出し、自動でダーク/ライトを選択します。明示的に指定する場合:

```csharp
var dark = MD3Theme.Dark();
var light = MD3Theme.Light();
```

### シードカラーからのテーマ生成

1 色から 25 色のテーマパレットを自動生成:

```csharp
var theme = MD3Theme.FromSeedColor(new Color(0.4f, 0.2f, 0.8f));
theme.ApplyTo(rootVisualElement);
```

## スタイルのカスタマイズ

色とフォントは `MD3Theme`、文字サイズ・角丸・余白・個別の形状は USS で設定できます。
`ApplyTo(root, customStyleSheets)` は SDK の標準 USS を自動で追加します。追加の USS は標準 USS の後に適用され、繰り返し呼び出しても重複しません。
標準 USS だけを自動追加する場合は `theme.ApplyTo(root, System.Array.Empty<StyleSheet>());` と呼び出してください。
既存の `ApplyTo(root)` はテーマだけを適用するので、USS を事前に追加する Quick Start の使い方もそのまま利用できます。

### ウィンドウ全体と一部分のテーマ

`Dark()` / `Light()` は共有インスタンスを返すため、変更する前に `Clone()` してください。
子要素にテーマを適用すると、その要素と子孫だけをカスタマイズできます。

```csharp
var theme = MD3Theme.Auto().Clone();
theme.Primary = new Color(0.10f, 0.40f, 0.65f);
theme.OnPrimary = Color.white;
var customStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MyStyles.uss");
rootVisualElement.AddToClassList("my-window");
theme.ApplyTo(rootVisualElement, customStyles);

var section = new VisualElement();
rootVisualElement.Add(section);
var sectionTheme = MD3Theme.FromSeedColor(new Color(0.0f, 0.55f, 0.55f), theme.IsDark);
sectionTheme.ApplyTo(section);

var action = new MD3Button("Save", MD3ButtonStyle.Filled);
action.AddToClassList("my-primary-action");
section.Add(action);
var actionTheme = sectionTheme.Clone();
actionTheme.Primary = new Color(0.60f, 0.22f, 0.10f);
actionTheme.OnPrimary = Color.white;
actionTheme.ApplyTo(action);

// 個別テーマを解除し、親のテーマに戻す
MD3Theme.ClearFrom(action);
MD3Theme.ClearFrom(section);
```

コンポーネントの色をテーマで設定すると、ホバー・押下・無効状態もそのパレットに追従します。
親のテーマを切り替えても、子要素に明示したテーマは維持されます。テーマの値を変更した後は `ApplyTo()` を呼び直してください。
`ClearFrom()` はテーマの指定を解除します。追加した USS やクラスは残るので、形状も戻す場合は対応するクラスを外してください。

テーマを適用した要素には、ウィンドウのルートと同じ `flex-grow: 1` と Surface の背景色が付きます。
一部分にだけ適用して残りの高さまで広げたくない場合は、その要素に `flex-grow: 0` を指定してください（コンポーネントに直接適用した場合は付きません）。
Tooltip・Dropdown・ContextMenu・DatePicker などのポップアップは、見切れないように一番外側のテーマ付き要素に表示され、開いた要素のテーマとフォントを引き継ぎます。

### 文字サイズ・角丸・余白と個別コンポーネント

追加 USS で SDK のクラスを指定すると、文字サイズ・角丸・余白をまとめて変更できます。
ウィンドウのルートや子要素に独自のクラスを付け、セレクターの先頭に置くと適用範囲を限定できます。
カスタムプロパティを使う場合は、自分の USS で参照する変数をすべて定義してください。変数は子孫にも継承されます。

```css
/* Assets/Editor/MyStyles.uss */
.my-window {
    --my-spacing-s: 12px;
    --my-button-padding: 20px;
    --my-card-padding: 28px;
    --my-button-radius: 10px;
    --my-card-radius: 16px;
    --my-body-font-size: 16px;
    --my-label-font-size: 15px;
}

.my-window .md3-button {
    border-radius: var(--my-button-radius);
    padding-left: var(--my-button-padding);
    padding-right: var(--my-button-padding);
}

.my-window .md3-card {
    border-radius: var(--my-card-radius);
    padding: var(--my-card-padding);
}

.my-window .md3-card__title {
    margin-bottom: var(--my-spacing-s);
}

.my-window .md3-card__body,
.my-window .md3-text--body > .md3-text__label {
    font-size: var(--my-body-font-size);
}

.my-window .md3-button__label {
    font-size: var(--my-label-font-size);
}

/* このボタンだけ形状と文字の太さを変える */
.my-window .my-primary-action {
    height: 48px;
    border-radius: 4px;
    padding-left: 32px;
    padding-right: 32px;
}

.my-window .my-primary-action .md3-button__label {
    font-size: 17px;
    -unity-font-style: normal;
}
```

`--my-*` はこの例で定義した変数名です。SDK に予約された変数名ではありません。
変数を使わず `border-radius: 10px;` のように直接指定することもできます。
カスタマイズ用クラスを外すと、SDK の標準 USS に戻ります。

`MD3Text` の文字サイズ・太さは内部の `.md3-text__label` に適用されます。
スタイルごとのクラスと既定の文字サイズは次のとおりです。

| スタイル | クラス (`md3-text--` に続く名前) | 既定の文字サイズ (px、記載順) |
|---|---|---|
| Display | `display-large`, `display-medium`, `display-small` | 40 / 32 / 28 |
| Headline | `headline-large`, `headline-medium`, `headline-small` | 24 / 20 / 16 |
| Title | `title-large`, `title-medium`, `title-small` | 18 / 14 / 12 |
| Body | `body`, `body-small` | 14 / 12 |
| Label | `label-large`, `label-medium`, `label-small`, `label-annotation` | 13 / 12 / 11 / 10 |

`style` やレイアウトヘルパーでインライン指定したサイズ・角丸・余白は USS より優先されます。
対話状態で更新される色は USS で上書きせず、テーマのパレットで指定してください。

### カスタムフォント

テーマに Unity の `Font` または TextCore の `FontAsset` を指定できます。

```csharp
var theme = MD3Theme.Auto().Clone();
theme.TextFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/MyFont.ttf");
// 生成済み FontAsset を使う場合はこちらを指定
// theme.TextFontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/Fonts/MyFont.asset");
theme.ApplyTo(rootVisualElement);
```

両方指定した場合は `TextFontAsset` が優先されます。両方 `null` の場合、一番外側のテーマは SDK のフォントを使い、入れ子のテーマは外側のフォントを引き継ぎます。
フォントのダウンロードや設定変更後の更新でも、明示したカスタムフォントは維持されます。
アイコンのフォントは `MD3Icon` が引き続き管理します。

`Window > 紫陽花広場 > Unity Material Design 3 SDK > Sample` の **Theme → Style Customization** で、
ローカルテーマ、カスタム USS、ボタン単位の上書きとリセットを試せます。USS の実例は [`MD3SampleStyles.uss`](MD3SampleStyles.uss) を参照してください。

## フォント

初回使用時に以下のフォントが自動ダウンロードされます:
- **Material Symbols Outlined** (アイコン)
- **Noto Sans CJK** (日本語/韓国語/中国語)
- **Noto Emoji** (絵文字 - モノクロ)

フォントは `Fonts/` ディレクトリにキャッシュされます。設定は `Window > 紫陽花広場 > Unity Material Design 3 SDK > Settings` から変更できます。

## 開発・テスト

Unity プロジェクトの `Packages/manifest.json` の `testables` に
`net.ajisaiflow.md3sdk` を追加し、Unity Test Framework をインストールしてください。
`Window > General > Test Runner` の EditMode で回帰テストを実行できます。
テストは選択イベント、ポップアップ、レイアウト、テーマ、アニメーションを検証します。

コマンドラインで実行する場合:

```text
Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode -testResults <results.xml> -logFile <test.log>
```

UI Toolkit のテストは一時 EditorWindow を使用するため、`-nographics` は指定しないでください。

## 動作環境

- Unity 2022.3 以上
- Editor only (UI Toolkit)
- 外部パッケージ依存なし

## ライセンス

[MIT License](LICENSE)

---

# English

## Overview

MD3 SDK is a Material Design 3 UI Toolkit component library for Unity Editor. It provides 70+ components, HCT color system, dark/light theming, animations, icons, and multi-language support.

## Installation

### VPM (ALCOM / VCC) - Recommended

1. Add the following VPM repository URL to ALCOM or VCC:
   ```
   https://lighfu.github.io/vpm/index.json
   ```
2. Install "MD3 SDK" from the package list

### Git URL

In Unity Package Manager, select "Add package from git URL":
```
https://github.com/lighfu/unity-md3sdk.git
```

## Quick Start

```csharp
using AjisaiFlow.MD3SDK.Editor;
using UnityEditor;
using UnityEngine.UIElements;

public class MyWindow : EditorWindow
{
    [MenuItem("Window/My MD3 Window")]
    static void Open() => GetWindow<MyWindow>("My Window");

    void CreateGUI()
    {
        var theme = MD3Theme.Auto();
        rootVisualElement.styleSheets.Add(MD3Theme.LoadThemeStyleSheet());
        rootVisualElement.styleSheets.Add(MD3Theme.LoadComponentsStyleSheet());
        theme.ApplyTo(rootVisualElement);

        var column = new MD3Column { style = { paddingTop = 16, paddingLeft = 16, paddingRight = 16 } };
        rootVisualElement.Add(column);

        column.Add(new MD3Button("Click Me", MD3ButtonStyle.Filled));
        column.Add(new MD3TextField("Name", MD3TextFieldStyle.Outlined));
        column.Add(new MD3Switch("Enable Feature"));
    }
}
```

## Theming

- `MD3Theme.Auto()` - Automatically detects Unity Editor dark/light mode
- `MD3Theme.Dark()` / `MD3Theme.Light()` - Explicit theme selection
- `MD3Theme.FromSeedColor(color)` - Generate a 25-color palette from a single seed color

## Style Customization

Clone a shared theme before changing it. `ApplyTo(root, customStyleSheets)` installs the SDK styles automatically and puts the custom sheets after them.
Use `theme.ApplyTo(root, System.Array.Empty<StyleSheet>())` for the default sheets alone. The existing `ApplyTo(root)` only applies the theme; add the sheets first as shown in Quick Start.
Apply a theme to a subtree or a component to give it an independent palette; hover, pressed and disabled colors follow that palette.
Use `MD3Theme.ClearFrom(element)` to resume inheriting the parent theme. Custom USS and classes stay attached.
A themed container gets `flex-grow: 1` and the Surface background like a window root; set `flex-grow: 0` on a partial scope that should not fill the remaining space (themes applied to components are exempt).
Popups such as Tooltip, Dropdown, ContextMenu and DatePicker open in the outermost themed element so they are not clipped, and keep the theme and font of the element that opened them.

```csharp
var theme = MD3Theme.Auto().Clone();
theme.Primary = new UnityEngine.Color(0.10f, 0.40f, 0.65f);
theme.OnPrimary = UnityEngine.Color.white;
rootVisualElement.AddToClassList("my-window");
theme.ApplyTo(rootVisualElement,
    AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MyStyles.uss"));

var button = new MD3Button("Save");
button.AddToClassList("my-action");
rootVisualElement.Add(button);
var accent = theme.Clone();
accent.Primary = new UnityEngine.Color(0.60f, 0.22f, 0.10f);
accent.ApplyTo(button);
```

```css
.my-window {
    --my-control-padding: 20px;
    --my-control-radius: 10px;
    --my-body-font-size: 16px;
}

.my-window .md3-button,
.my-window .md3-card {
    border-radius: var(--my-control-radius);
    padding-left: var(--my-control-padding);
    padding-right: var(--my-control-padding);
}

.my-window .md3-card__body,
.my-window .md3-text--body > .md3-text__label {
    font-size: var(--my-body-font-size);
}

.my-window .my-action {
    height: 48px;
    border-radius: 4px;
}

.my-window .my-action .md3-button__label {
    -unity-font-style: normal;
}
```

Custom USS selectors control spacing, shape and typography. Define every variable used by `var(--name)` in your stylesheet;
the `--my-*` names above belong to this example. Variables inherit through subtrees.
Add your own class to the beginning of selectors to scope overrides, then remove that class to restore the SDK defaults.
Style `MD3Text` labels through `.md3-text--body > .md3-text__label` or the corresponding role classes listed above.
Inline styles take precedence over USS; set interactive colors through the theme palette.
Set `TextFont` or `TextFontAsset` for a custom text font (`TextFontAsset` takes precedence). With both null, the outermost theme uses the SDK font and nested themes inherit the outer font.
Try the **Theme → Style Customization** section in the Sample window and see [`MD3SampleStyles.uss`](MD3SampleStyles.uss).

## Fonts

Fonts are automatically downloaded on first use:
- **Material Symbols Outlined** (icons)
- **Noto Sans CJK** (Japanese / Korean / Chinese)
- **Noto Emoji** (monochrome)

Fonts are cached in the `Fonts/` directory. Configure via `Window > 紫陽花広場 > Unity Material Design 3 SDK > Settings`.

## Development and tests

Install Unity Test Framework and add `net.ajisaiflow.md3sdk` to `testables` in
the consuming project's `Packages/manifest.json`. Run the EditMode suite from
`Window > General > Test Runner`. The tests cover selection events, popups,
layout, themes, and animations. For command-line runs, use the command above
without `-nographics`; the UI Toolkit tests open temporary EditorWindows.

## Requirements

- Unity 2022.3+
- Editor only (UI Toolkit)
- No external package dependencies

## License

[MIT License](LICENSE)
