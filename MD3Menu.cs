namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// このパッケージが出すメニューの配置を 1 か所に集める。
    ///
    /// 各ファイルにパスを直書きすると、製品名やフォルダを変えたときに必ず取りこぼす。
    /// 実際 v0.8.6 までは MD3SDK だけが「紫陽花広場」のルート直下に 2 項目を出したうえで
    /// 別名の Diagnostics フォルダも持っており、他製品の「1 製品 1 フォルダ」という
    /// 並びから外れていた。
    ///
    /// <see cref="MenuItem"/> の引数は定数でなければならないが、const string 同士の
    /// 連結はコンパイル時に解決されるので、ここを起点に組み立てられる。
    /// </summary>
    internal static class MD3Menu
    {
        /// <summary>製品のメニューフォルダ。末尾の "/" を含む。</summary>
        public const string Root = "Window/紫陽花広場/Unity Material Design 3 SDK/";

        /// <summary>開発者向けの診断ツール。</summary>
        public const string Diagnostics = Root + "Diagnostics/";

        // ── 並び順 ──
        // Unity は priority の差が 10 を超えると区切り線を入れる。
        // Settings と Sample を上に、Diagnostics は区切り線のあとに置く。
        public const int SettingsPriority = 1;
        public const int SamplePriority = 2;

        public const int BenchmarkPriority = 100;
        public const int ProbeWarmPriority = 101;
        public const int ProbeColdPriority = 102;
        public const int ProbeLogPriority = 103;

        /// <summary>破壊的ではないが巻き戻し操作なので、さらに区切り線の下へ。</summary>
        public const int ProbeResetPriority = 200;
    }
}
