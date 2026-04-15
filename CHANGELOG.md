# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.7.1]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.7.1
[0.7.0]: https://github.com/lighfu/unity-md3sdk/releases/tag/v0.7.0
