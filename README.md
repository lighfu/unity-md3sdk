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

## フォント

初回使用時に以下のフォントが自動ダウンロードされます:
- **Material Symbols Outlined** (アイコン)
- **Noto Sans CJK** (日本語/韓国語/中国語)
- **Noto Emoji** (絵文字 - モノクロ)

フォントは `Fonts/` ディレクトリにキャッシュされます。設定は `Window > 紫陽花広場 > MD3 SDK Settings` から変更できます。

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

## Fonts

Fonts are automatically downloaded on first use:
- **Material Symbols Outlined** (icons)
- **Noto Sans CJK** (Japanese / Korean / Chinese)
- **Noto Emoji** (monochrome)

Fonts are cached in the `Fonts/` directory. Configure via `Window > 紫陽花広場 > MD3 SDK Settings`.

## Requirements

- Unity 2022.3+
- Editor only (UI Toolkit)
- No external package dependencies

## License

[MIT License](LICENSE)
