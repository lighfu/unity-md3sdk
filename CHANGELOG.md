# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.4] - 2026-05-24

### Fixed
- Unity 2022.3 で `Importer(NativeFormatImporter) generated inconsistent result` 警告が累積し、最終的に D3D11 GPU バッファ参照不整合で Unity がクラッシュする問題を構造的に修正。根本原因は Unity 公式の既知バグ UUM-69151 (Unity 6 で fix) で、Dynamic な FontAsset を `.asset` として永続化していると `TextEditorResourceManager.DoPostRenderUpdates` が `ImportAsset(path)` を呼び、AssetDatabase V2 が同 input・同 contentHash に対して異なる artifactId を生成することで分裂が発生する。
- `MD3FontAssetStore` を Static main + memory-only Dynamic fallback 構造に再設計。main FontAsset は ASCII printable のみ事前焼きして `AtlasPopulationMode.Static` で固定し、動的文字 (日本語・絵文字・新規漢字) はすべて `HideFlags.DontSave` の memory-only Dynamic fallback で受ける。これにより main の atlas が dirty 化せず、`DoPostRenderUpdates` が `ImportAsset` を呼ばなくなる。
- Material Symbols アイコンフォントは `FontAsset.CreateFontAsset` を full overload で初期化 (`samplingPointSize: 50`, `atlasWidth/Height: 2048`, `enableMultiAtlasSupport: true`) し、`MD3Icon` の全 PUA codepoint (4000+) を `TryAddCharacters` で事前焼きしてから Static 固定するため、ランタイムでの atlas 拡張が発生しなくなった。
- 旧バージョン (v0.8.3 以前) で生成済みの Dynamic 永続化アセットは migration v2 で自動削除して作り直す (`Assets/MD3SDKFonts/Generated/` 配下)。migration 完了後に `MD3FontManager.RefreshAllWindows()` を遅延呼び出しすることで、既存ウィンドウのフォント参照も自動的に更新される。

### Note
- Unity 6000.0.x 以降を使う場合は本修正は不要 (Unity 自身が UUM-69151 を fix 済み)。ただし本修正は Unity 6 でも動作する。
- `MD3Icon` の定数を経由せず、UI コードが直接 Material Symbols の codepoint 文字列を書き込んでいる場合、その codepoint は事前焼き対象外となり描画できない。`MD3Icon.<Name>` 定数の利用を推奨する。

## [0.8.3] - 2026-05-24

### Fixed
- `MD3FontAssetStore` で AssetDatabase V2 の artifactId 分裂を引き起こしていた冗長な再インポートを削除
  - `CreateAsset` + `AddObjectToAsset` + `SaveAssetIfDirty` 直後に呼んでいた `AssetDatabase.ImportAsset(path)` を削除
  - 旧版が引き起こしていた `ConsistencyChecker` の "inconsistent result" 警告と、SceneView 描画中の `TextEditorResourceManager.DoPostRenderUpdates` 経由での GPU バッファ破壊 → D3D11 クラッシュを解消（MeshPainter v2 など `SceneView.duringSceneGui` を使うツールを開いた瞬間に Unity がクラッシュする問題）

### Added
- 旧版で分裂状態になった `Assets/MD3SDKFonts/Generated/` 配下の `FontAsset` を 1 度だけ自動削除する `MD3FontAssetStoreMigration` (`[InitializeOnLoad]` + `EditorApplication.delayCall`) を追加
  - SDK アップグレード時にユーザーが手動で `Generated/` を消す必要がなくなり、初回エディタ起動時に healthy な `FontAsset` が自動再生成される
  - 実行済みフラグは project dataPath 単位の `EditorPrefs` キーで管理し、フォントの再ビルドは 1 度のみ

## [0.8.2] - 2026-05-22

### Added
- `MD3FontAssetStore`: 実行時生成の `FontAsset` を `Assets/MD3SDKFonts/Generated/` 配下のディスクアセットとして永続化するストアを追加
  - `atlasTexture` / `material` をサブアセットとして保存し、ドメインリロード / プレイモード遷移後もアトラスを無傷で復帰
  - `MD3Theme.LoadFontAsset` および `MD3Icon.EnsureFont` / `EnsureFilledFont` をストア経由に変更

### Removed
- `IsFontAssetBroken` / `ProtectFontAsset` / `MD3FontAutoSetup.OnAfterAssemblyReload` を削除（永続化により atlas が壊れなくなったため不要）
- `ClearFontCache` / `ClearCache` を `MD3FontAssetStore.InvalidateAll` 呼び出しに置き換え

### Fixed
- ドメインリロード後の "テキスト歯抜け" バグの根本解決（atlas `Texture2D` が破棄されるのを永続化で回避）

## [0.8.1] - 2026-04-16

### Fixed
- カスタムテーマがタブ切り替え時に消える問題を修正（`DetachFromPanelEvent` のハンドリング不備）

## [0.8.0] - 2026-04-16

### Added
- SDK 設定ウィンドウに seed color picker を備えた default theme を追加
- `MD3TextStyle` に `LabelMedium` / `LabelSmall` を追加

### Deprecated
- `MD3TextStyle.LabelCaption`

## [0.7.2] - 2026-04-16

### Fixed
- Domain reload 直後のストリーミング描画で `UIRStylePainter.DrawTextInfo` NRE / テキスト歯抜けが発生する race を修正
  - `MD3FontAutoSetup` に `AssemblyReloadEvents.afterAssemblyReload` ハンドラを追加し、consumer の `EditorWindow.OnEnable` より前に font cache をクリア
  - `MD3Theme.LoadFontAsset` で生成直後の FontAsset に対しても `IsFontAssetBroken` チェックを行い、broken なら cache せず `FontDefinition.FromFont` フォールバックに委ねる
  - `MD3Theme` に `s_refreshRetryScheduled` パターンを導入し、AssetDatabase 準備中の場合は次の editor tick で `RefreshAllWindows` を自動リトライ

## [0.7.1] - 2026-04-02

### Changed
- VPM distribution switched from compiled DLL to source code
- Version display in Settings window now shows correct version

## [0.7.0] - 2026-04-02

### Added
- Initial open-source release
- 70+ Material Design 3 components for Unity Editor UI Toolkit
- HCT color space and tonal palette generation (`MD3HCT`, `MD3Palette`)
- Light / Dark theme with automatic detection (`MD3Theme`)
- Seed color-based theme generation (`MD3Theme.FromSeedColor`)
- Material Symbols icon integration (4,200+ icons via `MD3Icon`)
- Multi-language support: Japanese, English, Korean, Chinese (`MD3L10n`)
- Automatic font management: Noto Sans CJK, Material Symbols, Emoji (`MD3FontManager`)
- Animation system with 14 easing types, spring, keyframe, tween builder (`MD3Animate`)
- Virtual scrolling list for large datasets (`MD3VirtualList`)
- Shaped avatar with 15 presets and morphing (`MD3ShapedAvatar`)
- Progress indicators: circular, linear, loading, spinner, skeleton (`MD3CircularProgress`, `MD3LinearProgress`, `MD3Loading`, `MD3Spinner`, `MD3Skeleton`)
- Sample window demonstrating all components (`Window > 紫陽花広場 > MD3 Toolkit Sample`)
- Settings window for font and language configuration (`Window > 紫陽花広場 > MD3 SDK Settings`)

[0.8.3]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.8.3
[0.8.2]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.8.2
[0.8.1]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.8.1
[0.8.0]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.8.0
[0.7.2]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.7.2
[0.7.1]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.7.1
[0.7.0]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.7.0
