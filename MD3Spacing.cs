namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Material Design 3 spacing tokens based on the 4px grid.
    /// Use these instead of raw pixel values for consistent spacing.
    /// </summary>
    public static class MD3Spacing
    {
        /// <summary>2px — Hairline spacing (borders, dividers)</summary>
        public const float XXS = 2f;

        /// <summary>4px — Minimal spacing (icon-to-text, tight groups)</summary>
        public const float XS = 4f;

        /// <summary>8px — Small spacing (related items within a group)</summary>
        public const float S = 8f;

        /// <summary>12px — Medium spacing (default gap between components)</summary>
        public const float M = 12f;

        /// <summary>16px — Large spacing (section padding, card content)</summary>
        public const float L = 16f;

        /// <summary>24px — Extra-large spacing (between sections, dialog padding)</summary>
        public const float XL = 24f;

        /// <summary>32px — Page-level spacing</summary>
        public const float XXL = 32f;

        /// <summary>48px — Major section separation</summary>
        public const float XXXL = 48f;
    }

    /// <summary>
    /// Material Design 3 corner radius tokens.
    /// </summary>
    public static class MD3Radius
    {
        /// <summary>0px — No rounding (sharp corners)</summary>
        public const float None = 0f;

        /// <summary>4px — Minimal rounding (text fields, list items)</summary>
        public const float XS = 4f;

        /// <summary>8px — Small rounding (chips, small cards)</summary>
        public const float S = 8f;

        /// <summary>12px — Medium rounding (cards, dialogs)</summary>
        public const float M = 12f;

        /// <summary>16px — Large rounding (containers, sheets)</summary>
        public const float L = 16f;

        /// <summary>20px — Buttons</summary>
        public const float XL = 20f;

        /// <summary>28px — FABs, large buttons</summary>
        public const float XXL = 28f;

        /// <summary>9999px — Full rounding (pills, circles)</summary>
        public const float Full = 9999f;
    }
}
