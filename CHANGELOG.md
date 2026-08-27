# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.6] - 2026-08-27

`MD3Icon` を使うウィンドウを開くたびにエディターが数分間フリーズする問題 (#3) の修正。
アイコンアトラスのキャッシュが常にミスしていた根本原因に加えて、1 回のミスを
数分の停止に増幅していた 3 つの経路をあわせて塞いだ。

### Fixed

- **BMP 外のアイコン 51 個が焼かれていなかった** — `FontAsset.TryAddCharacters(string)` は
  サロゲートペアを 1 つの codepoint として扱わず、UTF-16 単位 2 つを個別に探して
  両方失敗する (実測: `uint[]` 版なら `ok=True`、`string` 版は `ok=False, missing=2`)。
  Material Symbols は U+FFF7E 以降 (Plane 15 の私用領域) にもアイコンを持つため、
  `HomeStorageGear` `TranslateSubtitles` `TranslateCc` `FrameSpark` `AppSpark`
  `YoutubeVideo` `CheckAlert` `CodeXml` `SpaceDashboard2` `GridLayoutSide` など
  **51 個の公開定数が定義されているのに □ で描画されていた**。
  codepoint 配列に変換して `TryAddCharacters(uint[], out uint[], bool)` を使うように変更。
  グリフ自体はフォントに存在しており、フォントの差し替えは不要。
  実測で `MD3_FA_icon.asset` の収録数が 3860 → 3911 (= `MD3Icon` のユニーク codepoint 全数)、
  欠落 51 → 0 になることを確認済み。入力ハッシュが変わるため、既存環境でも
  次回の描画で自動的に焼き直される。
- **アトラスのキャッシュ判定 (根本原因)** — `MD3Icon` の 4211 codepoint は
  2048x2048 の atlas 3 枚に収まるが、TextCore は multi-atlas を拡張するとき
  `m_AtlasTextures` を実使用枚数より大きく確保するため、末尾のスロットは正常な
  状態でも `null` のまま残る (3 枚使用 → 配列長 4)。`IsBroken()` はこの正常な
  末尾 `null` を破損と誤判定していたため、永続化した 12 MB のアセットが毎回
  `DeleteAsset` され、4211 glyph の SDF 焼き直し (実測 5 分以上、メインスレッド同期) が
  `CreateGUI` の中で走っていた。実使用枚数を返す `FontAsset.atlasTextureCount`
  (= `m_AtlasTextureIndex + 1`、`[SerializeField]` なのでロード後も有効) で
  `0 .. atlasTextureCount-1` だけを検査するように変更。
  atlas 1 枚の `MD3_FA_theme.asset` は配列長 1 で `null` を含まないためこの問題を
  踏まず、2026-05-24 の生成以降ずっとキャッシュが効いていた。
- **無制限の遅延リトライ** — `MD3Icon.EnsureFont` と `MD3Theme.ScheduleRefreshRetry` は
  「1 回だけリトライ」するはずが、`delayCall` の中で「済み」フラグを処理の *前* に
  戻していたため、`RefreshAllWindows` → `EnsureFont` が未スケジュールと誤認して
  毎 tick 際限なく再武装していた。1 回のリトライが `InvalidateAll` + 全 codepoint の
  焼き直しを伴うため、フォントが見つからない間ずっと CPU を焼き続けることになる。
  フラグを処理後に戻し、成功するまで最大 3 回で頭を打つようにした。
- **`RefreshAllWindows` の責務混在** — 「開いているウィンドウにフォントを貼り直す」
  処理が `InvalidateAll()` 経由で「生成済みアトラスを全削除する」まで行っていたため、
  フォントのダウンロード完了・設定画面の操作・リトライのたびに 12 MB のアイコン
  アトラスが焼き直されていた。`RefreshAllWindows` は static キャッシュを捨てて
  貼り直すだけに変更。Emoji のオン/オフやテーマフォントの切り替えでアイコン
  アトラスを巻き込むこともなくなった。

### Changed

- **生成アセットが内容アドレスになった** — 生成した FontAsset に「元フォントの GUID +
  焼き込んだ文字セット」のハッシュを記録し、要求が変わったときだけ焼き直すようにした。
  これまで `GetOrCreateIconFont` はパスの存在だけを見ており、docstring が約束していた
  「同じ codepoint セットなら」の判定を実際には行っていなかったため、`MD3Icon` に
  アイコンを追加しても `MD3FontAssetStoreMigration.CurrentVersion` を手で上げない限り
  古いアトラスが使われ続けた。テーマ側も key が `"theme"` 固定のため、フォントを
  差し替えても同じ穴があった。
  記録先は `EditorPrefs`。`AssetImporter.userData` を使うと `SaveAndReimport` が走り、
  この設計が回避している UUM-69151 の `ImportAsset` 経路に触れてしまうため。
  既存の生成アセットは記録が無いので、アップグレード直後に全ユーザーが焼き直しを
  踏まないよう「現在の入力で焼かれたもの」とみなして記録だけ引き継ぐ。
  古い世代を強制的に捨てたいときは従来どおり `MD3FontAssetStoreMigration.CurrentVersion`
  を上げる。

### Added

- **未収録 codepoint の実行時警告** — atlas に入っていない codepoint が実際に
  描画されたとき、その codepoint につきセッション 1 回だけ `MD3Icon` を名指しで警告する。
  焼き時の警告だけでは「定数はあるのに atlas に入っていない」状態に気づけず、
  実際に上記の 51 個が長期間見過ごされていた。アイコン 1 文字のときだけ判定するので、
  通常の文章にアイコンフォントを当てた場合は警告しない。
- `MD3FontManager.RebuildFontAssets()` — 生成済み FontAsset を全削除してから貼り直す。
  アトラスが壊れた場合の手動復旧用。通常は `RefreshAllWindows()` で足りる。
- **Narrow Layout Probe** (`Window/紫陽花広場/MD3 SDK Diagnostics/`) —
  狭い幅でレイアウトが返ってこなくなる事象 (#4) の犯人を切り分ける検査ウィンドウ。
  素の Label から全部乗せまで 11 段階の構成を、幅を変えながら 1 つずつ開く。
  ログはプロジェクト直下の `MD3SDK_LayoutProbe.log` に 1 行ずつ flush して書くので、
  ハングして強制終了してもディスクに残る。ステップ番号は構築の前に進めて
  永続化するため、強制終了して開き直せば自動的に次の構成へ進む。
  `warm` は 400px から段階的に絞って「どの幅で落ちたか」を出し、
  `cold` は一度も広い幅を通さずいきなり 100px で開く
  (`EditorWindow` の既定 minSize が 100x100 なので、`minSize` を設定していない
  ウィンドウの初回レイアウトはこの幅になる)。
- **Component Benchmark** (`Window/紫陽花広場/MD3 SDK Diagnostics/Component Benchmark`) —
  SDK の全コンポーネントを一括計測するベンチマーク。アセンブリ内の public な
  `VisualElement` 派生型をリフレクションで列挙するので、コンポーネントを追加しても
  自動的に対象に入る (67 型を計測、引数なしで生成できない 2 型のみスキップ)。
  測るのは 1 インスタンスあたりの生成時間・幅ごとのレイアウト時間・要素数、
  および任意で確保量。レイアウトは `EditorPanel.ValidateLayout()` を直接呼んで
  同期実行し、空のホストで同じ操作をした時間を差し引く。
  各コンポーネントで必ずウォームアップを挟む (初回は USS の解決とグリフの焼き込みで
  桁違いに遅く、混ぜると計測順に依存した嘘が出るため)。
  幅を複数取り「比 (狭/広)」列を出すので、狭いウィンドウで急に重くなる
  コンポーネントが上に来る。結果はソート可能な表と CSV
  (`MD3SDK_ComponentBenchmark.csv`) で出力する。
  進行状況は `MD3SDK_ComponentBenchmark.log` に 1 行ずつ flush して書くので、
  計測中にハングして強制終了しても、どのコンポーネントで止まったかが残る。
  既定 (確保量 off) で全 67 型が約 5 秒。確保量の計測は完全な GC を強制するため
  ヒープの大きいプロジェクトでは数分かかる。

## [0.8.5] - 2026-05-24

### Fixed
- v0.8.4 のリリース後、UnityAgent などのテキスト描画中に `MissingReferenceException: The object of type 'Texture2D' has been destroyed` が `FontAsset.TryAddCharacterInternal` で発生し、テキストの一部 (特に動的な日本語/絵文字) が描画されなくなる問題を修正。
- 根本原因: memory-only Dynamic fallback FontAsset に設定していた `HideFlags.DontSave` は「シリアライズしない」フラグでしかなく、Unity の `Resources.UnloadUnusedAssets` から atlas Texture2D を保護しない。正しくは `HideFlags.HideAndDontSave` (= `DontSave | HideInHierarchy | NotEditable`) を使う必要があった。さらに `isMultiAtlasTexturesEnabled = true` (default) では atlas overflow 時に新しい Texture2D が default HideFlags で作られ、同じ回収問題が再発する。
- 対策: fallback FontAsset を `FontAsset.CreateFontAsset` full overload で `atlasWidth/Height: 2048`, `enableMultiAtlasSupport: false` で初期化し、FontAsset 本体・material・atlasTextures すべてに `HideFlags.HideAndDontSave` を明示的に設定する。これにより 1 つの 2048×2048 atlas にすべての動的文字を集約し、追加 Texture2D が一切作られなくなる。

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
